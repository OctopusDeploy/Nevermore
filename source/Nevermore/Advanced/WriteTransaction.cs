using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Diagnostics;
using Nevermore.Mapping;
using Nevermore.Querying;
using Nevermore.Util;

namespace Nevermore.Advanced
{
    [DebuggerDisplay("{ToString()}")]
    public class WriteTransaction : ReadTransaction, IRelationalTransaction
    {
        readonly IRelationalStoreConfiguration configuration;
        readonly IKeyAllocator keyAllocator;
        readonly DataModificationQueryBuilder builder;

        public WriteTransaction(
            RelationalTransactionRegistry registry,
            RetriableOperation operationsToRetry,
            IRelationalStoreConfiguration configuration,
            IKeyAllocator keyAllocator,
            string name = null
        ) : base(registry, operationsToRetry, configuration, name)
        {
            this.configuration = configuration;
            this.keyAllocator = keyAllocator;
            builder = new DataModificationQueryBuilder(configuration, AllocateId);
        }

        public void Insert<TDocument>(TDocument document, InsertOptions options = null) where TDocument : class
        {
            var command = builder.PrepareInsert(new[] {document}, options);
            configuration.Hooks.BeforeInsert(document, command.Mapping, this);

            var newRowVersions = ExecuteSingleDataModification(command);
            AssertModificationHasBeenSuccessful(document, command.Mapping, newRowVersions);

            configuration.Hooks.AfterInsert(document, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, document);
        }

        public Task InsertAsync<TDocument>(TDocument document, CancellationToken cancellationToken = default) where TDocument : class
        {
            return InsertAsync(document, null, cancellationToken);
        }

        public async Task InsertAsync<TDocument>(TDocument document, InsertOptions options, CancellationToken cancellationToken = default) where TDocument : class
        {
            var command = builder.PrepareInsert(new[] {document}, options);
            await configuration.Hooks.BeforeInsertAsync(document, command.Mapping, this);

            var newRowVersion = await ExecuteSingleDataModificationAsync(command, cancellationToken);
            AssertModificationHasBeenSuccessful(document, command.Mapping, newRowVersion);

            await configuration.Hooks.AfterInsertAsync(document, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, document);
        }

        public void InsertMany<TDocument>(IReadOnlyCollection<TDocument> documents, InsertOptions options = null) where TDocument : class
        {
            IReadOnlyList<object> documentList = documents.ToArray();
            if (!documentList.Any()) return;

            var command = builder.PrepareInsert(documentList, options);
            foreach (var document in documents) configuration.Hooks.BeforeInsert(document, command.Mapping, this);

            var newRowVersions = ExecuteDataModification(command);
            AssertModificationsHaveBeenSuccessful(documentList, command.Mapping, newRowVersions);

            foreach (var document in documentList) configuration.Hooks.AfterInsert(document, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, documentList);
        }

        public Task InsertManyAsync<TDocument>(IReadOnlyCollection<TDocument> documents, CancellationToken cancellationToken = default) where TDocument : class
        {
            return InsertManyAsync(documents, null, cancellationToken);
        }

        public async Task InsertManyAsync<TDocument>(IReadOnlyCollection<TDocument> documents, InsertOptions options, CancellationToken cancellationToken = default) where TDocument : class
        {
            IReadOnlyList<object> documentList = documents.ToArray();
            if (!documentList.Any()) return;

            var command = builder.PrepareInsert(documentList, options);

            foreach (var document in documentList) await configuration.Hooks.BeforeInsertAsync(document, command.Mapping, this);

            var newRowVersions = await ExecuteDataModificationAsync(command, cancellationToken);
            AssertModificationsHaveBeenSuccessful(documentList, command.Mapping, newRowVersions);

            foreach (var document in documentList) await configuration.Hooks.AfterInsertAsync(document, command.Mapping, this);

            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, documentList);
        }

        public void Update<TDocument>(TDocument document, UpdateOptions options = null) where TDocument : class
        {
            var command = builder.PrepareUpdate(document, options);
            configuration.Hooks.BeforeUpdate(document, command.Mapping, this);

            var newRowVersion = ExecuteSingleDataModification(command);
            AssertModificationHasBeenSuccessful(document, command.Mapping, newRowVersion);

            configuration.Hooks.AfterUpdate(document, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, document);
        }

        public Task UpdateAsync<TDocument>(TDocument document, CancellationToken cancellationToken = default) where TDocument : class
        {
            return UpdateAsync(document, null, cancellationToken);
        }

        public async Task UpdateAsync<TDocument>(TDocument document, UpdateOptions options, CancellationToken cancellationToken = default) where TDocument : class
        {
            var command = builder.PrepareUpdate(document, options);
            await configuration.Hooks.BeforeUpdateAsync(document, command.Mapping, this);

            var newRowVersion = await ExecuteSingleDataModificationAsync(command, cancellationToken);
            AssertModificationHasBeenSuccessful(document, command.Mapping, newRowVersion);

            await configuration.Hooks.AfterUpdateAsync(document, command.Mapping, this);
        }

        public void Delete<TDocument>(string id, DeleteOptions options = null) where TDocument : class
            => Delete<TDocument>((object) id, options);

        public void Delete<TDocument>(int id, DeleteOptions options = null) where TDocument : class
            => Delete<TDocument>((object) id, options);

        public void Delete<TDocument>(long id, DeleteOptions options = null) where TDocument : class
            => Delete<TDocument>((object) id, options);

        public void Delete<TDocument>(Guid id, DeleteOptions options = null) where TDocument : class
            => Delete<TDocument>((object) id, options);

        public void Delete<TDocument>(TDocument document, DeleteOptions options = null) where TDocument : class
        {
            var id = configuration.DocumentMaps.GetId(document);
            Delete<TDocument>(id, options);
        }

        void Delete<TDocument>(object id, DeleteOptions options = null) where TDocument : class
        {
            var command = builder.PrepareDelete<TDocument>(id, options);
            configuration.Hooks.BeforeDelete<TDocument>(id, command.Mapping, this);
            ExecuteNonQuery(command);
            configuration.Hooks.AfterDelete<TDocument>(id, command.Mapping, this);
        }

        public Task DeleteAsync<TDocument>(string id, CancellationToken cancellationToken = default) where TDocument : class
            => DeleteAsync<TDocument>(id, null, cancellationToken);

        public Task DeleteAsync<TDocument>(int id, CancellationToken cancellationToken = default) where TDocument : class
            => DeleteAsync<TDocument>(id, null, cancellationToken);

        public Task DeleteAsync<TDocument>(long id, CancellationToken cancellationToken = default) where TDocument : class
            => DeleteAsync<TDocument>(id, null, cancellationToken);

        public Task DeleteAsync<TDocument>(Guid id, CancellationToken cancellationToken = default) where TDocument : class
            => DeleteAsync<TDocument>(id, null, cancellationToken);

        public Task DeleteAsync<TDocument>(TDocument document, CancellationToken cancellationToken = default) where TDocument : class
        {
            return DeleteAsync(document, null, cancellationToken);
        }

        public Task DeleteAsync<TDocument>(string id, DeleteOptions options, CancellationToken cancellationToken = default) where TDocument : class
            => DeleteAsync<TDocument>((object) id, options, cancellationToken);

        public Task DeleteAsync<TDocument>(int id, DeleteOptions options, CancellationToken cancellationToken = default) where TDocument : class
            => DeleteAsync<TDocument>((object) id, options, cancellationToken);

        public Task DeleteAsync<TDocument>(long id, DeleteOptions options, CancellationToken cancellationToken = default) where TDocument : class
            => DeleteAsync<TDocument>((object) id, options, cancellationToken);

        public Task DeleteAsync<TDocument>(Guid id, DeleteOptions options, CancellationToken cancellationToken = default) where TDocument : class
            => DeleteAsync<TDocument>((object) id, options, cancellationToken);

        public Task DeleteAsync<TDocument>(TDocument document, DeleteOptions options, CancellationToken cancellationToken = default) where TDocument : class
        {
            var id = configuration.DocumentMaps.GetId(document);
            return DeleteAsync<TDocument>(id, options, cancellationToken);
        }

        async Task DeleteAsync<TDocument>(object id, DeleteOptions options, CancellationToken cancellationToken = default) where TDocument : class
        {
            var command = builder.PrepareDelete<TDocument>(id, options);
            await configuration.Hooks.BeforeDeleteAsync<TDocument>(id, command.Mapping, this);
            await ExecuteNonQueryAsync(command, cancellationToken);
            await configuration.Hooks.AfterDeleteAsync<TDocument>(id, command.Mapping, this);
        }

        public IDeleteQueryBuilder<TDocument> DeleteQuery<TDocument>() where TDocument : class
        {
            return new DeleteQueryBuilder<TDocument>(ParameterNameGenerator, builder, this);
        }

        public string AllocateId(Type documentType)
        {
            var mapping = configuration.DocumentMaps.Resolve(documentType);
            return AllocateId(mapping);
        }

        string AllocateId(DocumentMap mapping)
        {
            return AllocateId(mapping.TableName, mapping.IdFormat);
        }

        public string AllocateId(string tableName, string idPrefix)
        {
            return AllocateId(tableName, key => $"{idPrefix}-{key}");
        }

        public string AllocateId(string tableName, Func<int, string> idFormatter)
        {
            var key = keyAllocator.NextId(tableName);
            return idFormatter(key);
        }

        public void Commit()
        {
            if (!configuration.AllowSynchronousOperations)
                throw new SynchronousOperationsDisabledException();
            configuration.Hooks.BeforeCommit(this);
            Transaction.Commit();
            configuration.Hooks.AfterCommit(this);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await configuration.Hooks.BeforeCommitAsync(this);
            await Transaction.CommitAsync(cancellationToken);
            await configuration.Hooks.AfterCommitAsync(this);
        }

        object[] ExecuteDataModification(PreparedCommand command)
        {
            if (!command.Mapping.IsRowVersioningEnabled)
            {
                ExecuteNonQuery(command);
                return Array.Empty<object>();
            }
            
            var newRowVersions = new List<object>();
            using (var reader = ExecuteReader(command))
            {
                while (reader.Read())
                {
                    newRowVersions.Add(reader.GetValue(0));
                }
            }

            return newRowVersions.ToArray();
        }

        object ExecuteSingleDataModification(PreparedCommand command)
        {
            var result = ExecuteDataModification(command);
            return result.SingleOrDefault();
        }

        async Task<object[]> ExecuteDataModificationAsync(PreparedCommand command, CancellationToken cancellationToken)
        {
            if (!command.Mapping.IsRowVersioningEnabled)
            {
                await ExecuteNonQueryAsync(command, cancellationToken);
                return Array.Empty<object>();
            }

            var newRowVersions = new List<object>();
            using (var reader = await ExecuteReaderAsync(command, cancellationToken))
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    newRowVersions.Add(reader.GetValue(0));
                }
            }

            return newRowVersions.ToArray();
        }

        async Task<object> ExecuteSingleDataModificationAsync(PreparedCommand command, CancellationToken cancellationToken)
        {
            var result = await ExecuteDataModificationAsync(command, cancellationToken);
            return result.SingleOrDefault();
        }

        void AssertModificationsHaveBeenSuccessful(IReadOnlyList<object> documentList, DocumentMap mapping, object[] newRowVersions)
        {
            if (!mapping.IsRowVersioningEnabled) return;

            for (var i = 0; i < documentList.Count; i++)
            {
                AssertModificationHasBeenSuccessful(documentList[i], mapping, newRowVersions[i]);
            }
        }

        void AssertModificationHasBeenSuccessful<TDocument>(TDocument document, DocumentMap mapping, object newRowVersion) where TDocument : class
        {
            if (!mapping.IsRowVersioningEnabled) return;

            if (newRowVersion == null) throw new StaleDataException($"Modification failed for '{typeof(TDocument).Name}' document with '{mapping.GetId(document)}' Id because submitted data was out of date. Refresh the document and try again.");

            mapping.RowVersionColumn.PropertyHandler.Write(document, newRowVersion);
        }

    }
}

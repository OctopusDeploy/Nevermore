using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Nevermore.Diagnostics;
using Nevermore.Mapping;
using Nevermore.Querying;
using Nevermore.Util;

namespace Nevermore.Advanced
{
    [DebuggerDisplay("{ToString()}")]
    public class WriteTransaction : ReadTransaction, IRelationalTransaction
    {
        readonly RelationalTransactionRegistry registry;
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
            this.registry = registry;
            this.configuration = configuration;
            this.keyAllocator = keyAllocator;
            builder = new DataModificationQueryBuilder(configuration, AllocateId);
        }

        public void Insert<TDocument>(TDocument document, InsertOptions options = null) where TDocument : class
        {
            var commands = builder.PrepareInsert(new[] {document}, options);
            foreach (var command in commands)
            {
                configuration.Hooks.BeforeInsert(document, command.Mapping, this);

                var newRowVersion = ExecuteSingleDataModification(command);
                ApplyNewRowVersionIfRequired(document, command.Mapping, newRowVersion);

                configuration.Hooks.AfterInsert(document, command.Mapping, this);
            }

            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, document);
        }

        public Task InsertAsync<TDocument>(TDocument document, CancellationToken cancellationToken = default) where TDocument : class
        {
            return InsertAsync(document, null, cancellationToken);
        }

        public async Task InsertAsync<TDocument>(TDocument document, InsertOptions options, CancellationToken cancellationToken = default) where TDocument : class
        {
            var commands = builder.PrepareInsert(new[] {document}, options);
            foreach (var command in commands)
            {
                await configuration.Hooks.BeforeInsertAsync(document, command.Mapping, this);

                var newRowVersion = await ExecuteSingleDataModificationAsync(command, cancellationToken);
                ApplyNewRowVersionIfRequired(document, command.Mapping, newRowVersion);

                await configuration.Hooks.AfterInsertAsync(document, command.Mapping, this);
            }

            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, document);
        }

        public void InsertMany<TDocument>(IReadOnlyCollection<TDocument> documents, InsertOptions options = null) where TDocument : class
        {
            IReadOnlyList<object> documentList = documents.ToArray();
            if (!documentList.Any()) return;

            var commands = builder.PrepareInsert(documentList, options);
            foreach (var command in commands)
            {
                foreach (var document in documents) configuration.Hooks.BeforeInsert(document, command.Mapping, this);

                var newRowVersions = ExecuteDataModification(command);
                ApplyNewRowVersionsIfRequired(documentList, command.Mapping, newRowVersions);

                foreach (var document in documentList) configuration.Hooks.AfterInsert(document, command.Mapping, this);
            }

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

            var commands = builder.PrepareInsert(documentList, options);
            foreach (var command in commands)
            {
                foreach (var document in documentList) await configuration.Hooks.BeforeInsertAsync(document, command.Mapping, this);

                var newRowVersions = await ExecuteDataModificationAsync(command, cancellationToken);
                ApplyNewRowVersionsIfRequired(documentList, command.Mapping, newRowVersions);

                foreach (var document in documentList) await configuration.Hooks.AfterInsertAsync(document, command.Mapping, this);
            }

            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, documentList);
        }

        public void Update<TDocument>(TDocument document, UpdateOptions options = null) where TDocument : class
        {
            var command = builder.PrepareUpdate(document, options);
            configuration.Hooks.BeforeUpdate(document, command.Mapping, this);

            var newRowVersion = ExecuteSingleDataModification(command);
            ApplyNewRowVersionIfRequired(document, command.Mapping, newRowVersion);

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
            ApplyNewRowVersionIfRequired(document, command.Mapping, newRowVersion);

            await configuration.Hooks.AfterUpdateAsync(document, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, document);
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

            //The results need to be read eagerly so errors are raised while code is still executing within CommandExecutor error handling logic
            return ReadResults(command, reader => reader.GetValue(0));
        }

        object ExecuteSingleDataModification(PreparedCommand command)
        {
            var results = ExecuteDataModification(command);
            return results.SingleOrDefault();
        }

        async Task<object[]> ExecuteDataModificationAsync(PreparedCommand command, CancellationToken cancellationToken)
        {
            if (!command.Mapping.IsRowVersioningEnabled)
            {
                await ExecuteNonQueryAsync(command, cancellationToken);
                return Array.Empty<object>();
            }

            return await ReadResultsAsync(command, reader => reader.GetValue(0), cancellationToken);
        }

        async Task<object> ExecuteSingleDataModificationAsync(PreparedCommand command, CancellationToken cancellationToken)
        {
            var results = await ExecuteDataModificationAsync(command, cancellationToken);
            return results.SingleOrDefault();
        }

        void ApplyNewRowVersionsIfRequired(IReadOnlyList<object> documentList, DocumentMap mapping, object[] newRowVersions)
        {
            if (!mapping.IsRowVersioningEnabled) return;

            for (var i = 0; i < documentList.Count; i++)
            {
                ApplyNewRowVersionIfRequired(documentList[i], mapping, newRowVersions[i]);
            }
        }

        void ApplyNewRowVersionIfRequired<TDocument>(TDocument document, DocumentMap mapping, object newRowVersion) where TDocument : class
        {
            if (!mapping.IsRowVersioningEnabled) return;

            if (newRowVersion == null) throw new StaleDataException($"Modification failed for '{typeof(TDocument).Name}' document with '{mapping.GetId(document)}' Id because submitted data was out of date. Refresh the document and try again.");

            mapping.RowVersionColumn.PropertyHandler.Write(document, newRowVersion);
        }

    }
}
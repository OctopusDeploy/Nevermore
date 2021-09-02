using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
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

            var output = ExecuteSingleDataModification(command);
            ApplyNewRowVersionIfRequired(document, command.Mapping, output);
            ApplyIdentityIdsIfRequired(document, command.Mapping, output);

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

            var output = await ExecuteSingleDataModificationAsync(command, cancellationToken);
            ApplyNewRowVersionIfRequired(document, command.Mapping, output);
            ApplyIdentityIdsIfRequired(document, command.Mapping, output);

            await configuration.Hooks.AfterInsertAsync(document, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, document);
        }

        public void InsertMany<TDocument>(IReadOnlyCollection<TDocument> documents, InsertOptions options = null) where TDocument : class
        {
            IReadOnlyList<object> documentList = documents.ToArray();
            if (!documentList.Any()) return;

            var command = builder.PrepareInsert(documentList, options);
            foreach (var document in documents) configuration.Hooks.BeforeInsert(document, command.Mapping, this);

            var outputs = ExecuteDataModification(command);
            ApplyNewRowVersionsIfRequired(documentList, command.Mapping, outputs);
            ApplyIdentityIdsIfRequired(documentList, command.Mapping, outputs);

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

            foreach (var document in documentList)
                await configuration.Hooks.BeforeInsertAsync(document, command.Mapping, this);

            var outputs = await ExecuteDataModificationAsync(command, cancellationToken);
            ApplyNewRowVersionsIfRequired(documentList, command.Mapping, outputs);
            ApplyIdentityIdsIfRequired(documentList, command.Mapping, outputs);

            foreach (var document in documentList)
                await configuration.Hooks.AfterInsertAsync(document, command.Mapping, this);

            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, documentList);
        }

        public void Update<TDocument>(TDocument document, UpdateOptions options = null) where TDocument : class
        {
            var command = builder.PrepareUpdate(document, options);
            configuration.Hooks.BeforeUpdate(document, command.Mapping, this);

            var output = ExecuteSingleDataModification(command);
            ApplyNewRowVersionIfRequired(document, command.Mapping, output);

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

            var output = await ExecuteSingleDataModificationAsync(command, cancellationToken);
            ApplyNewRowVersionIfRequired(document, command.Mapping, output);

            await configuration.Hooks.AfterUpdateAsync(document, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, document);
        }

        public void Delete<TDocument>(string id, DeleteOptions options = null) where TDocument : class
            => Delete<TDocument, string>(id, options);

        public void Delete<TDocument>(int id, DeleteOptions options = null) where TDocument : class
            => Delete<TDocument, int>(id, options);

        public void Delete<TDocument>(long id, DeleteOptions options = null) where TDocument : class
            => Delete<TDocument, long>(id, options);

        public void Delete<TDocument>(Guid id, DeleteOptions options = null) where TDocument : class
            => Delete<TDocument, Guid>(id, options);

        public void Delete<TDocument>(TDocument document, DeleteOptions options = null) where TDocument : class
        {
            var id = configuration.DocumentMaps.GetId(document);
            DeleteInternal<TDocument>(id, options);
        }

        public void Delete<TDocument, TKey>(TKey id, DeleteOptions options = null) where TDocument : class
        {
            DeleteInternal<TDocument>(id, options);
        }

        void DeleteInternal<TDocument>(object id, DeleteOptions options) where TDocument : class
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
            => DeleteAsyncInternal<TDocument>(document, null, cancellationToken);

        public Task DeleteAsync<TDocument>(string id, DeleteOptions options, CancellationToken cancellationToken = default) where TDocument : class
            => DeleteAsync<TDocument, string>(id, options, cancellationToken);

        public Task DeleteAsync<TDocument>(int id, DeleteOptions options, CancellationToken cancellationToken = default) where TDocument : class
            => DeleteAsync<TDocument, int>(id, options, cancellationToken);

        public Task DeleteAsync<TDocument>(long id, DeleteOptions options, CancellationToken cancellationToken = default) where TDocument : class
            => DeleteAsync<TDocument, long>(id, options, cancellationToken);

        public Task DeleteAsync<TDocument>(Guid id, DeleteOptions options, CancellationToken cancellationToken = default) where TDocument : class
            => DeleteAsync<TDocument, Guid>(id, options, cancellationToken);

        public Task DeleteAsync<TDocument>(TDocument document, DeleteOptions options, CancellationToken cancellationToken = default) where TDocument : class
        {
            var id = configuration.DocumentMaps.GetId(document);
            return DeleteAsyncInternal<TDocument>(id, options, cancellationToken);
        }

        public Task DeleteAsync<TDocument, TKey>(TKey id, CancellationToken cancellationToken = default) where TDocument : class
            => DeleteAsync<TDocument, TKey>(id, null, cancellationToken);

        public async Task DeleteAsync<TDocument, TKey>(TKey id, DeleteOptions options, CancellationToken cancellationToken = default) where TDocument : class
        {
            await DeleteAsyncInternal<TDocument>(id, options, cancellationToken);
        }

        async Task DeleteAsyncInternal<TDocument>(object id, DeleteOptions options, CancellationToken cancellationToken) where TDocument : class
        {
            var command = builder.PrepareDelete<TDocument>(id, options);
            await configuration.Hooks.BeforeDeleteAsync<TDocument>(id, command.Mapping, this);
            await ExecuteNonQueryAsync(command, cancellationToken);
            await configuration.Hooks.AfterDeleteAsync<TDocument>(id, command.Mapping, this);
        }

        public IDeleteQueryBuilder<TDocument> DeleteQuery<TDocument>() where TDocument : class
        {
            return new DeleteQueryBuilder<TDocument>(ParameterNameGeneratorFactory(), builder, this);
        }

        public TKey AllocateId<TKey>(Type documentType)
        {
            var mapping = configuration.DocumentMaps.Resolve(documentType);
            return AllocateIdForMapping<TKey>(mapping);
        }

        public TKey AllocateId<TDocument, TKey>()
        {
            var mapping = configuration.DocumentMaps.Resolve<TDocument>();
            return AllocateIdForMapping<TKey>(mapping);
        }

        TKey AllocateIdForMapping<TKey>(DocumentMap mapping)
        {
            if (mapping.IdColumn?.Direction == ColumnDirection.FromDatabase)
                throw new InvalidOperationException($"The document map for {mapping.Type} is configured to use an identity key handler.");

            return (TKey) AllocateIdUsingHandler(mapping);
        }

        object AllocateId(DocumentMap mapping)
        {
            if (mapping.IdColumn?.Direction == ColumnDirection.FromDatabase)
                throw new InvalidOperationException($"The document map for {mapping.Type} is configured to use an identity key handler.");

            return AllocateIdUsingHandler(mapping);
        }

        object AllocateIdUsingHandler(DocumentMap mapping)
        {
            if (mapping.IdColumn is null || mapping.IsIdentityId)
                throw new InvalidOperationException($"Cannot allocate an id when an Id column has not been mapped.");
            return mapping.IdColumn.PrimaryKeyHandler.GetNextKey(keyAllocator, mapping.TableName);
        }

        public string AllocateId(string tableName, string idPrefix)
        {
            var key = keyAllocator.NextId(tableName);
            return $"{idPrefix}-{key}";
        }

        public void Commit()
        {
            if (Transaction is null)
                throw new InvalidOperationException("There is no current transaction, call Open/OpenAsync to start a transaction");
            if (!configuration.AllowSynchronousOperations)
                throw new SynchronousOperationsDisabledException();
            configuration.Hooks.BeforeCommit(this);
            Transaction.Commit();
            configuration.Hooks.AfterCommit(this);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            if (Transaction is null)
                throw new InvalidOperationException("There is no current transaction, call Open/OpenAsync to start a transaction");
            await configuration.Hooks.BeforeCommitAsync(this);
            await Transaction.CommitAsync(cancellationToken);
            await configuration.Hooks.AfterCommitAsync(this);
        }

        DataModificationOutput[] ExecuteDataModification(PreparedCommand command)
        {
            if (!command.Mapping.HasModificationOutputs)
            {
                ExecuteNonQuery(command);
                return Array.Empty<DataModificationOutput>();
            }

            //The results need to be read eagerly so errors are raised while code is still executing within CommandExecutor error handling logic
            return ReadResults(command,
                reader => DataModificationOutput.Read(reader, command.Mapping,
                    command.Operation == RetriableOperation.Insert));
        }

        DataModificationOutput ExecuteSingleDataModification(PreparedCommand command)
        {
            var results = ExecuteDataModification(command);
            return results.SingleOrDefault();
        }

        async Task<DataModificationOutput[]> ExecuteDataModificationAsync(PreparedCommand command, CancellationToken cancellationToken)
        {
            if (!command.Mapping.HasModificationOutputs)
            {
                await ExecuteNonQueryAsync(command, cancellationToken);
                return Array.Empty<DataModificationOutput>();
            }

            return await ReadResultsAsync(command,
                async reader => await DataModificationOutput.ReadAsync(reader, command.Mapping,
                    command.Operation == RetriableOperation.Insert, cancellationToken), cancellationToken);
        }

        async Task<DataModificationOutput> ExecuteSingleDataModificationAsync(PreparedCommand command, CancellationToken cancellationToken)
        {
            var results = await ExecuteDataModificationAsync(command, cancellationToken);
            return results.SingleOrDefault();
        }

        void ApplyNewRowVersionsIfRequired(IReadOnlyList<object> documentList, DocumentMap mapping, DataModificationOutput[] outputs)
        {
            if (!mapping.IsRowVersioningEnabled) return;

            for (var i = 0; i < documentList.Count; i++)
            {
                ApplyNewRowVersionIfRequired(documentList[i], mapping, outputs[i]);
            }
        }

        void ApplyNewRowVersionIfRequired<TDocument>(TDocument document, DocumentMap mapping, DataModificationOutput output) where TDocument : class
        {
            if (!mapping.IsRowVersioningEnabled) return;

            if (output?.RowVersion == null)
                throw new StaleDataException($"Modification failed for '{mapping.Type.Name}' document with '{mapping.GetId(document)}' Id because submitted data was out of date. Refresh the document and try again.");

            mapping.RowVersionColumn!.PropertyHandler.Write(document, output.RowVersion);
        }

        void ApplyIdentityIdsIfRequired(IReadOnlyList<object> documentList, DocumentMap mapping, DataModificationOutput[] outputs)
        {
            if (!mapping.IsIdentityId) return;

            for (var i = 0; i < documentList.Count; i++)
            {
                ApplyIdentityIdsIfRequired(documentList[i], mapping, outputs[i]);
            }
        }

        void ApplyIdentityIdsIfRequired<TDocument>(TDocument document, DocumentMap mapping, DataModificationOutput output) where TDocument : class
        {
            if (!mapping.IsIdentityId) return;

            if (output?.Id == null)
                throw new InvalidOperationException(
                    $"Modification failed for '{typeof(TDocument).Name}' document with '{mapping.GetId(document)}' Id because the server failed to return a new Identity id.");

            mapping.IdColumn!.PropertyHandler.Write(document, output.Id);
        }

        class DataModificationOutput
        {
            public byte[] RowVersion { get; private set; }
            public object Id { get; private set; }

            public static DataModificationOutput Read(DbDataReader reader, DocumentMap map, bool isInsert)
            {
                var output = new DataModificationOutput();

                if (map.IsRowVersioningEnabled)
                    output.RowVersion =
                        reader.GetFieldValue<byte[]>(map.RowVersionColumn!.ColumnName);

                if (map.IsIdentityId && isInsert)
                    output.Id = reader.GetFieldValue<object>(map.IdColumn!.ColumnName);

                return output;
            }

            public static async Task<DataModificationOutput> ReadAsync(DbDataReader reader, DocumentMap map, bool isInsert, CancellationToken cancellationToken)
            {
                var output = new DataModificationOutput();

                if (map.IsRowVersioningEnabled)
                    output.RowVersion =
                        await reader.GetFieldValueAsync<byte[]>(map.RowVersionColumn!.ColumnName, cancellationToken);

                if (map.IsIdentityId && isInsert)
                    output.Id = await reader.GetFieldValueAsync<object>(map.IdColumn!.ColumnName, cancellationToken);

                return output;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Data;
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

            ExecuteNonQuery(command);
            RefreshRowVersionIfRequired(document, command);

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

            await ExecuteNonQueryAsync(command, cancellationToken);
            await RefreshRowVersionIfRequiredAsync(document, command);

            await configuration.Hooks.AfterInsertAsync(document, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, document);
        }

        public void InsertMany<TDocument>(IReadOnlyCollection<TDocument> documents, InsertOptions options = null) where TDocument : class
        {
            IReadOnlyList<object> documentList = documents.ToArray();
            if (!documentList.Any()) return;

            var command = builder.PrepareInsert(documentList, options);
            foreach (var document in documents) configuration.Hooks.BeforeInsert(document, command.Mapping, this);

            ExecuteNonQuery(command);
            foreach (var document in documentList) RefreshRowVersionIfRequired(document, command);

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

            await ExecuteNonQueryAsync(command, cancellationToken);
            foreach (var document in documentList) await RefreshRowVersionIfRequiredAsync(document, command);

            foreach (var document in documentList) await configuration.Hooks.AfterInsertAsync(document, command.Mapping, this);

            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, documentList);
        }

        public void Update<TDocument>(TDocument document, UpdateOptions options = null) where TDocument : class
        {
            var command = builder.PrepareUpdate(document, options);
            configuration.Hooks.BeforeUpdate(document, command.Mapping, this);
            var numberOfUpdatedRows = ExecuteNonQuery(command);

            AssertUpdateHasBeenSuccessful(numberOfUpdatedRows, command);
            RefreshRowVersionIfRequired(document, command);

            configuration.Hooks.AfterUpdate(document, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, document);
        }

        void RefreshRowVersionIfRequired<TDocument>(TDocument document, PreparedCommand command) where TDocument : class
        {
            if (command.Mapping.RowVersionColumn == null) return;

            var mapping = command.Mapping;

            var args = new CommandParameterValues {{"Id", command.Mapping.GetId(document)}};
            var dataVersionCommand = new PreparedCommand($"SELECT TOP 1 [{mapping.RowVersionColumn.ColumnName}]   FROM [{configuration.GetSchemaNameOrDefault(mapping)}].[{mapping.TableName}] WHERE [{mapping.IdColumn.ColumnName}] = @Id", args, RetriableOperation.Select, mapping, commandBehavior: CommandBehavior.SingleResult | CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
            var refreshedRowVersion = ExecuteScalar<object>(dataVersionCommand);

            mapping.RowVersionColumn.PropertyHandler.Write(document, refreshedRowVersion);
        }

        async Task RefreshRowVersionIfRequiredAsync<TDocument>(TDocument document, PreparedCommand command) where TDocument : class
        {
            if (command.Mapping.RowVersionColumn == null) return;

            var mapping = command.Mapping;

            var args = new CommandParameterValues {{"Id", command.Mapping.GetId(document)}};
            var dataVersionCommand = new PreparedCommand($"SELECT TOP 1 {mapping.RowVersionColumn.ColumnName}]   FROM [{configuration.GetSchemaNameOrDefault(mapping)}].[{mapping.TableName}] WHERE [{mapping.IdColumn.ColumnName}] = @Id", args, RetriableOperation.Select, mapping, commandBehavior: CommandBehavior.SingleResult | CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
            var refreshedRowVersion = await ExecuteScalarAsync<object>(dataVersionCommand);

            mapping.RowVersionColumn.PropertyHandler.Write(document, refreshedRowVersion);
        }

        public Task UpdateAsync<TDocument>(TDocument document, CancellationToken cancellationToken = default) where TDocument : class
        {
            return UpdateAsync(document, null, cancellationToken);
        }

        public async Task UpdateAsync<TDocument>(TDocument document, UpdateOptions options, CancellationToken cancellationToken = default) where TDocument : class
        {
            var command = builder.PrepareUpdate(document, options);
            await configuration.Hooks.BeforeUpdateAsync(document, command.Mapping, this);
            var numberOfUpdatedRows = await ExecuteNonQueryAsync(command, cancellationToken);

            AssertUpdateHasBeenSuccessful(numberOfUpdatedRows, command);
            await RefreshRowVersionIfRequiredAsync(document, command);

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

        static void AssertUpdateHasBeenSuccessful(int numberOfUpdatedRows, PreparedCommand command)
        {
            if (numberOfUpdatedRows == 0 && command.Mapping.RowVersionColumn != null) throw new StaleDataException("Update failed because submitted data was out of date. Refresh the data and try again.");
        }

    }
}

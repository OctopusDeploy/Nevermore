using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Mapping;
using Nevermore.Querying;
using Nevermore.Util;
using Newtonsoft.Json.Serialization;

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
            builder = new DataModificationQueryBuilder(configuration.DocumentMaps, configuration.DocumentSerializer, AllocateId);
        }
        
        public void Insert<TDocument>(TDocument instance, InsertOptions options = null) where TDocument : class
        {
            var command = builder.PrepareInsert(new[] {instance}, options);
            configuration.Hooks.BeforeInsert(instance, command.Mapping, this);
            ExecuteNonQuery(command);
            configuration.Hooks.AfterInsert(instance, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, instance);
        }

        public Task InsertAsync<TDocument>(TDocument instance, CancellationToken cancellationToken = default) where TDocument : class
        {
            return InsertAsync(instance, null, cancellationToken);
        }

        public async Task InsertAsync<TDocument>(TDocument instance, InsertOptions options, CancellationToken cancellationToken = default) where TDocument : class
        {
            var command = builder.PrepareInsert(new[] {instance}, options);
            await configuration.Hooks.BeforeInsertAsync(instance, command.Mapping, this);
            await ExecuteNonQueryAsync(command, cancellationToken);
            await configuration.Hooks.AfterInsertAsync(instance, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, instance);
        }

        public void InsertMany<TDocument>(IReadOnlyCollection<TDocument> documents, InsertOptions options = null) where TDocument : class
        {
            IReadOnlyList<object> instanceList = documents.ToArray();
            if (!instanceList.Any()) return;

            var command = builder.PrepareInsert(instanceList, options);
            foreach (var instance in instanceList) configuration.Hooks.BeforeInsert(instance, command.Mapping, this);
            ExecuteNonQuery(command);
            foreach (var instance in instanceList) configuration.Hooks.AfterInsert(instance, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, instanceList);
        }

        public Task InsertManyAsync<TDocument>(IReadOnlyCollection<TDocument> documents, CancellationToken cancellationToken = default) where TDocument : class
        {
            return InsertManyAsync(documents, null, cancellationToken);
        }

        public async Task InsertManyAsync<TDocument>(IReadOnlyCollection<TDocument> documents, InsertOptions options, CancellationToken cancellationToken = default) where TDocument : class
        {
            IReadOnlyList<object> instanceList = documents.ToArray();
            if (!instanceList.Any()) return;

            var command = builder.PrepareInsert(instanceList, options);
            foreach (var instance in instanceList) await configuration.Hooks.BeforeInsertAsync(instance, command.Mapping, this);
            await ExecuteNonQueryAsync(command, cancellationToken);
            foreach (var instance in instanceList) await configuration.Hooks.AfterInsertAsync(instance, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, instanceList);
        }

        public void Update<TDocument>(TDocument document, UpdateOptions options = null) where TDocument : class
        {
            var command = builder.PrepareUpdate(document, options);
            configuration.Hooks.BeforeUpdate(document, command.Mapping, this);
            ExecuteNonQuery(command);
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
            await ExecuteNonQueryAsync(command, cancellationToken);
            await configuration.Hooks.AfterUpdateAsync(document, command.Mapping, this);
        }

        public void Delete<TDocument>(TDocument document, DeleteOptions options = null) where TDocument : class
        {
            var id = configuration.DocumentMaps.GetId(document);
            Delete<TDocument>(id, options);
        }

        public Task DeleteAsync<TDocument>(TDocument document, CancellationToken cancellationToken = default) where TDocument : class
        {
            return DeleteAsync(document, null, cancellationToken);
        }

        public Task DeleteAsync<TDocument>(TDocument document, DeleteOptions options, CancellationToken cancellationToken = default) where TDocument : class
        {
            var id = configuration.DocumentMaps.GetId(document);
            return DeleteAsync<TDocument>(id, options, cancellationToken);
        }

        public void Delete<TDocument>(string id, DeleteOptions options = null) where TDocument : class
        {
            var command = builder.PrepareDelete<TDocument>(id, options);
            configuration.Hooks.BeforeDelete<TDocument>(id, command.Mapping, this);
            ExecuteNonQuery(command);
            configuration.Hooks.AfterDelete<TDocument>(id, command.Mapping, this);
        }

        public Task DeleteAsync<TDocument>(string id, CancellationToken cancellationToken = default) where TDocument : class
        {
            return DeleteAsync(id, null, cancellationToken);
        }

        public async Task DeleteAsync<TDocument>(string id, DeleteOptions options, CancellationToken cancellationToken = default) where TDocument : class
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
    }
}

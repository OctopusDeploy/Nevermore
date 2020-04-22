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
        readonly RelationalStoreConfiguration configuration;
        readonly IKeyAllocator keyAllocator;
        readonly DataModificationQueryBuilder builder;

        public WriteTransaction(
            RelationalTransactionRegistry registry,
            RetriableOperation operationsToRetry,
            RelationalStoreConfiguration configuration,
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
            configuration.HookRegistry.BeforeInsert(instance, command.Mapping, this);
            ExecuteNonQuery(command);
            configuration.HookRegistry.AfterInsert(instance, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, instance);
        }

        public async Task InsertAsync<TDocument>(TDocument instance, InsertOptions options = null, CancellationToken cancellationToken = default) where TDocument : class
        {
            var command = builder.PrepareInsert(new[] {instance}, options);
            await configuration.HookRegistry.BeforeInsertAsync(instance, command.Mapping, this);
            await ExecuteNonQueryAsync(command, cancellationToken);
            await configuration.HookRegistry.AfterInsertAsync(instance, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, instance);
        }

        public void InsertMany<TDocument>(IReadOnlyCollection<TDocument> documents, InsertOptions options = null) where TDocument : class
        {
            IReadOnlyList<object> instanceList = documents.ToArray();
            if (!instanceList.Any()) return;

            var command = builder.PrepareInsert(instanceList, options);
            foreach (var instance in instanceList) configuration.HookRegistry.BeforeInsert(instance, command.Mapping, this);
            ExecuteNonQuery(command);
            foreach (var instance in instanceList) configuration.HookRegistry.AfterInsert(instance, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, instanceList);
        }

        public async Task InsertManyAsync<TDocument>(IReadOnlyCollection<TDocument> documents, InsertOptions options = null, CancellationToken cancellationToken = default) where TDocument : class
        {
            IReadOnlyList<object> instanceList = documents.ToArray();
            if (!instanceList.Any()) return;

            var command = builder.PrepareInsert(instanceList, options);
            foreach (var instance in instanceList) await configuration.HookRegistry.BeforeInsertAsync(instance, command.Mapping, this);
            await ExecuteNonQueryAsync(command, cancellationToken);
            foreach (var instance in instanceList) await configuration.HookRegistry.AfterInsertAsync(instance, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, instanceList);
        }

        public void Update<TDocument>(TDocument document, UpdateOptions options = null) where TDocument : class
        {
            var command = builder.PrepareUpdate(document, options);
            configuration.HookRegistry.BeforeUpdate(document, command.Mapping, this);
            ExecuteNonQuery(command);
            configuration.HookRegistry.AfterUpdate(document, command.Mapping, this);
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, document);
        }

        public async Task UpdateAsync<TDocument>(TDocument document, UpdateOptions options = null, CancellationToken cancellationToken = default) where TDocument : class
        {
            var command = builder.PrepareUpdate(document, options);
            await configuration.HookRegistry.BeforeUpdateAsync(document, command.Mapping, this);
            await ExecuteNonQueryAsync(command, cancellationToken);
            await configuration.HookRegistry.AfterUpdateAsync(document, command.Mapping, this);
        }

        public void Delete<TDocument>(TDocument document, DeleteOptions options = null) where TDocument : class
        {
            var id = configuration.DocumentMaps.GetId(document);
            Delete<TDocument>(id, options);
        }

        public Task DeleteAsync<TDocument>(TDocument document, DeleteOptions options = null, CancellationToken cancellationToken = default) where TDocument : class
        {
            var id = configuration.DocumentMaps.GetId(document);
            return DeleteAsync<TDocument>(id, options, cancellationToken);
        }

        public void Delete<TDocument>(string id, DeleteOptions options = null) where TDocument : class
        {
            var command = builder.PrepareDelete<TDocument>(id, options);
            configuration.HookRegistry.BeforeDelete(id, command.Mapping, this);
            ExecuteNonQuery(command);
            configuration.HookRegistry.AfterDelete(id, command.Mapping, this);
        }

        public async Task DeleteAsync<TDocument>(string id, DeleteOptions options = null, CancellationToken cancellationToken = default) where TDocument : class
        {
            var command = builder.PrepareDelete<TDocument>(id, options);
            await configuration.HookRegistry.BeforeDeleteAsync(id, command.Mapping, this);
            await ExecuteNonQueryAsync(command, cancellationToken);
            await configuration.HookRegistry.AfterDeleteAsync(id, command.Mapping, this);
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
            configuration.HookRegistry.BeforeCommit(this);
            Transaction.Commit();
            configuration.HookRegistry.AfterCommit(this);
        }

        public async Task CommitAsync(CancellationToken cancellationToken = default)
        {
            await configuration.HookRegistry.BeforeCommitAsync(this);
            await Transaction.CommitAsync(cancellationToken);
            await configuration.HookRegistry.AfterCommitAsync(this);
        }
    }
}

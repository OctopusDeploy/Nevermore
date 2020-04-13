using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;
using Nevermore.Util;
using Newtonsoft.Json;

namespace Nevermore
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
            builder = new DataModificationQueryBuilder(configuration.Mappings, configuration.JsonSerializerSettings, AllocateId);
        }
        
        public void Insert<TDocument>(TDocument instance, InsertOptions options = null) where TDocument : class, IId
        {
            ExecuteNonQuery(builder.PrepareInsert(new[] {instance}, options));
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, instance);
        }

        public async Task InsertAsync<TDocument>(TDocument instance, InsertOptions options = null) where TDocument : class, IId
        {
            await ExecuteNonQueryAsync(builder.PrepareInsert(new[] {instance}, options));
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, instance);
        }

        public void InsertMany<TDocument>(IReadOnlyCollection<TDocument> documents, InsertOptions options = null) where TDocument : class, IId
        {
            IReadOnlyList<IId> instanceList = documents.ToArray();
            if (!instanceList.Any()) return;

            ExecuteNonQuery(builder.PrepareInsert(instanceList, options));
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, instanceList);
        }

        public async Task InsertManyAsync<TDocument>(IReadOnlyCollection<TDocument> documents, InsertOptions options = null) where TDocument : class, IId
        {
            IReadOnlyList<IId> instanceList = documents.ToArray();
            if (!instanceList.Any()) return;

            await ExecuteNonQueryAsync(builder.PrepareInsert(instanceList, options));
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, instanceList);
        }

        public void Update<TDocument>(TDocument document, UpdateOptions options = null) where TDocument : class, IId
        {
            ExecuteNonQuery(builder.PrepareUpdate(document, options));
            configuration.RelatedDocumentStore.PopulateRelatedDocuments(this, document);
        }

        public async Task UpdateAsync<TDocument>(TDocument document, UpdateOptions options = null) where TDocument : class, IId
        {
            await ExecuteNonQueryAsync(builder.PrepareUpdate(document, options));
        }

        public void Delete<TDocument>(TDocument document, DeleteOptions options = null) where TDocument : class, IId
        {
            ExecuteNonQuery(builder.PrepareDelete(document, options));
        }

        public async Task DeleteAsync<TDocument>(TDocument document, DeleteOptions options = null) where TDocument : class, IId
        {
            await ExecuteNonQueryAsync(builder.PrepareDelete(document, options));
        }

        public void Delete<TDocument>(string id, DeleteOptions options = null) where TDocument : class, IId
        {
            ExecuteNonQuery(builder.PrepareDelete<TDocument>(id, options));
        }

        public async Task DeleteAsync<TDocument>(string id, DeleteOptions options = null) where TDocument : class, IId
        {
            await ExecuteNonQueryAsync(builder.PrepareDelete<TDocument>(id, options));
        }

        public IDeleteQueryBuilder<TDocument> DeleteQuery<TDocument>() where TDocument : class, IId
        {
            return new DeleteQueryBuilder<TDocument>(uniqueParameterNameGenerator, builder, this);
        }

        public string AllocateId(Type documentType)
        {
            var mapping = configuration.Mappings.Resolve(documentType);
            return AllocateId(mapping);
        }

        string AllocateId(DocumentMap mapping)
        {
            if (!string.IsNullOrEmpty(mapping.SingletonId))
                return mapping.SingletonId;

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
            transaction.Commit();
        }
    }
}

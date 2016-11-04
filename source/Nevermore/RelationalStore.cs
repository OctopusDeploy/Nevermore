using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization.Formatters;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Nevermore
{
    public class RelationalStore : IRelationalStore
    {
        const int DefaultConnectTimeoutSeconds = 60 * 5; // Increase the default connection timeout to try and prevent transaction.Commit() to timeout on slower SQL Servers.

        private readonly ISqlCommandFactory sqlCommandFactory;
        readonly RelationalMappings mappings;
        readonly Lazy<string> connectionString;
        readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings();
        private readonly IRelatedDocumentStore relatedDocumentStore;
        readonly IKeyAllocator keyAllocator;

        public RelationalStore(
            string connectionString,
            string applicationName,
            ISqlCommandFactory sqlCommandFactory,
            RelationalMappings mappings,
            JsonSerializerSettings jsonSettings,
            IRelatedDocumentStore relatedDocumentStore,
            int keyBlockSize = 20)
            : this(
                new Lazy<string>(() => connectionString),
                applicationName,
                sqlCommandFactory,
                mappings,
                jsonSettings,
                relatedDocumentStore,
                keyBlockSize
            )
        {

        }

        /// <summary>
        /// Creates a new Relational Store
        /// </summary>
        /// <param name="connectionString">Allows the connection string to be set after the store is built (but before it is used)</param>
        /// <param name="applicationName">Name of the application in the SQL string</param>
        /// <param name="sqlCommandFactory"></param>
        /// <param name="mappings"></param>
        /// <param name="jsonSettings"></param>
        /// <param name="relatedDocumentStore">If you don't have releated documents use the EmptyRelatedDocumentStore</param>
        /// <param name="keyBlockSize">Block size for the KeyAllocator</param>
        public RelationalStore(
            Lazy<string> connectionString,
            string applicationName,
            ISqlCommandFactory sqlCommandFactory,
            RelationalMappings mappings,
            JsonSerializerSettings jsonSettings,
            IRelatedDocumentStore relatedDocumentStore,
            int keyBlockSize = 20)
        {
            this.connectionString = new Lazy<string>(
                () => SetConnectionStringOptions(connectionString.Value, applicationName)
            );
            this.sqlCommandFactory = sqlCommandFactory;
            this.mappings = mappings;
            keyAllocator = new KeyAllocator(this, keyBlockSize);

            this.jsonSettings = jsonSettings;
            this.relatedDocumentStore = relatedDocumentStore;
        }

        public string ConnectionString => connectionString.Value;

        public void Reset()
        {
            keyAllocator.Reset();
        }

        public IRelationalTransaction BeginTransaction(RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select)
        {
            return BeginTransaction(IsolationLevel.ReadCommitted, retriableOperation);
        }

        public IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select)
        {
            return new RelationalTransaction(ConnectionString, retriableOperation, isolationLevel, sqlCommandFactory, jsonSettings, mappings, keyAllocator, relatedDocumentStore);
        }
  
        static string SetConnectionStringOptions(string connectionString, string applicationName)
        {
            return new SqlConnectionStringBuilder(connectionString)
            {
                MultipleActiveResultSets = true,
                Pooling = true,
                ApplicationName = applicationName,
                ConnectTimeout = DefaultConnectTimeoutSeconds,
                ConnectRetryCount = 3,
                ConnectRetryInterval = 10
            }
            .ToString();
        }
    }
}
using System;
using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;
using Newtonsoft.Json;

namespace Nevermore
{
    public class RelationalStore : IRelationalStore
    {
        public const int DefaultConnectTimeoutSeconds = 60 * 5; // Increase the default connection timeout to try and prevent transaction.Commit() to timeout on slower SQL Servers.
        public const int DefaultConnectRetryCount = 3;
        public const int DefaultConnectRetryInterval = 10;

        private readonly ISqlCommandFactory sqlCommandFactory;
        readonly RelationalMappings mappings;
        readonly Lazy<ConnectionStringInfo> connectionString;
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
                () => connectionString,
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
            Func<string> connectionString,
            string applicationName,
            ISqlCommandFactory sqlCommandFactory,
            RelationalMappings mappings,
            JsonSerializerSettings jsonSettings,
            IRelatedDocumentStore relatedDocumentStore,
            int keyBlockSize = 20)
        {
            this.connectionString = new Lazy<ConnectionStringInfo>(
                () => SetConnectionStringOptions(connectionString(), applicationName)
            );
            this.sqlCommandFactory = sqlCommandFactory;
            this.mappings = mappings;
            keyAllocator = new KeyAllocator(this, keyBlockSize);

            this.jsonSettings = jsonSettings;
            this.relatedDocumentStore = relatedDocumentStore;
        }

        public string ConnectionString => connectionString.Value.ConnectionString;

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
            return new RelationalTransaction(ConnectionString, retriableOperation, isolationLevel, sqlCommandFactory, jsonSettings, mappings, keyAllocator, relatedDocumentStore, connectionString.Value.MaxPoolSize);
        }
  
        static ConnectionStringInfo SetConnectionStringOptions(string connectionString, string applicationName)
        {
            var connectionStringBuilder = new SqlConnectionStringBuilder(connectionString)
            {
                MultipleActiveResultSets = true,
                Pooling = true,
                ApplicationName = applicationName,
            };

            OverrideValueIfNotSet(connectionStringBuilder, nameof(connectionStringBuilder.ConnectTimeout), DefaultConnectTimeoutSeconds);
            OverrideValueIfNotSet(connectionStringBuilder, nameof(connectionStringBuilder.ConnectRetryCount), DefaultConnectRetryCount);
            OverrideValueIfNotSet(connectionStringBuilder, nameof(connectionStringBuilder.ConnectRetryInterval), DefaultConnectRetryInterval);

            return new ConnectionStringInfo(connectionStringBuilder);
        }

        static void OverrideValueIfNotSet(SqlConnectionStringBuilder connectionStringBuilder, string propertyName, object overrideValue)
        {
            var defaultConnectionStringBuilder = new SqlConnectionStringBuilder();

            var property = connectionStringBuilder.GetType().GetRuntimeProperty(propertyName);

            var defaultValue = property.GetValue(defaultConnectionStringBuilder);
            var currentValue = property.GetValue(connectionStringBuilder);

            if (Equals(defaultValue, currentValue))
            {
                property.SetValue(connectionStringBuilder, overrideValue);
            }
        }

        class ConnectionStringInfo
        {
            public ConnectionStringInfo(SqlConnectionStringBuilder builder)
            {
                ConnectionString = builder.ToString();
                MaxPoolSize = builder.MaxPoolSize;
            }

            public string ConnectionString { get; }
            public int MaxPoolSize { get; }
        }
    }
}
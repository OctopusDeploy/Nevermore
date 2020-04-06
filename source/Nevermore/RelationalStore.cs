using System;
using System.Data;
#if NETFRAMEWORK
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;
using Newtonsoft.Json;

namespace Nevermore
{
    public class RelationalStore : IRelationalStore
    {
        readonly ISqlCommandFactory sqlCommandFactory;
        readonly RelationalMappings mappings;
        readonly Lazy<RelationalTransactionRegistry> registry;
        readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings();
        readonly IRelatedDocumentStore relatedDocumentStore;
        readonly IKeyAllocator keyAllocator;
        readonly ObjectInitialisationOptions objectInitialisationOptions;

        /// <summary>
        ///
        /// </summary>
        /// <param name="connectionString"></param>
        /// <param name="applicationName"></param>
        /// <param name="sqlCommandFactory"></param>
        /// <param name="mappings"></param>
        /// <param name="jsonSettings"></param>
        /// <param name="relatedDocumentStore"></param>
        /// <param name="keyBlockSize"></param>
        /// <param name="objectInitialisationOptions"></param>
        /// <param name="forceMars">MARS: https://docs.microsoft.com/en-us/sql/relational-databases/native-client/features/using-multiple-active-result-sets-mars?view=sql-server-ver15</param>
        public RelationalStore(
            string connectionString,
            string applicationName,
            ISqlCommandFactory sqlCommandFactory,
            RelationalMappings mappings,
            JsonSerializerSettings jsonSettings,
            IRelatedDocumentStore relatedDocumentStore,
            int keyBlockSize = 20,
            ObjectInitialisationOptions objectInitialisationOptions = ObjectInitialisationOptions.None,
            bool forceMars = true)
            : this(
                () => connectionString,
                applicationName,
                sqlCommandFactory,
                mappings,
                jsonSettings,
                relatedDocumentStore,
                keyBlockSize,
                objectInitialisationOptions,
                forceMars
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
        /// <param name="objectInitialisationOptions"></param>
        /// <param name="forceMars"></param>
        public RelationalStore(
            Func<string> connectionString,
            string applicationName,
            ISqlCommandFactory sqlCommandFactory,
            RelationalMappings mappings,
            JsonSerializerSettings jsonSettings,
            IRelatedDocumentStore relatedDocumentStore,
            int keyBlockSize = 20,
            ObjectInitialisationOptions objectInitialisationOptions = ObjectInitialisationOptions.None,
            bool forceMars = true)
        {
            registry = new Lazy<RelationalTransactionRegistry>(
                () => SetConnectionStringOptions(connectionString(), applicationName, forceMars)
            );

            this.sqlCommandFactory = sqlCommandFactory;
            this.mappings = mappings;
            keyAllocator = new KeyAllocator(this, keyBlockSize);

            this.jsonSettings = jsonSettings;
            this.relatedDocumentStore = relatedDocumentStore;
            this.objectInitialisationOptions = objectInitialisationOptions;
        }

        public string ConnectionString => registry.Value.ConnectionString;
        public int MaxPoolSize => registry.Value.MaxPoolSize;

        public void WriteCurrentTransactions(StringBuilder sb) => registry.Value.WriteCurrentTransactions(sb);
        public DocumentMap GetMappingFor(Type type) => mappings.Get(type);

        public DocumentMap GetMappingFor<T>() => mappings.Get(typeof(T));

        public void Reset()
        {
            keyAllocator.Reset();
        }

        public IReadTransaction BeginReadTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation, string name)
        {
            var txn = CreateReadTransaction(retriableOperation, name);
            txn.Open(isolationLevel);
            return txn;
        }

        public async Task<IReadTransaction> BeginReadTransactionAsync(IsolationLevel isolationLevel, RetriableOperation retriableOperation, string name)
        {
            var txn = CreateReadTransaction(retriableOperation, name);
            await txn.OpenAsync(isolationLevel);
            return txn;
        }

        public IWriteTransaction BeginWriteTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation, string name)
        {
            var txn = CreateWriteTransaction(retriableOperation, name);
            txn.Open(isolationLevel);
            return txn;
        }

        public async Task<IWriteTransaction> BeginWriteTransactionAsync(IsolationLevel isolationLevel, RetriableOperation retriableOperation, string name)
        {
            var txn = CreateWriteTransaction(retriableOperation, name);
            await txn.OpenAsync(isolationLevel);
            return txn;
        }

        public IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation, string name)
        {
            return (IRelationalTransaction)BeginWriteTransaction(isolationLevel, retriableOperation, name);
        }

        ReadRelationalTransaction CreateReadTransaction(RetriableOperation retriableOperation, string name)
        {
            return new ReadRelationalTransaction(registry.Value, retriableOperation, sqlCommandFactory, jsonSettings, mappings, relatedDocumentStore, name, objectInitialisationOptions);
        }
        
        RelationalTransaction CreateWriteTransaction(RetriableOperation retriableOperation, string name)
        {
            return new RelationalTransaction(registry.Value, retriableOperation, sqlCommandFactory, jsonSettings, mappings, keyAllocator, relatedDocumentStore, name, objectInitialisationOptions);
        }

        static RelationalTransactionRegistry SetConnectionStringOptions(string connectionString, string applicationName, bool forceMars)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            builder.ApplicationName = applicationName;

            if (forceMars) builder.MultipleActiveResultSets = true;

            OverrideValueIfNotSet(builder, nameof(builder.ConnectTimeout), NevermoreDefaults.DefaultConnectTimeoutSeconds);
            OverrideValueIfNotSet(builder, nameof(builder.ConnectRetryCount), NevermoreDefaults.DefaultConnectRetryCount);
            OverrideValueIfNotSet(builder, nameof(builder.ConnectRetryInterval), NevermoreDefaults.DefaultConnectRetryInterval);

            return new RelationalTransactionRegistry(builder);
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
    }
}
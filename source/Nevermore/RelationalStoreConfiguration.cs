using System;
using Microsoft.Data.SqlClient;
using Nevermore.Advanced;
using Nevermore.Advanced.Hooks;
using Nevermore.Advanced.InstanceTypeResolvers;
using Nevermore.Advanced.ReaderStrategies;
using Nevermore.Advanced.ReaderStrategies.AnonymousTypes;
using Nevermore.Advanced.ReaderStrategies.ArbitraryClasses;
using Nevermore.Advanced.ReaderStrategies.Documents;
using Nevermore.Advanced.ReaderStrategies.Primitives;
using Nevermore.Advanced.ReaderStrategies.ValueTuples;
using Nevermore.Advanced.Serialization;
using Nevermore.Advanced.TypeHandlers;
using Nevermore.Diagnostics;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;
using Nevermore.TableColumnNameResolvers;

namespace Nevermore
{
    public class RelationalStoreConfiguration : IRelationalStoreConfiguration
    {
        readonly Lazy<string> connectionString;

#nullable enable
        public RelationalStoreConfiguration(string connectionString, IPrimaryKeyHandlerRegistry? customPrimaryKeyHandlerRegistry = null) : this(() => connectionString, customPrimaryKeyHandlerRegistry)
        {
        }

        public RelationalStoreConfiguration(Func<string> connectionStringFunc, IPrimaryKeyHandlerRegistry? customPrimaryKeyHandlerRegistry = null)
        {
            CommandFactory = new SqlCommandFactory();
            KeyBlockSize = NevermoreDefaults.DefaultKeyBlockSize;
            InstanceTypeResolvers = new InstanceTypeRegistry();
            RelatedDocumentStore = new EmptyRelatedDocumentStore();

            this.UseJsonNetSerialization(s => {});

            ReaderStrategies = new ReaderStrategyRegistry();
            ReaderStrategies.Register(new DocumentReaderStrategy(this));
            ReaderStrategies.Register(new ValueTupleReaderStrategy(this));
            ReaderStrategies.Register(new AnonymousTypeReaderStrategy(this));
            ReaderStrategies.Register(new PrimitiveReaderStrategy(this));
            ReaderStrategies.Register(new ArbitraryClassReaderStrategy(this));

            Hooks = new HookRegistry();

            DefaultSchema = NevermoreDefaults.FallbackDefaultSchemaName;

            TypeHandlers = new TypeHandlerRegistry();

            PrimaryKeyHandlers = customPrimaryKeyHandlerRegistry ?? new PrimaryKeyHandlerRegistry();

            DocumentMaps = new DocumentMapRegistry(PrimaryKeyHandlers);
            TableNameResolver = new TableNameResolver(DocumentMaps);

            TableColumnNameResolver = _ => new SelectAllColumnsTableResolver();

            AllowSynchronousOperations = true;
            ConcurrencyMode = ConcurrencyMode.LockOnly;

            QueryLogger = new DefaultQueryLogger();
            TransactionLogger = new DefaultTransactionLogger();

            connectionString = new Lazy<string>(() =>
            {
                var result = connectionStringFunc();
                return InitializeConnectionString(result);
            });
        }
#nullable disable

        public string ApplicationName { get; set; }

        public string ConnectionString => connectionString.Value;

        public bool AllowSynchronousOperations { get; set; }

        public string DefaultSchema { get; set; }

        public IDocumentMapRegistry DocumentMaps { get; set; }
        
        public Func<IRelationalStore, ITableColumnNameResolver> TableColumnNameResolver { get; set; }

        public IDocumentSerializer DocumentSerializer { get; set; }

        public IRelatedDocumentStore RelatedDocumentStore { get; set; }

        public IQueryLogger QueryLogger { get; set; }

        public ITransactionLogger TransactionLogger { get; set; }

        public IRelationalTransactionRegistry RelationalTransactionRegistry { get; set; }

        public IHookRegistry Hooks { get; }
        public int KeyBlockSize { get; set; }

        public IReaderStrategyRegistry ReaderStrategies { get; }

        public ITypeHandlerRegistry TypeHandlers { get; }
        public IInstanceTypeRegistry InstanceTypeResolvers { get; }

        public IPrimaryKeyHandlerRegistry PrimaryKeyHandlers { get; }

        /// <summary>
        /// MARS: https://docs.microsoft.com/en-us/sql/relational-databases/native-client/features/using-multiple-active-result-sets-mars?view=sql-server-ver15
        /// </summary>
        public bool ForceMultipleActiveResultSets { get; set; }

        /// <summary>
        /// MARS: https://docs.microsoft.com/en-us/sql/relational-databases/native-client/features/using-multiple-active-result-sets-mars?view=sql-server-ver15
        /// </summary>
        public bool DetectQueryPlanThrashing { get; set; }

        public bool SupportLargeNumberOfRelatedDocuments { get; set; }

        public ISqlCommandFactory CommandFactory { get; set; }

        public Func<IKeyAllocator> KeyAllocatorFactory { get; set; }
        
        public ITableNameResolver TableNameResolver { get; set; }
        
        public ConcurrencyMode ConcurrencyMode { get; set; }

        string InitializeConnectionString(string sqlConnectionString)
        {
            var builder = new SqlConnectionStringBuilder(sqlConnectionString);
            if (ApplicationName != null) builder.ApplicationName = ApplicationName;
            if (ForceMultipleActiveResultSets) builder.MultipleActiveResultSets = true;


            builder.OverrideConnectionStringPropertyValueIfNotSet(DbConnectionStringKeyword.ConnectTimeout, NevermoreDefaults.DefaultConnectTimeoutSeconds);
            builder.OverrideConnectionStringPropertyValueIfNotSet(DbConnectionStringKeyword.ConnectRetryCount, NevermoreDefaults.DefaultConnectRetryCount);
            builder.OverrideConnectionStringPropertyValueIfNotSet(DbConnectionStringKeyword.ConnectRetryInterval, NevermoreDefaults.DefaultConnectRetryInterval);
            builder.OverrideConnectionStringPropertyValueIfNotSet(DbConnectionStringKeyword.TrustServerCertificate, NevermoreDefaults.DefaultTrustServerCertificate);

            return builder.ToString();
        }

    }
}
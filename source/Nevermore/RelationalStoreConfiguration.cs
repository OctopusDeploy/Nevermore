using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Nevermore.Advanced;
using Nevermore.Advanced.Hooks;
using Nevermore.Advanced.InstanceTypeResolvers;
using Nevermore.Advanced.ReaderStrategies;
using Nevermore.Advanced.ReaderStrategies.ArbitraryClasses;
using Nevermore.Advanced.ReaderStrategies.Documents;
using Nevermore.Advanced.ReaderStrategies.Primitives;
using Nevermore.Advanced.ReaderStrategies.ValueTuples;
using Nevermore.Advanced.Serialization;
using Nevermore.Advanced.TypeHandlers;
using Nevermore.Diagnostics;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;

namespace Nevermore
{

    public class CacheTableColumnsBuilder
    {
        readonly IRelationalStore store;
        readonly ConcurrentDictionary<string, List<string>> mappingColumnNamesSortedWithJsonLastCache = new();

        public CacheTableColumnsBuilder(IRelationalStore store)
        {
            this.store = store;
        }
        
        public IEnumerable<string> GetMappingTableColumnNamesSortedWithJsonLast(string schemaName, string tableName)
        {
            var key = $"{schemaName}.{tableName}";
            if (mappingColumnNamesSortedWithJsonLastCache.ContainsKey(key))
            {
                return mappingColumnNamesSortedWithJsonLastCache[key];
            }
            
            //load time
            using var transaction = store.BeginTransaction();
            var getColumnNamesWithJsonLastQuery = @$"
SELECT c.name
FROM sys.tables AS t
INNER JOIN sys.all_columns AS c ON c.object_id = t.object_id
WHERE t.name = '{tableName}'
ORDER BY (CASE WHEN c.name = 'JSON' THEN 1 ELSE 0 END) ASC, c.column_id
";
            var columnNames = transaction.Stream<string>(getColumnNamesWithJsonLastQuery).ToList();
            mappingColumnNamesSortedWithJsonLastCache.TryAdd(key, columnNames);

            return columnNames;
        }
    }
    
    public class RelationalStoreConfiguration : IRelationalStoreConfiguration
    {
        readonly Lazy<string> connectionString;

        public RelationalStoreConfiguration(string connectionString) : this(() => connectionString)
        {
        }

        public RelationalStoreConfiguration(Func<string> connectionStringFunc)
        {
            CommandFactory = new SqlCommandFactory();
            KeyBlockSize = NevermoreDefaults.DefaultKeyBlockSize;
            InstanceTypeResolvers = new InstanceTypeRegistry();
            RelatedDocumentStore = new EmptyRelatedDocumentStore();

            this.UseJsonNetSerialization(s => {});

            ReaderStrategies = new ReaderStrategyRegistry();
            ReaderStrategies.Register(new DocumentReaderStrategy(this));
            ReaderStrategies.Register(new ValueTupleReaderStrategy(this));
            ReaderStrategies.Register(new ArbitraryClassReaderStrategy(this));
            ReaderStrategies.Register(new PrimitiveReaderStrategy(this));

            Hooks = new HookRegistry();

            DefaultSchema = NevermoreDefaults.FallbackDefaultSchemaName;

            TypeHandlers = new TypeHandlerRegistry();

            PrimaryKeyHandlers = new PrimaryKeyHandlerRegistry();

            DocumentMaps = new DocumentMapRegistry(PrimaryKeyHandlers);

            CacheTableColumns = new CacheTableColumnsBuilder(new RelationalStore(this));

            AllowSynchronousOperations = true;

            QueryLogger = new DefaultQueryLogger();

            connectionString = new Lazy<string>(() =>
            {
                var result = connectionStringFunc();
                return InitializeConnectionString(result);
            });
        }

        public string ApplicationName { get; set; }

        public string ConnectionString => connectionString.Value;

        public bool AllowSynchronousOperations { get; set; }

        public string DefaultSchema { get; set; }

        public IDocumentMapRegistry DocumentMaps { get; set; }
        public CacheTableColumnsBuilder CacheTableColumns { get; }

        public IDocumentSerializer DocumentSerializer { get; set; }

        public IRelatedDocumentStore RelatedDocumentStore { get; set; }

        public IQueryLogger QueryLogger { get; set; }

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
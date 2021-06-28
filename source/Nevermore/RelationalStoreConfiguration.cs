using System;
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
    public class RelationalStoreConfiguration : IRelationalStoreConfiguration
    {
        readonly Lazy<string> connectionString;

        public RelationalStoreConfiguration(string connectionString) : this(() => connectionString)
        {
        }

        public RelationalStoreConfiguration(Func<string> connectionStringFunc)
        {
            CommandFactory = new SqlCommandFactory();
            DocumentMaps = new DocumentMapRegistry();
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

            AllowSynchronousOperations = true;

            QueryLogger = new DefaultQueryLogger();

            RelatedDocumentsGlobalTempTableNameGenerator = () => Guid.NewGuid().ToString("N");
            RelatedDocumentsDatabaseCollation = "SQL_Latin1_General_CP1_CS_AS";

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

        public IDocumentSerializer DocumentSerializer { get; set; }

        public IRelatedDocumentStore RelatedDocumentStore { get; set; }

        public IQueryLogger QueryLogger { get; set; }

        public IHookRegistry Hooks { get; }
        public int KeyBlockSize { get; set; }

        public IReaderStrategyRegistry ReaderStrategies { get; }

        public ITypeHandlerRegistry TypeHandlers { get; }
        public IInstanceTypeRegistry InstanceTypeResolvers { get; }

        /// <summary>
        /// MARS: https://docs.microsoft.com/en-us/sql/relational-databases/native-client/features/using-multiple-active-result-sets-mars?view=sql-server-ver15
        /// </summary>
        public bool ForceMultipleActiveResultSets { get; set; }

        /// <summary>
        /// MARS: https://docs.microsoft.com/en-us/sql/relational-databases/native-client/features/using-multiple-active-result-sets-mars?view=sql-server-ver15
        /// </summary>
        public bool DetectQueryPlanThrashing { get; set; }

        public ISqlCommandFactory CommandFactory { get; set; }

        public Func<string> RelatedDocumentsGlobalTempTableNameGenerator { get; set; }

        public string RelatedDocumentsDatabaseCollation { get; set; }

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
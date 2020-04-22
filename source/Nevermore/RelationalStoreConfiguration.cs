using System;
using System.Reflection;
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
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
            
            HookRegistry = new HookRegistry();

            TypeHandlers = new TypeHandlerRegistry();

            connectionString = new Lazy<string>(() =>
            {
                var result = connectionStringFunc();
                return InitializeConnectionString(result);
            });
        }
        
        public string ApplicationName { get; set; }

        public string ConnectionString => connectionString.Value;

        public IDocumentMapRegistry DocumentMaps { get; }
        
        public IDocumentSerializer DocumentSerializer { get; set; }
        
        public IRelatedDocumentStore RelatedDocumentStore { get; set; }
        
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
        public IHookRegistry HookRegistry { get; private set; }

        string InitializeConnectionString(string sqlConnectionString)
        {
            var builder = new SqlConnectionStringBuilder(sqlConnectionString);
            if (ApplicationName != null) builder.ApplicationName = ApplicationName;
            if (ForceMultipleActiveResultSets) builder.MultipleActiveResultSets = true;

            OverrideValueIfNotSet(builder, nameof(builder.ConnectTimeout), NevermoreDefaults.DefaultConnectTimeoutSeconds);
            OverrideValueIfNotSet(builder, nameof(builder.ConnectRetryCount), NevermoreDefaults.DefaultConnectRetryCount);
            OverrideValueIfNotSet(builder, nameof(builder.ConnectRetryInterval), NevermoreDefaults.DefaultConnectRetryInterval);

            return builder.ToString();
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
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Serialization.Formatters;
using DbUp;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Nevermore
{
    public class RelationalStore : IRelationalStore
    {
        readonly RelationalMappings mappings;
        readonly string connectionString;
        readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings();
        readonly KeyAllocator keyAllocator;

        public RelationalStore(string connectionString,
            string applicationName,
            RelationalMappings mappings,
            IContractResolver contractResolver,
            IEnumerable<JsonConverter> converters,
            JsonSerializerSettings jsonSettings = null,
            KeyAllocator keyAllocator = null)
        {
            this.connectionString = SetConnectionStringOptions(connectionString, applicationName);
            this.mappings = mappings;
            this.keyAllocator = keyAllocator ?? new KeyAllocator(this, 20);

            jsonSettings = jsonSettings ?? SetJsonSerializerSettings(contractResolver);
            jsonSettings.Converters.Add(new StringEnumConverter());
            jsonSettings.Converters.Add(new VersionConverter());
            foreach (var converter in converters)
            {
                jsonSettings.Converters.Add(converter);
            }

            RunMigrations();
        }

        private void RunMigrations()
        {
            var upgrader =
                DeployChanges.To
                    .SqlDatabase(ConnectionString)
                    .WithScriptsAndCodeEmbeddedInAssembly(typeof (RelationalStore).Assembly)
                    .LogScriptOutput()
                    .WithVariable("databaseName", new SqlConnectionStringBuilder(ConnectionString).InitialCatalog)
                    .Build();

            var result = upgrader.PerformUpgrade();

            if (!result.Successful)
            {
                throw new Exception("Database migration failed: " + result.Error.GetErrorSummary(), result.Error);
            }

        }

        public string ConnectionString
        {
            get { return connectionString; }
        }

        public void Reset()
        {
            keyAllocator.Reset();
        }

        public IRelationalTransaction BeginTransaction()
        {
            return BeginTransaction(IsolationLevel.ReadCommitted);
        }

        public IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return new RelationalTransaction(connectionString, isolationLevel, jsonSettings, mappings, keyAllocator);
        }

        static JsonSerializerSettings SetJsonSerializerSettings(IContractResolver contractResolver)
        {
            return new JsonSerializerSettings
            {
                ContractResolver = contractResolver,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind,
                TypeNameHandling = TypeNameHandling.Auto,
                TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple
            };
        }

        static string SetConnectionStringOptions(string connectionString, string applicationName)
        {
            var builder = new SqlConnectionStringBuilder(connectionString)
            {
                MultipleActiveResultSets = true,
                Enlist = false,
                Pooling = true,
                ApplicationName = applicationName
            };
            return builder.ToString();
        }
    }
}
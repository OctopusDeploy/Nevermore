using System.Data;
using System.Data.SqlClient;
using System.Reflection;
using System.Runtime.Serialization.Formatters;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Nevermore
{
    public class RelationalStore : IRelationalStore
    {
        readonly RelationalMappings mappings;
        readonly string connectionString;
        readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings();

        public RelationalStore(string connectionString, RelationalMappings mappings)
        {
            this.mappings = mappings;
            this.connectionString = SetConnectionStringOptions(connectionString, ""); //TODO : Pass app name in

            jsonSettings.ContractResolver = new RelationalJsonContractResolver(mappings);
            jsonSettings.Converters.Add(new StringEnumConverter());
            jsonSettings.Converters.Add(new VersionConverter());
            jsonSettings.DateFormatHandling = DateFormatHandling.IsoDateFormat;
            jsonSettings.DateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
            jsonSettings.TypeNameHandling = TypeNameHandling.Auto;
            jsonSettings.TypeNameAssemblyFormat = FormatterAssemblyStyle.Simple;
        }

        public string ConnectionString
        {
            get { return connectionString; }
        }

        public IRelationalTransaction BeginTransaction()
        {
            return BeginTransaction(IsolationLevel.ReadCommitted);
        }

        public IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel)
        {
            return new RelationalTransaction(connectionString, isolationLevel, jsonSettings, mappings);
        }

        static string SetConnectionStringOptions(string connectionString, string applicationName)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            builder.MultipleActiveResultSets = true;
            builder.Enlist = false;
            builder.Pooling = true;
            builder.ApplicationName = applicationName;
            return builder.ToString();
        }
    }
}
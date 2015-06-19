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
        readonly KeyAllocator keyAllocator;

        public RelationalStore(string connectionString, RelationalMappings mappings, IMasterKeyEncryption masterKey)
        {
            this.mappings = mappings;
            this.connectionString = SetConnectionStringOptions(connectionString);
            keyAllocator = new KeyAllocator(this, 20);

            jsonSettings.ContractResolver = new RelationalJsonContractResolver(mappings, masterKey);
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

        static string SetConnectionStringOptions(string connectionString)
        {
            var builder = new SqlConnectionStringBuilder(connectionString);
            builder.MultipleActiveResultSets = true;
            builder.Enlist = false;
            builder.Pooling = true;
            builder.ApplicationName = "Octopus " + Assembly.GetExecutingAssembly().GetFileVersion();
            return builder.ToString();
        }
    }
}
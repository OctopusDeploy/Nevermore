using Microsoft.Data.SqlClient;
using Octopus.TinyTypes;

namespace Nevermore.Advanced
{
    public static class SqlConnectionStringHelpers
    {
        public static void OverrideValueIfNotSet(SqlConnectionStringBuilder connectionStringBuilder, DbConnectionStringKeyword propertyName, object overrideValue)
        {
            if (!connectionStringBuilder.ShouldSerialize(propertyName.Value))
            {
                connectionStringBuilder[propertyName.Value] = overrideValue;
            }
        }
    }

    public class DbConnectionStringKeyword : TinyType<string>
    {
        public DbConnectionStringKeyword(string value) : base(value)
        {
        }
    }

    // https://github.com/dotnet/SqlClient/blob/5c5a15d5ac842b48c4e99bff951b026f07f1a5d3/src/Microsoft.Data.SqlClient/netcore/src/Microsoft/Data/Common/DbConnectionStringCommon.cs#L885
    // Updated to use TinyTypes
    public static class DbConnectionStringKeywords
    {
        // all
        // internal static readonly DbConnectionStringKeyword NamedConnection = new DbConnectionStringKeyword("Named Connection");

        // SqlClient
        internal static readonly DbConnectionStringKeyword ApplicationIntent = new DbConnectionStringKeyword("Application Intent");
        internal static readonly DbConnectionStringKeyword ApplicationName = new DbConnectionStringKeyword("Application Name");
        internal static readonly DbConnectionStringKeyword AsynchronousProcessing = new DbConnectionStringKeyword("Asynchronous Processing");
        internal static readonly DbConnectionStringKeyword AttachDBFilename = new DbConnectionStringKeyword("AttachDbFilename");
        internal static readonly DbConnectionStringKeyword CommandTimeout = new DbConnectionStringKeyword("Command Timeout");
        internal static readonly DbConnectionStringKeyword ConnectTimeout = new DbConnectionStringKeyword("Connect Timeout");
        internal static readonly DbConnectionStringKeyword ConnectionReset = new DbConnectionStringKeyword("Connection Reset");
        internal static readonly DbConnectionStringKeyword ContextConnection = new DbConnectionStringKeyword("Context Connection");
        internal static readonly DbConnectionStringKeyword CurrentLanguage = new DbConnectionStringKeyword("Current Language");
        internal static readonly DbConnectionStringKeyword Encrypt = new DbConnectionStringKeyword("Encrypt");
        internal static readonly DbConnectionStringKeyword FailoverPartner = new DbConnectionStringKeyword("Failover Partner");
        internal static readonly DbConnectionStringKeyword InitialCatalog = new DbConnectionStringKeyword("Initial Catalog");
        internal static readonly DbConnectionStringKeyword MultipleActiveResultSets = new DbConnectionStringKeyword("Multiple Active Result Sets");
        internal static readonly DbConnectionStringKeyword MultiSubnetFailover = new DbConnectionStringKeyword("Multi Subnet Failover");
        internal static readonly DbConnectionStringKeyword NetworkLibrary = new DbConnectionStringKeyword("Network Library");
        internal static readonly DbConnectionStringKeyword PacketSize = new DbConnectionStringKeyword("Packet Size");
        internal static readonly DbConnectionStringKeyword Replication = new DbConnectionStringKeyword("Replication");
        internal static readonly DbConnectionStringKeyword TransactionBinding = new DbConnectionStringKeyword("Transaction Binding");
        internal static readonly DbConnectionStringKeyword TrustServerCertificate = new DbConnectionStringKeyword("Trust Server Certificate");
        internal static readonly DbConnectionStringKeyword TypeSystemVersion = new DbConnectionStringKeyword("Type System Version");
        internal static readonly DbConnectionStringKeyword UserInstance = new DbConnectionStringKeyword("User Instance");
        internal static readonly DbConnectionStringKeyword WorkstationID = new DbConnectionStringKeyword("Workstation ID");
        internal static readonly DbConnectionStringKeyword ConnectRetryCount = new DbConnectionStringKeyword("Connect Retry Count");
        internal static readonly DbConnectionStringKeyword ConnectRetryInterval = new DbConnectionStringKeyword("Connect Retry Interval");
        internal static readonly DbConnectionStringKeyword Authentication = new DbConnectionStringKeyword("Authentication");
        internal static readonly DbConnectionStringKeyword ColumnEncryptionSetting = new DbConnectionStringKeyword("Column Encryption Setting");
        internal static readonly DbConnectionStringKeyword EnclaveAttestationUrl = new DbConnectionStringKeyword("Enclave Attestation Url");
        internal static readonly DbConnectionStringKeyword AttestationProtocol = new DbConnectionStringKeyword("Attestation Protocol");
        internal static readonly DbConnectionStringKeyword IPAddressPreference = new DbConnectionStringKeyword("IP Address Preference");

        // common keywords (OleDb, OracleClient, SqlClient)
        internal static readonly DbConnectionStringKeyword DataSource = new DbConnectionStringKeyword("Data Source");
        internal static readonly DbConnectionStringKeyword IntegratedSecurity = new DbConnectionStringKeyword("Integrated Security");
        internal static readonly DbConnectionStringKeyword Password = new DbConnectionStringKeyword("Password");
        internal static readonly DbConnectionStringKeyword Driver = new DbConnectionStringKeyword("Driver");
        internal static readonly DbConnectionStringKeyword PersistSecurityInfo = new DbConnectionStringKeyword("Persist Security Info");
        internal static readonly DbConnectionStringKeyword UserID = new DbConnectionStringKeyword("User ID");

        // managed pooling (OracleClient, SqlClient)
        internal static readonly DbConnectionStringKeyword Enlist = new DbConnectionStringKeyword("Enlist");
        internal static readonly DbConnectionStringKeyword LoadBalanceTimeout = new DbConnectionStringKeyword("Load Balance Timeout");
        internal static readonly DbConnectionStringKeyword MaxPoolSize = new DbConnectionStringKeyword("Max Pool Size");
        internal static readonly DbConnectionStringKeyword Pooling = new DbConnectionStringKeyword("Pooling");
        internal static readonly DbConnectionStringKeyword MinPoolSize = new DbConnectionStringKeyword("Min Pool Size");
#if NETCOREAPP
        internal static readonly DbConnectionStringKeyword PoolBlockingPeriod = new DbConnectionStringKeyword("Pool Blocking Period");
#endif
    }
}
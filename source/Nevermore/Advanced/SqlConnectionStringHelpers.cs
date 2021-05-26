using Microsoft.Data.SqlClient;

namespace Nevermore.Advanced
{
    public static class SqlConnectionStringBuilderExtensions
    {
        // Extension method for SqlConnectionStringBuilder to override values
        public static void OverrideConnectionStringPropertyValueIfNotSet(this SqlConnectionStringBuilder connectionStringBuilder, DbConnectionStringKeyword propertyName, object overrideValue)
        {
            if (!connectionStringBuilder.ShouldSerialize(propertyName.Value))
            {
                connectionStringBuilder[propertyName.Value] = overrideValue;
            }
        }
    }

    public class DbConnectionStringKeyword
    {

        public string Value { get; }
        public DbConnectionStringKeyword(string value)
        {
            Value = value;
        }

        // https://github.com/dotnet/SqlClient/blob/5c5a15d5ac842b48c4e99bff951b026f07f1a5d3/src/Microsoft.Data.SqlClient/netcore/src/Microsoft/Data/Common/DbConnectionStringCommon.cs#L885
        // all
        // public static readonly DbConnectionStringKeyword NamedConnection = new DbConnectionStringKeyword("Named Connection");

        // SqlClient
        public static readonly DbConnectionStringKeyword ApplicationIntent = new DbConnectionStringKeyword("Application Intent");
        public static readonly DbConnectionStringKeyword ApplicationName = new DbConnectionStringKeyword("Application Name");
        public static readonly DbConnectionStringKeyword AsynchronousProcessing = new DbConnectionStringKeyword("Asynchronous Processing");
        public static readonly DbConnectionStringKeyword AttachDBFilename = new DbConnectionStringKeyword("AttachDbFilename");
        public static readonly DbConnectionStringKeyword CommandTimeout = new DbConnectionStringKeyword("Command Timeout");
        public static readonly DbConnectionStringKeyword ConnectTimeout = new DbConnectionStringKeyword("Connect Timeout");
        public static readonly DbConnectionStringKeyword ConnectionReset = new DbConnectionStringKeyword("Connection Reset");
        public static readonly DbConnectionStringKeyword ContextConnection = new DbConnectionStringKeyword("Context Connection");
        public static readonly DbConnectionStringKeyword CurrentLanguage = new DbConnectionStringKeyword("Current Language");
        public static readonly DbConnectionStringKeyword Encrypt = new DbConnectionStringKeyword("Encrypt");
        public static readonly DbConnectionStringKeyword FailoverPartner = new DbConnectionStringKeyword("Failover Partner");
        public static readonly DbConnectionStringKeyword InitialCatalog = new DbConnectionStringKeyword("Initial Catalog");
        public static readonly DbConnectionStringKeyword MultipleActiveResultSets = new DbConnectionStringKeyword("Multiple Active Result Sets");
        public static readonly DbConnectionStringKeyword MultiSubnetFailover = new DbConnectionStringKeyword("Multi Subnet Failover");
        public static readonly DbConnectionStringKeyword NetworkLibrary = new DbConnectionStringKeyword("Network Library");
        public static readonly DbConnectionStringKeyword PacketSize = new DbConnectionStringKeyword("Packet Size");
        public static readonly DbConnectionStringKeyword Replication = new DbConnectionStringKeyword("Replication");
        public static readonly DbConnectionStringKeyword TransactionBinding = new DbConnectionStringKeyword("Transaction Binding");
        public static readonly DbConnectionStringKeyword TrustServerCertificate = new DbConnectionStringKeyword("Trust Server Certificate");
        public static readonly DbConnectionStringKeyword TypeSystemVersion = new DbConnectionStringKeyword("Type System Version");
        public static readonly DbConnectionStringKeyword UserInstance = new DbConnectionStringKeyword("User Instance");
        public static readonly DbConnectionStringKeyword WorkstationID = new DbConnectionStringKeyword("Workstation ID");
        public static readonly DbConnectionStringKeyword ConnectRetryCount = new DbConnectionStringKeyword("Connect Retry Count");
        public static readonly DbConnectionStringKeyword ConnectRetryInterval = new DbConnectionStringKeyword("Connect Retry Interval");
        public static readonly DbConnectionStringKeyword Authentication = new DbConnectionStringKeyword("Authentication");
        public static readonly DbConnectionStringKeyword ColumnEncryptionSetting = new DbConnectionStringKeyword("Column Encryption Setting");
        public static readonly DbConnectionStringKeyword EnclaveAttestationUrl = new DbConnectionStringKeyword("Enclave Attestation Url");
        public static readonly DbConnectionStringKeyword AttestationProtocol = new DbConnectionStringKeyword("Attestation Protocol");
        public static readonly DbConnectionStringKeyword IPAddressPreference = new DbConnectionStringKeyword("IP Address Preference");

        // common keywords (OleDb, OracleClient, SqlClient)
        public static readonly DbConnectionStringKeyword DataSource = new DbConnectionStringKeyword("Data Source");
        public static readonly DbConnectionStringKeyword IntegratedSecurity = new DbConnectionStringKeyword("Integrated Security");
        public static readonly DbConnectionStringKeyword Password = new DbConnectionStringKeyword("Password");
        public static readonly DbConnectionStringKeyword Driver = new DbConnectionStringKeyword("Driver");
        public static readonly DbConnectionStringKeyword PersistSecurityInfo = new DbConnectionStringKeyword("Persist Security Info");
        public static readonly DbConnectionStringKeyword UserID = new DbConnectionStringKeyword("User ID");

        // managed pooling (OracleClient, SqlClient)
        public static readonly DbConnectionStringKeyword Enlist = new DbConnectionStringKeyword("Enlist");
        public static readonly DbConnectionStringKeyword LoadBalanceTimeout = new DbConnectionStringKeyword("Load Balance Timeout");
        public static readonly DbConnectionStringKeyword MaxPoolSize = new DbConnectionStringKeyword("Max Pool Size");
        public static readonly DbConnectionStringKeyword Pooling = new DbConnectionStringKeyword("Pooling");
        public static readonly DbConnectionStringKeyword MinPoolSize = new DbConnectionStringKeyword("Min Pool Size");
#if NETCOREAPP
        public static readonly DbConnectionStringKeyword PoolBlockingPeriod = new DbConnectionStringKeyword("Pool Blocking Period");
#endif
    }

}

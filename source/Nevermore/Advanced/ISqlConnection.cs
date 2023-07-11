using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Nevermore.Advanced
{
    public interface ISqlConnection : IDisposable
    {
        // deliberately don't expose the "Sql" specific types so this interface can participate in mocking
        DbConnection Connection { get; }
        ConnectionState State { get; }
        void Open();
        Task OpenAsync();
        DbTransaction BeginTransaction(IsolationLevel iso, string transactionName);
    }

    public class DefaultSqlConnection : ISqlConnection
    {
        readonly SqlConnection connection;

        public DefaultSqlConnection(SqlConnection connection)
        {
            this.connection = connection;
        }

        public DbConnection Connection => connection;

        public ConnectionState State => connection.State;

        public void Open() => connection.Open();
        public Task OpenAsync() => connection.OpenAsync();

        public DbTransaction BeginTransaction(IsolationLevel iso, string transactionName) => connection.BeginTransaction(iso, transactionName);

        public void Dispose() => Connection.Dispose();
    }
}
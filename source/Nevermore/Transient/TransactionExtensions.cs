using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;


namespace Nevermore.Transient
{
    public static class TransactionExtensions
    {
        public static DbTransaction BeginTransactionWithRetry(this SqlConnection connection, IsolationLevel isolationLevel, string sqlServerTransactionName)
        {
            return BeginTransactionWithRetry(connection, isolationLevel, sqlServerTransactionName, RetryManager.Instance.GetDefaultSqlTransactionRetryPolicy());
        }

        public static DbTransaction BeginTransactionWithRetry(this SqlConnection connection, IsolationLevel isolationLevel, string sqlServerTransactionName, RetryPolicy retryPolicy)
        {
            return (retryPolicy ?? RetryPolicy.NoRetry).LoggingRetries("Beginning Database Transaction").ExecuteAction(() => connection.BeginTransaction());
        }
    }
}
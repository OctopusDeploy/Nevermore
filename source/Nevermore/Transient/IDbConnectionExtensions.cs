using System;
using System.Data;

namespace Nevermore.Transient
{
    // ReSharper disable once InconsistentNaming
    public static class IDbConnectionExtensions
    {
        public static void OpenWithRetry(this IDbConnection connection)
        {
            OpenWithRetry(connection, RetryManager.Instance.GetDefaultSqlConnectionRetryPolicy());
        }

        public static void OpenWithRetry(this IDbConnection connection, RetryPolicy retryPolicy)
        {
            (retryPolicy ?? RetryPolicy.NoRetry).LoggingRetries("Open Database Connection").ExecuteAction(connection.Open);
        }
    }
}
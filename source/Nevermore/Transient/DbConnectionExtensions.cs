using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Transient
{
    // ReSharper disable once InconsistentNaming
    public static class DbConnectionExtensions
    {
        public static void OpenWithRetry(this DbConnection connection)
        {
            OpenWithRetry(connection, RetryManager.Instance.GetDefaultSqlConnectionRetryPolicy());
        }

        public static void OpenWithRetry(this DbConnection connection, RetryPolicy retryPolicy)
        {
            (retryPolicy ?? RetryPolicy.NoRetry).LoggingRetries("Open Database Connection").ExecuteAction(connection.Open);
        }
        
        public static Task OpenWithRetryAsync(this DbConnection connection, CancellationToken cancellationToken = default)
        {
            return OpenWithRetryAsync(connection, RetryManager.Instance.GetDefaultSqlConnectionRetryPolicy(), cancellationToken);
        }

        public static Task OpenWithRetryAsync(this DbConnection connection, RetryPolicy retryPolicy, CancellationToken cancellationToken = default)
        {
            return (retryPolicy ?? RetryPolicy.NoRetry).LoggingRetries("Open Database Connection").ExecuteActionAsync(() => connection.OpenAsync(cancellationToken));
        }
    }
}
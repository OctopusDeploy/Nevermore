using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Nevermore.Transient
{
    // ReSharper disable once InconsistentNaming
    public static class DbConnectionExtensions
    {
        public static void OpenWithRetry(this DbConnection connection)
        {
            OpenWithRetry(connection, RetryManager.Instance.GetDefaultSqlConnectionRetryPolicy());
        }

        public static void OpenWithRetry(this DbConnection connection, RetryPolicy retryPolicy, ILogger logger = null)
        {
            var policy = retryPolicy ?? RetryPolicy.NoRetry;
            if (logger is not null)
            {
                policy = policy.LoggingRetries(logger, "Open Database Connection");
            }
            policy.ExecuteAction(connection.Open);
        }

        public static Task OpenWithRetryAsync(this DbConnection connection)
        {
            return OpenWithRetryAsync(connection, RetryManager.Instance.GetDefaultSqlConnectionRetryPolicy());
        }

        public static Task OpenWithRetryAsync(this DbConnection connection, CancellationToken cancellationToken)
        {
            return OpenWithRetryAsync(connection, RetryManager.Instance.GetDefaultSqlConnectionRetryPolicy(), cancellationToken);
        }

        public static Task OpenWithRetryAsync(this DbConnection connection, RetryPolicy retryPolicy)
        {
            return OpenWithRetryAsync(connection, retryPolicy, CancellationToken.None);
        }

        public static Task OpenWithRetryAsync(this DbConnection connection, RetryPolicy retryPolicy, CancellationToken cancellationToken, ILogger logger = null)
        {
            var policy = retryPolicy ?? RetryPolicy.NoRetry;
            if (logger is not null)
            {
                policy = policy.LoggingRetries(logger, "Open Database Connection");
            }
            return policy.ExecuteActionAsync(connection.OpenAsync, cancellationToken);
        }
    }
}
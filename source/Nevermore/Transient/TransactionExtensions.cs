using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
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
            return (retryPolicy ?? RetryPolicy.NoRetry).LoggingRetries("Beginning Database Transaction").ExecuteAction(() => connection.BeginTransaction(isolationLevel, sqlServerTransactionName));
        }

        public static Task<DbTransaction> BeginTransactionWithRetryAsync(this SqlConnection connection, IsolationLevel isolationLevel, string sqlServerTransactionName, CancellationToken cancellationToken)
        {
            return BeginTransactionWithRetryAsync(connection, isolationLevel, sqlServerTransactionName, RetryManager.Instance.GetDefaultSqlTransactionRetryPolicy(), cancellationToken);
        }

        public static async Task<DbTransaction> BeginTransactionWithRetryAsync(this SqlConnection connection, IsolationLevel isolationLevel, string sqlServerTransactionName, RetryPolicy retryPolicy, CancellationToken cancellationToken)
        {
            return await (retryPolicy ?? RetryPolicy.NoRetry).LoggingRetries("Beginning Database Transaction").ExecuteActionAsync(async () =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Task.Yield();

                // We use the synchronous overload here even though there is an async one, because the BeginTransactionAsync calls
                // the synchronous version anyway, and the async overload doesn't accept a name parameter.
                return connection.BeginTransaction(isolationLevel, sqlServerTransactionName);
            }).ConfigureAwait(false);
        }
    }
}
using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Transient
{
    internal static class DbCommandExtensions
    {
        public static int ExecuteNonQueryWithRetry(this DbCommand command, RetryPolicy commandRetryPolicy, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteNonQuery")
        {
            RetryUtil.GuardConnectionIsNotNull(command.Connection);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            return effectiveCommandRetryPolicy.ExecuteAction(() =>
            {
                var weOwnTheConnectionLifetime = RetryUtil.EnsureValidConnection(command.Connection, connectionRetryPolicy);
                try
                {
                    return command.ExecuteNonQuery();
                }
                finally
                {
                    if (weOwnTheConnectionLifetime && command.Connection?.State == ConnectionState.Open)
                        command.Connection.Close();
                }
            });
        }

        public static Task<int> ExecuteNonQueryWithRetryAsync(this DbCommand command, RetryPolicy commandRetryPolicy, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteNonQueryAsync", CancellationToken cancellationToken = default)
        {
            RetryUtil.GuardConnectionIsNotNull(command.Connection);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            return effectiveCommandRetryPolicy.ExecuteAction(async () =>
            {
                var weOwnTheConnectionLifetime = await RetryUtil.EnsureValidConnectionAsync(command.Connection, connectionRetryPolicy, cancellationToken).ConfigureAwait(false);
                try
                {
                    return await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    if (weOwnTheConnectionLifetime && command.Connection?.State == ConnectionState.Open)
                        await command.Connection.CloseAsync().ConfigureAwait(false);
                }
            });
        }

        public static DbDataReader ExecuteReaderWithRetry(this DbCommand command, RetryPolicy commandRetryPolicy, CommandBehavior behavior = CommandBehavior.Default, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteReader")
        {
            RetryUtil.GuardConnectionIsNotNull(command.Connection);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            return effectiveCommandRetryPolicy.ExecuteAction(() =>
            {
                var weOwnTheConnectionLifetime = RetryUtil.EnsureValidConnection(command.Connection, connectionRetryPolicy);
                try
                {
                    return command.ExecuteReader(behavior);
                }
                catch (Exception)
                {
                    if (weOwnTheConnectionLifetime && command.Connection != null &&
                        command.Connection.State == ConnectionState.Open)
                        command.Connection.Close();
                    throw;
                }
            });
        }

        public static async Task<DbDataReader> ExecuteReaderWithRetryAsync(this DbCommand command, RetryPolicy commandRetryPolicy, CommandBehavior commandBehavior, CancellationToken cancellationToken, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteReader")
        {
            RetryUtil.GuardConnectionIsNotNull(command.Connection);
            var effectiveCommandRetryPolicy =
                (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            return await effectiveCommandRetryPolicy.ExecuteActionAsync(async () =>
            {
                var weOwnTheConnectionLifetime = await RetryUtil.EnsureValidConnectionAsync(command.Connection, connectionRetryPolicy, cancellationToken).ConfigureAwait(false);
                try
                {
                    return await command.ExecuteReaderAsync(commandBehavior, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception)
                {
                    if (weOwnTheConnectionLifetime && command.Connection != null &&
                        command.Connection.State == ConnectionState.Open)
                        await command.Connection.CloseAsync().ConfigureAwait(false);
                    throw;
                }
            }).ConfigureAwait(false);
        }

        public static object ExecuteScalarWithRetry(this DbCommand command, RetryPolicy commandRetryPolicy, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteScalar")
        {
            RetryUtil.GuardConnectionIsNotNull(command.Connection);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            return effectiveCommandRetryPolicy.ExecuteAction(() =>
            {
                var weOwnTheConnectionLifetime = RetryUtil.EnsureValidConnection(command.Connection, connectionRetryPolicy);
                try
                {
                    return command.ExecuteScalar();
                }
                finally
                {
                    if (weOwnTheConnectionLifetime && command.Connection?.State == ConnectionState.Open)
                        command.Connection.Close();
                }
            });
        }

        public static async Task<object> ExecuteScalarWithRetryAsync(this DbCommand command, RetryPolicy commandRetryPolicy, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteScalar", CancellationToken cancellationToken = default)
        {
            RetryUtil.GuardConnectionIsNotNull(command.Connection);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryManager.Instance.GetDefaultSqlCommandRetryPolicy()).LoggingRetries(operationName);
            return await effectiveCommandRetryPolicy.ExecuteActionAsync(async () =>
            {
                var weOwnTheConnectionLifetime = await RetryUtil.EnsureValidConnectionAsync(command.Connection, connectionRetryPolicy, cancellationToken).ConfigureAwait(false);
                try
                {
                    return await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
                }
                finally
                {
                    if (weOwnTheConnectionLifetime && command.Connection?.State == ConnectionState.Open)
                        await command.Connection.CloseAsync().ConfigureAwait(false);
                }
            }).ConfigureAwait(false);
        }
    }
}
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
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            return effectiveCommandRetryPolicy.ExecuteAction(() =>
            {
                var weOwnTheConnectionLifetime = EnsureValidConnection(command, connectionRetryPolicy);
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
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            return effectiveCommandRetryPolicy.ExecuteActionAsync(async () =>
            {
                var weOwnTheConnectionLifetime = await EnsureValidConnectionAsync(command, connectionRetryPolicy, cancellationToken).ConfigureAwait(false);
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
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            return effectiveCommandRetryPolicy.ExecuteAction(() =>
            {
                var weOwnTheConnectionLifetime = EnsureValidConnection(command, connectionRetryPolicy);
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
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy =
                (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            return await effectiveCommandRetryPolicy.ExecuteActionAsync(async () =>
            {
                var weOwnTheConnectionLifetime = await EnsureValidConnectionAsync(command, connectionRetryPolicy, cancellationToken).ConfigureAwait(false);
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
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            return effectiveCommandRetryPolicy.ExecuteAction(() =>
            {
                var weOwnTheConnectionLifetime = EnsureValidConnection(command, connectionRetryPolicy);
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
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryManager.Instance.GetDefaultSqlCommandRetryPolicy()).LoggingRetries(operationName);
            return await effectiveCommandRetryPolicy.ExecuteActionAsync(async () =>
            {
                var weOwnTheConnectionLifetime = await EnsureValidConnectionAsync(command, connectionRetryPolicy, cancellationToken).ConfigureAwait(false);
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

        static void GuardConnectionIsNotNull(DbCommand command)
        {
            if (command.Connection == null)
                throw new InvalidOperationException("Connection property has not been initialized.");
        }

        /// <summary>
        /// Ensures the command either has an existing open connection, or we will open one for it.
        /// </summary>
        /// <returns>True if we opened the connection (indicating we own its lifetime), False if the connection was already open (indicating someone else owns its lifetime)</returns>
        static bool EnsureValidConnection(DbCommand command, RetryPolicy retryPolicy)
        {
            if (command == null) return false;

            GuardConnectionIsNotNull(command);

            if (command.Connection.State == ConnectionState.Open) return false;

            command.Connection.OpenWithRetry(retryPolicy);
            return true;
        }

        static async Task<bool> EnsureValidConnectionAsync(DbCommand command, RetryPolicy retryPolicy, CancellationToken cancellationToken)
        {
            if (command == null) return false;

            GuardConnectionIsNotNull(command);

            if (command.Connection.State == ConnectionState.Open) return false;

            await command.Connection.OpenWithRetryAsync(retryPolicy, cancellationToken).ConfigureAwait(false);
            return true;
        }
    }
}
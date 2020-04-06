using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;

namespace Nevermore.Transient
{
    // ReSharper disable once InconsistentNaming
    public static class DbCommandExtensions
    {
        public static int ExecuteNonQueryWithRetry(this DbCommand command, string operationName = "ExecuteNonQuery")
        {
            var commandPolicy = RetryManager.Instance.GetDefaultSqlCommandRetryPolicy();
            return command.ExecuteNonQueryWithRetry(commandPolicy, operationName);
        }

        public static int ExecuteNonQueryWithRetry(this DbCommand command, RetryPolicy retryPolicy, string operationName = "ExecuteNonQuery")
        {
            return command.ExecuteNonQueryWithRetry(retryPolicy, RetryPolicy.NoRetry, operationName);
        }

        public static Task<int> ExecuteNonQueryWithRetryAsync(this DbCommand command, RetryPolicy retryPolicy, string operationName = "ExecuteNonQuery")
        {
            return command.ExecuteNonQueryWithRetryAsync(retryPolicy, RetryPolicy.NoRetry, operationName);
        }

        public static int ExecuteNonQueryWithRetry(this DbCommand command, RetryPolicy commandRetryPolicy, RetryPolicy connectionRetryPolicy, string operationName = "ExecuteNonQuery")
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
                    if (weOwnTheConnectionLifetime && command.Connection != null && command.Connection.State == ConnectionState.Open)
                        command.Connection.Close();
                }
            });
        }

        public static Task<int> ExecuteNonQueryWithRetryAsync(this DbCommand command, RetryPolicy commandRetryPolicy, RetryPolicy connectionRetryPolicy, string operationName = "ExecuteNonQueryAsync")
        {
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            return effectiveCommandRetryPolicy.ExecuteAction(async () =>
            {
                var weOwnTheConnectionLifetime = EnsureValidConnection(command, connectionRetryPolicy);
                try
                {
                    return await command.ExecuteNonQueryAsync();
                }
                finally
                {
                    if (weOwnTheConnectionLifetime && command.Connection != null && command.Connection.State == ConnectionState.Open)
                        command.Connection.Close();
                }
            });
        }

        public static DbDataReader ExecuteReaderWithRetry(this DbCommand command, string operationName = "ExecuteReader")
        {
            return command.ExecuteReaderWithRetry(RetryManager.Instance.GetDefaultSqlCommandRetryPolicy(), operationName);
        }

        public static Task<DbDataReader> ExecuteReaderAsyncWithRetry(this DbCommand command, string operationName = "ExecuteReaderAsync")
        {
            return command.ExecuteReaderAsyncWithRetry(RetryManager.Instance.GetDefaultSqlCommandRetryPolicy(), operationName);
        }

        public static DbDataReader ExecuteReaderWithRetry(this DbCommand command, RetryPolicy retryPolicy, string operationName = "ExecuteReader")
        {
            return command.ExecuteReaderWithRetry(retryPolicy, RetryPolicy.NoRetry, operationName);
        }

        public static Task<DbDataReader> ExecuteReaderAsyncWithRetry(this DbCommand command, RetryPolicy retryPolicy, string operationName = "ExecuteReader")
        {
            return command.ExecuteReaderAsyncWithRetry(retryPolicy, RetryPolicy.NoRetry, operationName);
        }

        public static DbDataReader ExecuteReaderWithRetry(this DbCommand command, RetryPolicy commandRetryPolicy, RetryPolicy connectionRetryPolicy, string operationName = "ExecuteReader")
        {
            try
            {
                GuardConnectionIsNotNull(command);
                var effectiveCommandRetryPolicy =
                    (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
                return effectiveCommandRetryPolicy.ExecuteAction(() =>
                {
                    var weOwnTheConnectionLifetime = EnsureValidConnection(command, connectionRetryPolicy);
                    try
                    {
                        return command.ExecuteReader();
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
            catch (Exception ex)
            {
                throw new Exception($"Exception occurred while executing a reader for `{command.CommandText}`", ex);
            }
        }
        
        public static async Task<DbDataReader> ExecuteReaderAsyncWithRetry(this DbCommand command, RetryPolicy commandRetryPolicy, RetryPolicy connectionRetryPolicy, string operationName = "ExecuteReader")
        {
            try
            {
                GuardConnectionIsNotNull(command);
                var effectiveCommandRetryPolicy = 
                    (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
                return await effectiveCommandRetryPolicy.ExecuteActionAsync(async () =>
                {
                    var weOwnTheConnectionLifetime = EnsureValidConnection(command, connectionRetryPolicy);
                    try
                    {
                        return await command.ExecuteReaderAsync();
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
            catch (Exception ex)
            {
                throw new Exception($"Exception occurred while executing a reader for `{command.CommandText}`", ex);
            }
        }

        public static DbDataReader ExecuteReaderWithRetry(this DbCommand command, CommandBehavior behavior, string operationName = "ExecuteReader")
        {
            return command.ExecuteReaderWithRetry(behavior, RetryManager.Instance.GetDefaultSqlCommandRetryPolicy(), operationName);
        }

        public static DbDataReader ExecuteReaderWithRetry(this DbCommand command, CommandBehavior behavior, RetryPolicy retryPolicy, string operationName = "ExecuteReader")
        {
            return command.ExecuteReaderWithRetry(behavior, retryPolicy, RetryPolicy.NoRetry, operationName);
        }

        public static DbDataReader ExecuteReaderWithRetry(this DbCommand command, CommandBehavior behavior, RetryPolicy commandRetryPolicy, RetryPolicy connectionRetryPolicy, string operationName = "ExecuteReader")
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
                    if (weOwnTheConnectionLifetime && command.Connection != null && command.Connection.State == ConnectionState.Open)
                        command.Connection.Close();
                    throw;
                }
            });
        }

        public static object ExecuteScalarWithRetry(this DbCommand command, string operationName = "ExecuteScalar")
        {
            return command.ExecuteScalarWithRetry(RetryManager.Instance.GetDefaultSqlCommandRetryPolicy());
        }

        public static object ExecuteScalarWithRetry(this DbCommand command, RetryPolicy retryPolicy, string operationName = "ExecuteScalar")
        {
            return ExecuteScalarWithRetry(command, retryPolicy, RetryPolicy.NoRetry);
        }

        public static object ExecuteScalarWithRetry(this DbCommand command, RetryPolicy commandRetryPolicy, RetryPolicy connectionRetryPolicy, string operationName = "ExecuteScalar")
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
                    if (weOwnTheConnectionLifetime && command.Connection != null && command.Connection.State == ConnectionState.Open)
                        command.Connection.Close();
                }
            });
        }

        // ReSharper disable once UnusedParameter.Local
        private static void GuardConnectionIsNotNull(DbCommand command)
        {
            if (command.Connection == null)
                throw new InvalidOperationException("Connection property has not been initialized.");
        }

        /// <summary>
        /// Ensures the command either has an existing open connection, or we will open one for it.
        /// </summary>
        /// <returns>True if we opened the connection (indicating we own its lifetime), False if the connection was already open (indicating someone else owns its lifetime)</returns>
        private static bool EnsureValidConnection(DbCommand command, RetryPolicy retryPolicy)
        {
            if (command == null) return false;

            GuardConnectionIsNotNull(command);

            if (command.Connection.State == ConnectionState.Open) return false;

            command.Connection.OpenWithRetry(retryPolicy);
            return true;
        }
    }
}
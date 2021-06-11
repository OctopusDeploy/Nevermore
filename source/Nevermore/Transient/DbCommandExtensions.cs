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
                using var validCommand = ValidCommand.For(command, connectionRetryPolicy);
                return validCommand.ExecuteNonQuery();
            });
        }

        public static Task<int> ExecuteNonQueryWithRetryAsync(this DbCommand command, RetryPolicy commandRetryPolicy, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteNonQueryAsync", CancellationToken cancellationToken = default)
        {
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            return effectiveCommandRetryPolicy.ExecuteAction(async () =>
            {
                await using var validCommand = await ValidCommandAsync.For(command, connectionRetryPolicy, cancellationToken);
                return await validCommand.ExecuteNonQueryAsync(cancellationToken);
            });
        }

        public static DbDataReader ExecuteReaderWithRetry(this DbCommand command, RetryPolicy commandRetryPolicy, CommandBehavior behavior = CommandBehavior.Default, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteReader")
        {
            try
            {
                GuardConnectionIsNotNull(command);
                var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
                return effectiveCommandRetryPolicy.ExecuteAction(() =>
                {
                    using var validCommand = ValidCommand.For(command, connectionRetryPolicy);
                    return validCommand.ExecuteReader(behavior);
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception occurred while executing a reader for `{command.CommandText}`", ex);
            }
        }
        
        public static async Task<DbDataReader> ExecuteReaderWithRetryAsync(this DbCommand command, RetryPolicy commandRetryPolicy, CommandBehavior commandBehavior, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteReader", CancellationToken cancellationToken = default)
        {
            try
            {
                GuardConnectionIsNotNull(command);
                var effectiveCommandRetryPolicy = 
                    (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
                return await effectiveCommandRetryPolicy.ExecuteActionAsync(async () =>
                {
                    await using var validCommand = await ValidCommandAsync.For(command, connectionRetryPolicy, cancellationToken);
                    return await validCommand.ExecuteReaderAsync(commandBehavior, cancellationToken);
                });
            }
            catch (Exception ex)
            {
                throw new Exception($"Exception occurred while executing a reader for `{command.CommandText}`", ex);
            }
        }

        public static object ExecuteScalarWithRetry(this DbCommand command, RetryPolicy commandRetryPolicy, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteScalar")
        {
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            return effectiveCommandRetryPolicy.ExecuteAction(() =>
            {
                using var validCommand = ValidCommand.For(command, connectionRetryPolicy);
                return validCommand.ExecuteScalar();
            });
        }
        
        public static async Task<object> ExecuteScalarWithRetryAsync(this DbCommand command, RetryPolicy commandRetryPolicy, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteScalar", CancellationToken cancellationToken = default)
        {
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryManager.Instance.GetDefaultSqlCommandRetryPolicy()).LoggingRetries(operationName);
            return await effectiveCommandRetryPolicy.ExecuteActionAsync(async () =>
            {
                await using var validCommand = await ValidCommandAsync.For(command, connectionRetryPolicy, cancellationToken);
                return await validCommand.ExecuteScalarAsync(cancellationToken);
            });
        }

        static void GuardConnectionIsNotNull(DbCommand command)
        {
            if (command?.Connection == null)
                throw new InvalidOperationException("Connection property has not been initialized.");
        }

        class ValidCommand : IDisposable
        {
            DbConnection ourConnection = null;
            
            readonly DbCommand innerCommand = null;
            
            ValidCommand(DbCommand command, DbConnection connection = null)
            {
                innerCommand = command;
                ourConnection = connection;
            }

            public static ValidCommand For(DbCommand command, RetryPolicy retryPolicy)
            {
                GuardConnectionIsNotNull(command);
                
                if (command.Connection.State == ConnectionState.Open) return new ValidCommand(command);
                
                command.Connection.OpenWithRetry(retryPolicy);
                return new ValidCommand(command, command.Connection);
            }

            public object ExecuteScalar() => innerCommand.ExecuteScalar();

            public DbDataReader ExecuteReader(CommandBehavior behavior) => innerCommand.ExecuteReader(behavior);

            public int ExecuteNonQuery() => innerCommand.ExecuteNonQuery();
            
            public void Dispose()
            {
                if (ourConnection?.State != ConnectionState.Open) return;
                ourConnection.Close();
                ourConnection = null;
            }
        }

        /// <summary>
        /// Ensures the command either has an existing open connection, or we will open one for it.
        /// </summary>
        /// <returns>True if we opened the connection (indicating we own its lifetime), False if the connection was already open (indicating someone else owns its lifetime)</returns>
        class ValidCommandAsync : IAsyncDisposable
        {
            DbConnection ourConnection = null;

            readonly DbCommand innerCommand = null;

            ValidCommandAsync(DbCommand command, DbConnection connection = null)
            {
                innerCommand = command;
                ourConnection = connection;
            }
            
            public static async Task<ValidCommandAsync> For(DbCommand command, RetryPolicy retryPolicy, CancellationToken cancellationToken)
            {
                GuardConnectionIsNotNull(command);
                
                if (command.Connection.State == ConnectionState.Open) return new ValidCommandAsync(command);
                
                await command.Connection.OpenWithRetryAsync(retryPolicy, cancellationToken);
                return new ValidCommandAsync(command, command.Connection);
            }
            
            public async ValueTask DisposeAsync()
            {
                if (ourConnection?.State == ConnectionState.Open)
                {
                    await ourConnection.CloseAsync();
                    ourConnection = null;
                }
            }

            public Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
                => innerCommand.ExecuteScalarAsync(cancellationToken);
            
            public Task<DbDataReader> ExecuteReaderAsync(CommandBehavior behavior, CancellationToken cancellationToken) 
                => innerCommand.ExecuteReaderAsync(behavior, cancellationToken);

            public Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
                => innerCommand.ExecuteNonQueryAsync(cancellationToken);
        }
    }
}
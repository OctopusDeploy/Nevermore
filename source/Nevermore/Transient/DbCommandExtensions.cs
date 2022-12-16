using System;
using System.Data;
using System.Data.Common;
using System.IO;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;

namespace Nevermore.Transient
{
    internal static class DbCommandExtensions
    {
        //********************************************
        //Testing/Exploration code only.
        static readonly Random random = new Random();

        public class ChaosMonkeyException : Exception
        {

        }

        public static bool EnableChaos = false;
        public static int PercentChanceOfChaos = 10;
        //********************************************
        
        private static void ProvideChaos()
        {
            if (EnableChaos && random.Next(1, 100) > (100 - PercentChanceOfChaos))
                throw new ChaosMonkeyException();
        }

        public static int ExecuteNonQueryWithRetry(this DbCommand command, RetryPolicy commandRetryPolicy, Action replayClosure, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteNonQuery")
        {
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            var performReplay = false;
            return effectiveCommandRetryPolicy.ExecuteAction(() =>
            {
                var weOwnTheConnectionLifetime = EnsureValidConnection(command, connectionRetryPolicy);
                try
                {
                    if (performReplay)
                    {   
                        Replay(command, replayClosure);
                    }
                    
                    performReplay = true;
                    ProvideChaos();
                    return command.ExecuteNonQuery();
                }
                finally
                {
                    if (weOwnTheConnectionLifetime && command.Connection?.State == ConnectionState.Open)
                        command.Connection.Close();
                }
            });
        }

        public static Task<int> ExecuteNonQueryWithRetryAsync(this DbCommand command, RetryPolicy commandRetryPolicy, Func<CancellationToken, Task> replayClosureAsync, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteNonQueryAsync", CancellationToken cancellationToken = default)
        {
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            var performReplay = false;
            return effectiveCommandRetryPolicy.ExecuteAction(async () =>
            {
                var weOwnTheConnectionLifetime = await EnsureValidConnectionAsync(command, connectionRetryPolicy, cancellationToken);
                try
                {
                    if (performReplay)
                    {
                        await Replay(command, replayClosureAsync, cancellationToken);
                    }

                    performReplay = true;
                    ProvideChaos();
                    return await command.ExecuteNonQueryAsync(cancellationToken);
                }
                finally
                {
                    if (weOwnTheConnectionLifetime && command.Connection?.State == ConnectionState.Open)
                        await command.Connection.CloseAsync();
                }
            });
        }

        public static DbDataReader ExecuteReaderWithRetry(this DbCommand command, RetryPolicy commandRetryPolicy, Action replayClosure, CommandBehavior behavior = CommandBehavior.Default, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteReader")
        {
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            var performReplay = false;
            return effectiveCommandRetryPolicy.ExecuteAction(() =>
            {
                var weOwnTheConnectionLifetime = EnsureValidConnection(command, connectionRetryPolicy);
                try
                {
                    if (performReplay)
                    {
                        Replay(command, replayClosure);
                    }

                    performReplay = true;
                    ProvideChaos();
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

        public static async Task<DbDataReader> ExecuteReaderWithRetryAsync(this DbCommand command, RetryPolicy commandRetryPolicy, Func<CancellationToken, Task> replayClosure, CommandBehavior commandBehavior, CancellationToken cancellationToken, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteReader")
        {
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy =
                (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            var performReplay = false;
            return await effectiveCommandRetryPolicy.ExecuteActionAsync(async () =>
            {
                var weOwnTheConnectionLifetime = await EnsureValidConnectionAsync(command, connectionRetryPolicy, cancellationToken);
                try
                {
                    if (performReplay)
                    {
                        await Replay(command, replayClosure, cancellationToken);
                    }

                    performReplay = true;
                    ProvideChaos();
                    return await command.ExecuteReaderAsync(commandBehavior, cancellationToken);
                }
                catch (Exception)
                {
                    if (weOwnTheConnectionLifetime && command.Connection != null &&
                        command.Connection.State == ConnectionState.Open)
                        await command.Connection.CloseAsync();
                    throw;
                }
            });
        }

        public static object ExecuteScalarWithRetry(this DbCommand command, RetryPolicy commandRetryPolicy, Action replayClosure, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteScalar")
        {
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryPolicy.NoRetry).LoggingRetries(operationName);
            var performReplay = false;
            return effectiveCommandRetryPolicy.ExecuteAction(() =>
            {
                var weOwnTheConnectionLifetime = EnsureValidConnection(command, connectionRetryPolicy);
                try
                {
                    if (performReplay)
                    {
                        Replay(command, replayClosure);
                    }

                    performReplay = true;
                    ProvideChaos();
                    return command.ExecuteScalar();
                }
                finally
                {
                    if (weOwnTheConnectionLifetime && command.Connection?.State == ConnectionState.Open)
                        command.Connection.Close();
                }
            });
        }

        public static async Task<object> ExecuteScalarWithRetryAsync(this DbCommand command, RetryPolicy commandRetryPolicy, Func<CancellationToken, Task> replayClosure, RetryPolicy connectionRetryPolicy = null, string operationName = "ExecuteScalar", CancellationToken cancellationToken = default)
        {
            GuardConnectionIsNotNull(command);
            var effectiveCommandRetryPolicy = (commandRetryPolicy ?? RetryManager.Instance.GetDefaultSqlCommandRetryPolicy()).LoggingRetries(operationName);
            var performReplay = false;

            return await effectiveCommandRetryPolicy.ExecuteActionAsync(async () =>
            {
                var weOwnTheConnectionLifetime = await EnsureValidConnectionAsync(command, connectionRetryPolicy, cancellationToken);
                try
                {
                    if (performReplay)
                    {
                        await Replay(command, replayClosure, cancellationToken);
                    }

                    performReplay = true;
                    ProvideChaos();
                    return await command.ExecuteScalarAsync(cancellationToken);
                }
                finally
                {
                    if (weOwnTheConnectionLifetime && command.Connection?.State == ConnectionState.Open)
                        await command.Connection.CloseAsync();
                }
            });
        }

        static void GuardConnectionIsNotNull(DbCommand command)
        {
            if (command.Connection == null)
                throw new InvalidOperationException("Connection property has not been initialized.");
        }


        static void Replay(DbCommand command, Action replayClosure)
        {
            replayClosure();

            foreach (DbParameter commandParameter in command.Parameters)
            {
                if (commandParameter.Value is Stream stream)
                    stream.Seek(0, SeekOrigin.Begin);
            }
        }

        static async Task Replay(DbCommand command, Func<CancellationToken, Task> replayClosure, CancellationToken cancellationToken)
        {
            await replayClosure(cancellationToken);

            foreach (DbParameter commandParameter in command.Parameters)
            {
                if (commandParameter.Value is Stream stream)
                    stream.Seek(0, SeekOrigin.Begin);
            }
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

            await command.Connection.OpenWithRetryAsync(retryPolicy, cancellationToken);
            return true;
        }
    }
}
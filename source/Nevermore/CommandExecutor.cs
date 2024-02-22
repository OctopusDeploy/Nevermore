using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Nevermore.Advanced;
using Nevermore.Diagnostics;
using Nevermore.Mapping;
using Nevermore.Transient;

namespace Nevermore
{
    /// <summary>
    /// A nevermore query has two phases.
    ///  - Building the query, which results in a <see cref="PreparedCommand"/>
    ///  - Executing the prepared command against the database
    /// This class does phase 2. It wraps DbCommand, but with our timing and exception handling code.
    /// </summary>
    internal class CommandExecutor : IDisposable
    {
        // Getting a typed ILog causes JIT compilation - we should only do this once
        readonly DbCommand command;
        readonly PreparedCommand prepared;
        readonly RetryPolicy retryPolicy;
        readonly TimedSection timedSection;
        readonly ITransactionDiagnostic transaction;
        readonly bool allowSynchronousOperations;
        readonly ILogger logger;

        public CommandExecutor(DbCommand command, PreparedCommand prepared, RetryPolicy retryPolicy, TimedSection timedSection, ITransactionDiagnostic transaction, bool allowSynchronousOperations, ILogger logger)
        {
            this.command = command;
            this.prepared = prepared;
            this.retryPolicy = retryPolicy;
            this.timedSection = timedSection;
            this.transaction = transaction;
            this.allowSynchronousOperations = allowSynchronousOperations;
            this.logger = logger;
        }

        public int ExecuteNonQuery()
        {
            AssertSynchronousOperation();
            try
            {
                AssertSynchronousOperation();
                return command.ExecuteNonQueryWithRetry(retryPolicy);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Exception in relational transaction '{TransactionName}'", transaction.Name);
                throw;
            }
        }

        public async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await command.ExecuteNonQueryWithRetryAsync(retryPolicy, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Exception in relational transaction '{TransactionName}'", transaction.Name);
                throw;
            }
        }

        public object ExecuteScalar()
        {
            AssertSynchronousOperation();
            try
            {
                return command.ExecuteScalarWithRetry(retryPolicy);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Exception in relational transaction '{TransactionName}'", transaction.Name);
                throw;
            }
        }

        public async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await command.ExecuteScalarWithRetryAsync(retryPolicy, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Exception in relational transaction '{TransactionName}'", transaction.Name);
                throw;
            }
        }

        public T[] ReadResults<T>(Func<DbDataReader, T> mapper)
        {
            AssertSynchronousOperation();
            try
            {
                var data = new List<T>();
                using var reader = command.ExecuteReaderWithRetry(retryPolicy, prepared.CommandBehavior);
                while (reader.Read())
                {
                    data.Add(mapper(reader));
                }

                return data.ToArray();
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Exception in relational transaction '{TransactionName}'", transaction.Name);
                throw;
            }
        }

        public DbDataReader ExecuteReader()
        {
            AssertSynchronousOperation();
            try
            {
                return command.ExecuteReaderWithRetry(retryPolicy, prepared.CommandBehavior);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Exception in relational transaction '{TransactionName}'", transaction.Name);
                throw;
            }
        }

        public async Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await command.ExecuteReaderWithRetryAsync(retryPolicy, prepared.CommandBehavior, cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Exception in relational transaction '{TransactionName}'", transaction.Name);
                throw;
            }
        }

        public async Task<T[]> ReadResultsAsync<T>(Func<DbDataReader, Task<T>> mapper, CancellationToken cancellationToken)
        {
            try
            {
                var data = new List<T>();
                using (var reader = await command.ExecuteReaderWithRetryAsync(retryPolicy, prepared.CommandBehavior, cancellationToken).ConfigureAwait(false))
                {
                    while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        data.Add(await mapper(reader).ConfigureAwait(false));
                    }
                }

                return data.ToArray();
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Exception in relational transaction '{TransactionName}'", transaction.Name);
                throw;
            }
        }

        Exception WrapException(Exception ex)
        {
            if (ex is SqlException {Number: 1205 or 1222 or -2}) // 1205 deadlock, 1222 row lock timeout, -2 timeout
            {
                var builder = new StringBuilder();
                builder.AppendLine(ex.Message);
                builder.AppendLine("Current transactions: ");
                transaction.WriteCurrentTransactions(builder);
                throw new Exception(builder.ToString(), ex);
            }

            logger.LogDebug(ex, "Error while executing SQL command in transaction '{TransactionName}'", transaction.Name);

            return new Exception($"Error while executing SQL command in transaction '{transaction.Name}': {ex.Message}{Environment.NewLine}The command being executed was:{Environment.NewLine}{command.CommandText}", ex);
        }

        static void DetectAndThrowIfKnownException(SqlException ex, DocumentMap mapping)
        {
            if (mapping == null)
                return;
            if (ex.Number == 2627 || ex.Number == 2601)
            {
                var uniqueRule = mapping.UniqueConstraints.FirstOrDefault(u => ex.Message.Contains(u.ConstraintName));
                if (uniqueRule != null)
                {
                    throw new UniqueConstraintViolationException(uniqueRule.Message);
                }
            }
        }

        void AssertSynchronousOperation()
        {
            if (!allowSynchronousOperations)
                throw new SynchronousOperationsDisabledException();
        }

        public void Dispose()
        {
            DisposeOfParameters();

            timedSection?.Dispose();
            command?.Dispose();
        }

        void DisposeOfParameters()
        {
            foreach (DbParameter parameter in command.Parameters)
            {
                // Parameters can contain streams and other disposable objects.
                if (parameter.Value is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Nevermore.Advanced;
using Nevermore.Diagnositcs;
using Nevermore.Diagnostics;
using Nevermore.Mapping;

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
        static readonly ILog Log = LogProvider.For<ReadTransaction>();

        readonly DbCommand command;
        readonly PreparedCommand prepared;
        readonly TimedSection timedSection;
        readonly ITransactionDiagnostic transactionDiagnostic;
        readonly bool allowSynchronousOperations;

        public CommandExecutor(DbCommand command, PreparedCommand prepared, TimedSection timedSection, ITransactionDiagnostic transactionDiagnostic, bool allowSynchronousOperations)
        {
            this.command = command;
            this.prepared = prepared;
            this.timedSection = timedSection;
            this.transactionDiagnostic = transactionDiagnostic;
            this.allowSynchronousOperations = allowSynchronousOperations;
        }

        public int ExecuteNonQuery()
        {
            AssertSynchronousOperation();
            try
            {
                AssertSynchronousOperation();
                return command.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                Log.DebugException($"Exception in relational transaction '{transactionDiagnostic.Name}'", ex);
                throw;
            }
        }

        public async Task<int> ExecuteNonQueryAsync( CancellationToken cancellationToken)
        {
            try
            {
                return await command.ExecuteNonQueryAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                Log.DebugException($"Exception in relational transaction '{transactionDiagnostic.Name}'", ex);
                throw;
            }
        }

        public object ExecuteScalar()
        {
            AssertSynchronousOperation();
            try
            {
                return command.ExecuteScalar();
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                Log.DebugException($"Exception in relational transaction '{transactionDiagnostic.Name}'", ex);
                throw;
            }
        }

        public async Task<object> ExecuteScalarAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await command.ExecuteScalarAsync(cancellationToken);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                Log.DebugException($"Exception in relational transaction '{transactionDiagnostic.Name}'", ex);
                throw;
            }
        }

        public T[] ReadResults<T>(Func<DbDataReader, T> mapper)
        {
            AssertSynchronousOperation();
            try
            {
                var data = new List<T>();
                using var reader = command.ExecuteReader(prepared.CommandBehavior);
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
                Log.DebugException($"Exception in relational transaction '{transactionDiagnostic.Name}'", ex);
                throw;
            }
        }

        public DbDataReader ExecuteReader()
        {
            AssertSynchronousOperation();
            try
            {
                return command.ExecuteReader(prepared.CommandBehavior);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                Log.DebugException($"Exception in relational transaction '{transactionDiagnostic.Name}'", ex);
                throw;
            }
        }

        public async Task<DbDataReader> ExecuteReaderAsync(CancellationToken cancellationToken)
        {
            try
            {
                return await command.ExecuteReaderAsync(prepared.CommandBehavior, cancellationToken);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                Log.DebugException($"Exception in relational transaction '{transactionDiagnostic.Name}'", ex);
                throw;
            }
        }

        public async Task<T[]> ReadResultsAsync<T>(Func<DbDataReader, Task<T>> mapper, CancellationToken cancellationToken)
        {
            try
            {
                var data = new List<T>();
                await using (var reader = await command.ExecuteReaderAsync(prepared.CommandBehavior, cancellationToken))
                {
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        data.Add(await mapper(reader));
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
                Log.DebugException($"Exception in relational transaction '{transactionDiagnostic.Name}'", ex);
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
                transactionDiagnostic.WriteCurrentTransactions(builder);
                throw new Exception(builder.ToString(), ex);
            }

            Log.DebugException($"Error while executing SQL command in transaction '{transactionDiagnostic.Name}'", ex);

            return new Exception($"Error while executing SQL command in transaction '{transactionDiagnostic.Name}': {ex.Message}{Environment.NewLine}The command being executed was:{Environment.NewLine}{command.CommandText}", ex);
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
            timedSection?.Dispose();
            command?.Dispose();
        }
    }
}
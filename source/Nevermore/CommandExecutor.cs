using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Nevermore.Diagnositcs;
using Nevermore.Diagnostics;
using Nevermore.Mapping;
using Nevermore.Transient;
using Nevermore.Util;

namespace Nevermore
{
    internal interface ITransactionDiagnostic
    {
        public string Name { get; }
        public void WriteCurrentTransactions(StringBuilder output);
    }
    
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
        readonly RetryPolicy retryPolicy;
        readonly TimedSection timedSection;
        readonly ITransactionDiagnostic transaction;

        public CommandExecutor(DbCommand command, PreparedCommand prepared, RetryPolicy retryPolicy, TimedSection timedSection, ITransactionDiagnostic transaction)
        {
            this.command = command;
            this.prepared = prepared;
            this.retryPolicy = retryPolicy;
            this.timedSection = timedSection;
            this.transaction = transaction;
        }

        public int ExecuteNonQuery()
        {
            try
            {
                return command.ExecuteNonQueryWithRetry(retryPolicy);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                Log.DebugException($"Exception in relational transaction '{transaction.Name}'", ex);
                throw;
            }
        }

        public async Task<int> ExecuteNonQueryAsync()
        {
            try
            {
                return await command.ExecuteNonQueryWithRetryAsync(retryPolicy);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                Log.DebugException($"Exception in relational transaction '{transaction.Name}'", ex);
                throw;
            }
        }

        public object ExecuteScalar()
        {
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
                Log.DebugException($"Exception in relational transaction '{transaction.Name}'", ex);
                throw;
            }
        }

        public async Task<object> ExecuteScalarAsync()
        {
            try
            {
                return await command.ExecuteScalarWithRetryAsync(retryPolicy);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                Log.DebugException($"Exception in relational transaction '{transaction.Name}'", ex);
                throw;
            }
        }

        public DbDataReader ExecuteReader()
        {
            try
            {
                return command.ExecuteReaderWithRetry(retryPolicy);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                Log.DebugException($"Exception in relational transaction '{transaction.Name}'", ex);
                throw;
            }
        }
        
        public async Task<DbDataReader> ExecuteReaderAsync()
        {
            try
            {
                return await command.ExecuteReaderAsyncWithRetry(retryPolicy);
            }
            catch (SqlException ex)
            {
                DetectAndThrowIfKnownException(ex, prepared.Mapping);
                throw WrapException(ex);
            }
            catch (Exception ex)
            {
                Log.DebugException($"Exception in relational transaction '{transaction.Name}'", ex);
                throw;
            }
        }
            
        Exception WrapException(Exception ex)
        {
            if (ex is SqlException sqlEx && sqlEx.Number == 1205) // deadlock
            {
                var builder = new StringBuilder();
                builder.AppendLine(ex.Message);
                builder.AppendLine("Current transactions: ");
                transaction.WriteCurrentTransactions(builder);
                throw new Exception(builder.ToString());
            }

            Log.DebugException($"Error while executing SQL command in transaction '{transaction.Name}'", ex);

            return new Exception($"Error while executing SQL command in transaction '{transaction.Name}': {ex.Message}{Environment.NewLine}The command being executed was:{Environment.NewLine}{command.CommandText}", ex);
        }

        static void DetectAndThrowIfKnownException(SqlException ex, DocumentMap mapping)
        {
            if (mapping == null) 
                return;
            if (ex.Number == 2627 || ex.Number == 2601)
            {
                var uniqueRule = mapping.UniqueConstraints.FirstOrDefault(u => ex.Message.Contains((string) u.ConstraintName));
                if (uniqueRule != null)
                {
                    throw new UniqueConstraintViolationException(uniqueRule.Message);
                }
            }
        }
            
        public void Dispose()
        {
            timedSection?.Dispose();
            command?.Dispose();
        }
    }
}
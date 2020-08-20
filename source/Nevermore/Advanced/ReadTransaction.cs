using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Nevermore.Diagnositcs;
using Nevermore.Diagnostics;
using Nevermore.Querying.AST;
using Nevermore.Transient;

namespace Nevermore.Advanced
{
    [DebuggerDisplay("{" + nameof(ToString) + "()}")]
    public class ReadTransaction : IReadTransaction, ITransactionDiagnostic
    {
        static readonly ILog Log = LogProvider.For<ReadTransaction>();

        readonly RelationalTransactionRegistry registry;
        readonly RetriableOperation operationsToRetry;
        readonly IRelationalStoreConfiguration configuration;
        readonly ITableAliasGenerator tableAliasGenerator = new TableAliasGenerator();
        readonly string name;

        DbConnection connection;

        protected IUniqueParameterNameGenerator ParameterNameGenerator { get; } = new UniqueParameterNameGenerator();

        // To help track deadlocks
        readonly List<string> commandTrace = new List<string>();

        public DateTimeOffset CreatedTime { get; } = DateTimeOffset.Now;

        public IDictionary<string, object> State { get; }

        public ReadTransaction(RelationalTransactionRegistry registry, RetriableOperation operationsToRetry, IRelationalStoreConfiguration configuration, string name = null)
        {
            State = new Dictionary<string, object>();
            this.registry = registry;
            this.operationsToRetry = operationsToRetry;
            this.configuration = configuration;
            this.name = name ?? Thread.CurrentThread.Name;
            if (string.IsNullOrEmpty(name))
                this.name = "<unknown>";
            registry.Add(this);
        }

        protected DbTransaction Transaction { get; private set; }

        public void Open()
        {
            if (!configuration.AllowSynchronousOperations)
                throw new SynchronousOperationsDisabledException();

            connection = new SqlConnection(registry.ConnectionString);
            connection.OpenWithRetry();
        }

        public async Task OpenAsync()
        {
            connection = new SqlConnection(registry.ConnectionString);
            await connection.OpenWithRetryAsync();
        }

        public void Open(IsolationLevel isolationLevel)
        {
            Open();
            Transaction = connection.BeginTransaction(isolationLevel);
        }

        public async Task OpenAsync(IsolationLevel isolationLevel)
        {
            await OpenAsync();
            Transaction = await connection.BeginTransactionAsync(isolationLevel);
        }

        [Pure]
        public TDocument Load<TDocument>(string id) where TDocument : class
        {
            return Stream<TDocument>(PrepareLoad<TDocument>(id)).FirstOrDefault();
        }

        public async Task<TDocument> LoadAsync<TDocument>(string id, CancellationToken cancellationToken = default) where TDocument : class
        {
            var results = StreamAsync<TDocument>(PrepareLoad<TDocument>(id), cancellationToken);
            await foreach (var row in results.WithCancellation(cancellationToken))
                return row;
            return null;
        }

        public List<TDocument> Load<TDocument>(IEnumerable<string> ids) where TDocument : class
            => LoadStream<TDocument>(ids).ToList();

        [Pure]
        public async Task<List<TDocument>> LoadAsync<TDocument>(IEnumerable<string> ids, CancellationToken cancellationToken = default) where TDocument : class
        {
            var results = new List<TDocument>();
            await foreach (var item in LoadStreamAsync<TDocument>(ids, cancellationToken))
            {
                results.Add(item);
            }

            return results;
        }

        [Pure]
        public TDocument LoadRequired<TDocument>(string id) where TDocument : class
        {
            var result = Load<TDocument>(id);
            if (result == null)
                throw new ResourceNotFoundException(id);
            return result;
        }

        [Pure]
        public async Task<TDocument> LoadRequiredAsync<TDocument>(string id, CancellationToken cancellationToken = default) where TDocument : class
        {
            var result = await LoadAsync<TDocument>(id, cancellationToken);
            if (result == null)
                throw new ResourceNotFoundException(id);
            return result;
        }

        public List<TDocument> LoadRequired<TDocument>(IEnumerable<string> ids) where TDocument : class
        {
            var idList = ids.Distinct().ToList();
            var results = Load<TDocument>(idList);
            if (results.Count != idList.Count)
            {
                var firstMissing = idList.FirstOrDefault(id => results.All(record => configuration.DocumentMaps.GetId(record) != id));
                throw new ResourceNotFoundException(firstMissing);
            }

            return results;
        }

        public async Task<List<TDocument>> LoadRequiredAsync<TDocument>(IEnumerable<string> ids, CancellationToken cancellationToken = default) where TDocument : class
        {
            var idList = ids.Distinct().ToList();
            var results = await LoadAsync<TDocument>(idList, cancellationToken);
            if (results.Count != idList.Count)
            {
                var firstMissing = idList.FirstOrDefault(id => results.All(record => configuration.DocumentMaps.GetId(record) != id));
                throw new ResourceNotFoundException(firstMissing);
            }

            return results;
        }

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(IEnumerable<string> ids) where TDocument : class
        {
            var idList = ids.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            return idList.Count == 0 ? new List<TDocument>() : Stream<TDocument>(PrepareLoadMany<TDocument>(idList));
        }

        [Pure]
        public async IAsyncEnumerable<TDocument> LoadStreamAsync<TDocument>(IEnumerable<string> ids, [EnumeratorCancellation] CancellationToken cancellationToken = default) where TDocument : class
        {
            var idList = ids.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            if (idList.Count == 0)
                yield break;

            await foreach (var item in StreamAsync<TDocument>(PrepareLoadMany<TDocument>(idList), cancellationToken))
            {
                yield return item;
            }
        }

        public ITableSourceQueryBuilder<TRecord> Query<TRecord>() where TRecord : class
        {
            var map = configuration.DocumentMaps.Resolve(typeof(TRecord));
            return new TableSourceQueryBuilder<TRecord>(map.TableName, configuration.GetSchemaNameOrDefault(map), map.IdColumn.ColumnName, this, tableAliasGenerator, ParameterNameGenerator, new CommandParameterValues(), new Parameters(), new ParameterDefaults());
        }

        public ISubquerySourceBuilder<TRecord> RawSqlQuery<TRecord>(string query) where TRecord : class
        {
            return new SubquerySourceBuilder<TRecord>(new RawSql(query), this, tableAliasGenerator, ParameterNameGenerator, new CommandParameterValues(), new Parameters(), new ParameterDefaults());
        }

        public IEnumerable<TRecord> Stream<TRecord>(string query, CommandParameterValues args, TimeSpan? commandTimeout = null)
        {
            return Stream<TRecord>(new PreparedCommand(query, args, RetriableOperation.Select, commandBehavior: CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, commandTimeout: commandTimeout));
        }

        public IAsyncEnumerable<TRecord> StreamAsync<TRecord>(string query, CommandParameterValues args = null, TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return StreamAsync<TRecord>(new PreparedCommand(query, args, RetriableOperation.Select, commandBehavior: CommandBehavior.SequentialAccess | CommandBehavior.SingleResult, commandTimeout: commandTimeout), cancellationToken);
        }

        public IEnumerable<TRecord> Stream<TRecord>(PreparedCommand command)
        {
            using var reader = ExecuteReader(command);
            foreach (var item in ProcessReader<TRecord>(reader, command))
                yield return item;
        }

        public async IAsyncEnumerable<TRecord> StreamAsync<TRecord>(PreparedCommand command, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            await using var reader = await ExecuteReaderAsync(command, cancellationToken);
            await foreach (var result in ProcessReaderAsync<TRecord>(reader, command, cancellationToken))
                yield return result;
        }

        IEnumerable<TRecord> ProcessReader<TRecord>(DbDataReader reader, PreparedCommand command)
        {
            using var timed = new TimedSection(ms => configuration.QueryLogger.ProcessReader(ms, name, command.Statement));
            var strategy = configuration.ReaderStrategies.Resolve<TRecord>(command);
            while (reader.Read())
            {
                var (instance, success) = strategy(reader);
                if (success)
                    yield return instance;
            }
        }

        async IAsyncEnumerable<TRecord> ProcessReaderAsync<TRecord>(DbDataReader reader, PreparedCommand command, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            using var timed = new TimedSection(ms => configuration.QueryLogger.ProcessReader(ms, name, command.Statement));
            var strategy = configuration.ReaderStrategies.Resolve<TRecord>(command);

            while (await reader.ReadAsync(cancellationToken))
            {
                var (instance, success) = strategy(reader);
                if (success)
                    yield return instance;
            }
        }

        public IEnumerable<TResult> Stream<TResult>(string query, CommandParameterValues args, Func<IProjectionMapper, TResult> projectionMapper, TimeSpan? commandTimeout = null)
        {
            var command = new PreparedCommand(query, args, RetriableOperation.Select, commandBehavior: CommandBehavior.Default, commandTimeout: commandTimeout);
            using var reader = ExecuteReader(command);
            var mapper = new ProjectionMapper(command, reader, configuration.ReaderStrategies);
            while (reader.Read())
            {
                yield return projectionMapper(mapper);
            }
        }

        public async IAsyncEnumerable<TResult> StreamAsync<TResult>(string query, CommandParameterValues args, Func<IProjectionMapper, TResult> projectionMapper, TimeSpan? commandTimeout = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var command = new PreparedCommand(query, args, RetriableOperation.Select, commandBehavior: CommandBehavior.Default, commandTimeout: commandTimeout);
            await using var reader = await ExecuteReaderAsync(command, cancellationToken);
            var mapper = new ProjectionMapper(command, reader, configuration.ReaderStrategies);
            while (await reader.ReadAsync(cancellationToken))
            {
                yield return projectionMapper(mapper);
            }
        }

        void AddCommandTrace(string commandText)
        {
            lock (commandTrace)
            {
                if (commandTrace.Count == 100)
                    Log.DebugFormat("A possible N+1 or long running transaction detected, this is a diagnostic message only does not require end-user action.\r\nStarted: {0:s}\r\nStack: {1}\r\n\r\n{2}", CreatedTime, Environment.StackTrace, string.Join("\r\n", commandTrace));

                if (commandTrace.Count <= 200)
                    commandTrace.Add(DateTime.Now.ToString("s") + " " + commandText);
            }
        }

        public int ExecuteNonQuery(string query, CommandParameterValues args, TimeSpan? commandTimeout = null)
        {
            return ExecuteNonQuery(new PreparedCommand(query, args, RetriableOperation.None, commandBehavior: CommandBehavior.Default, commandTimeout: commandTimeout));
        }

        public Task<int> ExecuteNonQueryAsync(string query, CommandParameterValues args = null, TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return ExecuteNonQueryAsync(new PreparedCommand(query, args, RetriableOperation.None, commandBehavior: CommandBehavior.Default, commandTimeout: commandTimeout), cancellationToken);
        }

        public int ExecuteNonQuery(PreparedCommand preparedCommand)
        {
            using var command = CreateCommand(preparedCommand);
            return command.ExecuteNonQuery();
        }

        public async Task<int> ExecuteNonQueryAsync(PreparedCommand preparedCommand, CancellationToken cancellationToken = default)
        {
            using var command = CreateCommand(preparedCommand);
            return await command.ExecuteNonQueryAsync(cancellationToken);
        }

        public TResult ExecuteScalar<TResult>(string query, CommandParameterValues args, RetriableOperation operation, TimeSpan? commandTimeout = null)
        {
            return ExecuteScalar<TResult>(new PreparedCommand(query, args, operation, commandTimeout: commandTimeout));
        }

        public Task<TResult> ExecuteScalarAsync<TResult>(string query, CommandParameterValues args = null, RetriableOperation retriableOperation = RetriableOperation.Select, TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return ExecuteScalarAsync<TResult>(new PreparedCommand(query, args, retriableOperation, null, commandTimeout), cancellationToken);
        }

        public TResult ExecuteScalar<TResult>(PreparedCommand preparedCommand)
        {
            using var command = CreateCommand(preparedCommand);
            var result = command.ExecuteScalar();
            if (result == DBNull.Value)
                return default;
            return (TResult) result;
        }

        public async Task<TResult> ExecuteScalarAsync<TResult>(PreparedCommand preparedCommand, CancellationToken cancellationToken = default)
        {
            using var command = CreateCommand(preparedCommand);
            var result = await command.ExecuteScalarAsync(cancellationToken);
            if (result == DBNull.Value)
                return default;
            return (TResult) result;
        }

        public DbDataReader ExecuteReader(string query, CommandParameterValues args = null, TimeSpan? commandTimeout = null)
        {
            return ExecuteReader(new PreparedCommand(query, args, RetriableOperation.Select, commandTimeout: commandTimeout));
        }

        public Task<DbDataReader> ExecuteReaderAsync(string query, CommandParameterValues args = null, TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default)
        {
            return ExecuteReaderAsync(new PreparedCommand(query, args, RetriableOperation.Select, commandTimeout: commandTimeout), cancellationToken);
        }

        public DbDataReader ExecuteReader(PreparedCommand preparedCommand)
        {
            using var command = CreateCommand(preparedCommand);
            return command.ExecuteReader();
        }

        public async Task<DbDataReader> ExecuteReaderAsync(PreparedCommand preparedCommand, CancellationToken cancellationToken = default)
        {
            using var command = CreateCommand(preparedCommand);
            return await command.ExecuteReaderAsync(cancellationToken);
        }

        PreparedCommand PrepareLoad<TDocument>(string id)
        {
            var mapping = configuration.DocumentMaps.Resolve(typeof(TDocument));
            var tableName = mapping.TableName;
            var args = new CommandParameterValues {{"Id", id}};
            return new PreparedCommand($"SELECT TOP 1 * FROM [{configuration.GetSchemaNameOrDefault(mapping)}].[{tableName}] WHERE [{mapping.IdColumn.ColumnName}] = @Id", args, RetriableOperation.Select, mapping, commandBehavior: CommandBehavior.SingleResult | CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
        }

        PreparedCommand PrepareLoadMany<TDocument>(IEnumerable<string> idList)
        {
            var mapping = configuration.DocumentMaps.Resolve(typeof(TDocument));
            var tableName = mapping.TableName;

            var param = new CommandParameterValues();
            param.AddTable("criteriaTable", idList);
            var statement = $"SELECT s.* FROM [{configuration.GetSchemaNameOrDefault(mapping)}].[{tableName}] s INNER JOIN @criteriaTable t on t.[ParameterValue] = s.[{mapping.IdColumn.ColumnName}] order by s.[{mapping.IdColumn.ColumnName}]";
            return new PreparedCommand(statement, param, RetriableOperation.Select, mapping, commandBehavior: CommandBehavior.SingleResult | CommandBehavior.SequentialAccess);
        }

        CommandExecutor CreateCommand(PreparedCommand command)
        {
            var timedSection = new TimedSection(ms => LogBasedOnCommand(ms, command));
            var sqlCommand = configuration.CommandFactory.CreateCommand(connection, Transaction, command.Statement, command.ParameterValues, configuration.TypeHandlers, command.Mapping, command.CommandTimeout);
            AddCommandTrace(command.Statement);
            if (command.ParameterValues != null)
            {
                var keys = command.ParameterValues.Keys.ToArray();
                ParameterNameGenerator.Return(keys);
            }

            if (configuration.DetectQueryPlanThrashing)
            {
                QueryPlanThrashingDetector.Detect(command.Statement);
            }

            return new CommandExecutor(sqlCommand, command, GetRetryPolicy(command.Operation), timedSection, this, configuration.AllowSynchronousOperations);
        }

        RetryPolicy GetRetryPolicy(RetriableOperation operation)
        {
            if (operationsToRetry == RetriableOperation.None)
                return RetryPolicy.NoRetry;

            return (operationsToRetry & operation) != 0 ? RetryManager.Instance.GetDefaultSqlCommandRetryPolicy() : RetryPolicy.NoRetry;
        }

        internal void WriteDebugInfoTo(StringBuilder sb)
        {
            string[] copy;
            lock (commandTrace)
                copy = commandTrace.ToArray();

            sb.AppendLine($"Transaction '{name}' {connection?.State} with {copy.Length} commands started at {CreatedTime:s} ({(DateTime.Now - CreatedTime).TotalSeconds:n2} seconds ago)");
            foreach (var command in copy)
                sb.AppendLine(command);
        }
         
        void LogBasedOnCommand(long duration, PreparedCommand command)
        {
            switch (command.Operation)
            {
                case RetriableOperation.Insert:
                    configuration.QueryLogger.Insert(duration, name, command.Statement);
                    break;
                case RetriableOperation.Update:
                    configuration.QueryLogger.Update(duration, name, command.Statement);
                    break;
                case RetriableOperation.Delete:
                    configuration.QueryLogger.Delete(duration, name, command.Statement);
                    break;
                case RetriableOperation.Select:
                    configuration.QueryLogger.ExecuteReader(duration, name, command.Statement);
                    break;
                case RetriableOperation.All:
                case RetriableOperation.None:
                default:
                    configuration.QueryLogger.NonQuery(duration, name, command.Statement);
                    break;
            }
        }

        public override string ToString()
        {
            return $"{CreatedTime} - {connection?.State} - {name}";
        }

        string ITransactionDiagnostic.Name => name;

        void ITransactionDiagnostic.WriteCurrentTransactions(StringBuilder output)
        {
            registry.WriteCurrentTransactions(output);
        }

        public void Dispose()
        {
            Transaction?.Dispose();
            connection?.Dispose();
            registry.Remove(this);
        }
    }
}
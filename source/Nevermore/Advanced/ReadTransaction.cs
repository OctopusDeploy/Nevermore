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
using Nevermore.Util;

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
        public TDocument Load<TDocument, TKey>(TKey id) where TDocument : class
        {
            return Stream<TDocument>(PrepareLoad<TDocument, TKey>(id)).FirstOrDefault();
        }

        [Pure]
        public TDocument Load<TDocument>(string id) where TDocument : class => Load<TDocument, string>(id);

        [Pure]
        public TDocument Load<TDocument>(int id) where TDocument : class => Load<TDocument, int>(id);

        [Pure]
        public TDocument Load<TDocument>(long id) where TDocument : class => Load<TDocument, long>(id);

        [Pure]
        public TDocument Load<TDocument>(Guid id) where TDocument : class => Load<TDocument, Guid>(id);

        private async Task<TDocument> LoadAsync<TDocument, TKey>(TKey id, CancellationToken cancellationToken = default) where TDocument : class
        {
            var results = StreamAsync<TDocument>(PrepareLoad<TDocument, TKey>(id), cancellationToken);
            await foreach (var row in results.WithCancellation(cancellationToken))
                return row;
            return null;
        }

        [Pure]
        public Task<TDocument> LoadAsync<TDocument>(string id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadAsync<TDocument, string>(id, cancellationToken);

        [Pure]
        public Task<TDocument> LoadAsync<TDocument>(int id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadAsync<TDocument, int>(id, cancellationToken);

        [Pure]
        public Task<TDocument> LoadAsync<TDocument>(long id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadAsync<TDocument, long>(id, cancellationToken);

        [Pure]
        public Task<TDocument> LoadAsync<TDocument>(Guid id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadAsync<TDocument, Guid>(id, cancellationToken);

        private List<TDocument> LoadMany<TDocument, TKey>(IEnumerable<TKey> ids) where TDocument : class
            => LoadStream<TDocument, TKey>(ids).ToList();

        public List<TDocument> LoadMany<TDocument>(params string[] ids) where TDocument : class
            => LoadStream<TDocument>(ids).ToList();

        public List<TDocument> LoadMany<TDocument>(params int[] ids) where TDocument : class
          => LoadStream<TDocument>(ids).ToList();

        public List<TDocument> LoadMany<TDocument>(params long[] ids) where TDocument : class
          => LoadStream<TDocument>(ids).ToList();

        public List<TDocument> LoadMany<TDocument>(params Guid[] ids) where TDocument : class
          => LoadStream<TDocument>(ids).ToList();

        public List<TDocument> LoadMany<TDocument>(IEnumerable<string> ids) where TDocument : class
            => LoadStream<TDocument>(ids).ToList();

        public List<TDocument> LoadMany<TDocument>(IEnumerable<int> ids) where TDocument : class
           => LoadStream<TDocument>(ids).ToList();

        public List<TDocument> LoadMany<TDocument>(IEnumerable<long> ids) where TDocument : class
           => LoadStream<TDocument>(ids).ToList();

        public List<TDocument> LoadMany<TDocument>(IEnumerable<Guid> ids) where TDocument : class
           => LoadStream<TDocument>(ids).ToList();

        [Pure]
        private async Task<List<TDocument>> LoadManyAsync<TDocument, TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default) where TDocument : class
        {
            var results = new List<TDocument>();
            await foreach (var item in LoadStreamAsync<TDocument, TKey>(ids, cancellationToken))
            {
                results.Add(item);
            }

            return results;
        }

        [Pure]
        public Task<List<TDocument>> LoadManyAsync<TDocument>(IEnumerable<string> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyAsync<TDocument, string>(ids, cancellationToken);

        [Pure]
        public Task<List<TDocument>> LoadManyAsync<TDocument>(IEnumerable<int> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyAsync<TDocument, int>(ids, cancellationToken);

        [Pure]
        public Task<List<TDocument>> LoadManyAsync<TDocument>(IEnumerable<long> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyAsync<TDocument, long>(ids, cancellationToken);

        [Pure]
        public Task<List<TDocument>> LoadManyAsync<TDocument>(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyAsync<TDocument, Guid>(ids, cancellationToken);

        [Pure]
        private TDocument LoadRequired<TDocument, TKey>(TKey id) where TDocument : class
        {
            var result = Load<TDocument, TKey>(id);
            if (result == null)
                throw new ResourceNotFoundException(id);
            return result;
        }

        [Pure]
        public TDocument LoadRequired<TDocument>(string id) where TDocument : class
            => LoadRequired<TDocument, string>(id);

        [Pure]
        public TDocument LoadRequired<TDocument>(int id) where TDocument : class
            => LoadRequired<TDocument, int>(id);

        [Pure]
        public TDocument LoadRequired<TDocument>(long id) where TDocument : class
            => LoadRequired<TDocument, long>(id);

        [Pure]
        public TDocument LoadRequired<TDocument>(Guid id) where TDocument : class
            => LoadRequired<TDocument, Guid>(id);

        [Pure]
        private async Task<TDocument> LoadRequiredAsync<TDocument, TKey>(TKey id, CancellationToken cancellationToken = default) where TDocument : class
        {
            var result = await LoadAsync<TDocument, TKey>(id, cancellationToken);
            if (result == null)
                throw new ResourceNotFoundException(id);
            return result;
        }

        [Pure]
        public Task<TDocument> LoadRequiredAsync<TDocument>(string id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadRequiredAsync<TDocument, string>(id, cancellationToken);

        [Pure]
        public Task<TDocument> LoadRequiredAsync<TDocument>(int id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadRequiredAsync<TDocument, int>(id, cancellationToken);

        [Pure]
        public Task<TDocument> LoadRequiredAsync<TDocument>(long id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadRequiredAsync<TDocument, long>(id, cancellationToken);

        [Pure]
        public Task<TDocument> LoadRequiredAsync<TDocument>(Guid id, CancellationToken cancellationToken = default) where TDocument : class
            => LoadRequiredAsync<TDocument, Guid>(id, cancellationToken);

        private List<TDocument> LoadManyRequired<TDocument, TKey>(IEnumerable<TKey> ids) where TDocument : class
        {
            var idList = ids.Distinct().ToList();
            var results = LoadMany<TDocument, TKey>(idList);
            if (results.Count != idList.Count)
            {
                var firstMissing = idList.FirstOrDefault(id => results.All(record => !((TKey)configuration.DocumentMaps.GetId(record)).Equals(id)));
                throw new ResourceNotFoundException(firstMissing);
            }

            return results;
        }

        public List<TDocument> LoadManyRequired<TDocument>(params string[] ids) where TDocument : class
            => LoadManyRequired<TDocument, string>(ids);

        public List<TDocument> LoadManyRequired<TDocument>(params int[] ids) where TDocument : class
            => LoadManyRequired<TDocument, int>(ids);

        public List<TDocument> LoadManyRequired<TDocument>(params long[] ids) where TDocument : class
            => LoadManyRequired<TDocument, long>(ids);

        public List<TDocument> LoadManyRequired<TDocument>(params Guid[] ids) where TDocument : class
            => LoadManyRequired<TDocument, Guid>(ids);

        public List<TDocument> LoadManyRequired<TDocument>(IEnumerable<string> ids) where TDocument : class
           => LoadManyRequired<TDocument, string>(ids);

        public List<TDocument> LoadManyRequired<TDocument>(IEnumerable<int> ids) where TDocument : class
          => LoadManyRequired<TDocument, int>(ids);

        public List<TDocument> LoadManyRequired<TDocument>(IEnumerable<long> ids) where TDocument : class
          => LoadManyRequired<TDocument, long>(ids);

        public List<TDocument> LoadManyRequired<TDocument>(IEnumerable<Guid> ids) where TDocument : class
          => LoadManyRequired<TDocument, Guid>(ids);

        private async Task<List<TDocument>> LoadManyRequiredAsync<TDocument, TKey>(IEnumerable<TKey> ids, CancellationToken cancellationToken = default) where TDocument : class
        {
            var idList = ids.Distinct().ToArray();
            var results = await LoadManyAsync<TDocument, TKey>(idList, cancellationToken);
            if (results.Count != idList.Length)
            {
                var firstMissing = idList.FirstOrDefault(id => results.All(record => !((TKey)configuration.DocumentMaps.GetId(record)).Equals(id)));
                throw new ResourceNotFoundException(firstMissing);
            }

            return results;
        }

        public Task<List<TDocument>> LoadManyRequiredAsync<TDocument>(IEnumerable<string> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyRequiredAsync<TDocument, string>(ids, cancellationToken);

        public Task<List<TDocument>> LoadManyRequiredAsync<TDocument>(IEnumerable<int> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyRequiredAsync<TDocument, int>(ids, cancellationToken);

        public Task<List<TDocument>> LoadManyRequiredAsync<TDocument>(IEnumerable<long> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyRequiredAsync<TDocument, long>(ids, cancellationToken);

        public Task<List<TDocument>> LoadManyRequiredAsync<TDocument>(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadManyRequiredAsync<TDocument, Guid>(ids, cancellationToken);

        [Pure]
        private IEnumerable<TDocument> LoadStream<TDocument, TKey>(IEnumerable<TKey> ids) where TDocument : class
        {
            var idList = ids.Where(id => id != null).Distinct().ToList();

            return idList.Count == 0 ? new List<TDocument>() : Stream<TDocument>(PrepareLoadMany<TDocument, TKey>(idList));
        }

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(IEnumerable<string> ids) where TDocument : class
            => LoadStream<TDocument, string>(ids);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(IEnumerable<int> ids) where TDocument : class
            => LoadStream<TDocument, int>(ids);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(IEnumerable<long> ids) where TDocument : class
            => LoadStream<TDocument, long>(ids);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(IEnumerable<Guid> ids) where TDocument : class
            => LoadStream<TDocument, Guid>(ids);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(params string[] ids) where TDocument : class
            => LoadStream<TDocument, string>(ids);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(params int[] ids) where TDocument : class
            => LoadStream<TDocument, int>(ids);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(params long[] ids) where TDocument : class
            => LoadStream<TDocument, long>(ids);

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(params Guid[] ids) where TDocument : class
            => LoadStream<TDocument, Guid>(ids);

        [Pure]
        private async IAsyncEnumerable<TDocument> LoadStreamAsync<TDocument, TKey>(IEnumerable<TKey> ids, [EnumeratorCancellation] CancellationToken cancellationToken = default) where TDocument : class
        {
            var idList = ids.Where(id => id != null).Distinct().ToList();
            if (idList.Count == 0)
                yield break;

            await foreach (var item in StreamAsync<TDocument>(PrepareLoadMany<TDocument, TKey>(idList), cancellationToken))
            {
                yield return item;
            }
        }

        [Pure]
        public IAsyncEnumerable<TDocument> LoadStreamAsync<TDocument>(IEnumerable<string> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadStreamAsync<TDocument, string>(ids, cancellationToken);

        [Pure]
        public IAsyncEnumerable<TDocument> LoadStreamAsync<TDocument>(IEnumerable<int> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadStreamAsync<TDocument, int>(ids, cancellationToken);

        [Pure]
        public IAsyncEnumerable<TDocument> LoadStreamAsync<TDocument>(IEnumerable<long> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadStreamAsync<TDocument, long>(ids, cancellationToken);

        [Pure]
        public IAsyncEnumerable<TDocument> LoadStreamAsync<TDocument>(IEnumerable<Guid> ids, CancellationToken cancellationToken = default) where TDocument : class
            => LoadStreamAsync<TDocument, Guid>(ids, cancellationToken);

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

        protected TResult[] ReadResults<TResult>(PreparedCommand preparedCommand, Func<DbDataReader, TResult> mapper)
        {
            using var command = CreateCommand(preparedCommand);
            return command.ReadResults(mapper);
        }

        protected async Task<TResult[]> ReadResultsAsync<TResult>(PreparedCommand preparedCommand, Func<DbDataReader, TResult> mapper)
        {
            using var command = CreateCommand(preparedCommand);
            return await command.ReadResultsAsync(mapper);
        }

        PreparedCommand PrepareLoad<TDocument, TKey>(TKey id)
        {
            var mapping = configuration.DocumentMaps.Resolve(typeof(TDocument));

            if (mapping.IdColumn.Type != typeof(TKey))
                throw new ArgumentException($"Provided Id of type '{id.GetType().FullName}' does not match configured type of '{mapping.IdColumn.Type.FullName}'.");

            var tableName = mapping.TableName;
            var args = new CommandParameterValues {{"Id", id}};
            if (mapping.IdColumn.Type.IsStronglyTypedString())
                args = new CommandParameterValues {{"Id", id.ToString()}};
            return new PreparedCommand($"SELECT TOP 1 * FROM [{configuration.GetSchemaNameOrDefault(mapping)}].[{tableName}] WHERE [{mapping.IdColumn.ColumnName}] = @Id", args, RetriableOperation.Select, mapping, commandBehavior: CommandBehavior.SingleResult | CommandBehavior.SingleRow | CommandBehavior.SequentialAccess);
        }

        PreparedCommand PrepareLoadMany<TDocument, TKey>(IEnumerable<TKey> idList)
        {
            var mapping = configuration.DocumentMaps.Resolve(typeof(TDocument));

            if (mapping.IdColumn.Type != typeof(TKey))
                throw new ArgumentException($"Provided Id of type '{typeof(TKey).FullName}' does not match configured type of '{mapping.IdColumn.Type.FullName}'.");

            var param = new CommandParameterValues();
            param.AddTable("criteriaTable", idList.ToList());
            var statement = $"SELECT s.* FROM [{configuration.GetSchemaNameOrDefault(mapping)}].[{mapping.TableName}] s INNER JOIN @criteriaTable t on t.[ParameterValue] = s.[{mapping.IdColumn.ColumnName}] order by s.[{mapping.IdColumn.ColumnName}]";
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
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Nevermore.AST;
using Nevermore.Contracts;
using Nevermore.Diagnositcs;
using Nevermore.Diagnostics;
using Nevermore.Mapping;
using Nevermore.Transient;
using Nevermore.Util;
using Newtonsoft.Json;

namespace Nevermore
{
    [DebuggerDisplay("{ToString()}")]
    public class ReadTransaction : IReadTransaction, ITransactionDiagnostic
    {
        // Getting a typed ILog causes JIT compilation - we should only do this once
        static readonly ILog Log = LogProvider.For<ReadTransaction>();

        readonly RelationalTransactionRegistry registry;
        readonly RetriableOperation operationsToRetry;
        readonly RelationalStoreConfiguration configuration;
        DbConnection connection;
        protected DbTransaction transaction;
        readonly string name;
        readonly ITableAliasGenerator tableAliasGenerator = new TableAliasGenerator();
        protected readonly IUniqueParameterNameGenerator uniqueParameterNameGenerator = new UniqueParameterNameGenerator();

        // To help track deadlocks
        readonly List<string> commandTrace = new List<string>();

        public DateTimeOffset CreatedTime { get; } = DateTimeOffset.Now;
        
        public ReadTransaction(RelationalTransactionRegistry registry, RetriableOperation operationsToRetry, RelationalStoreConfiguration configuration, string name = null)
        {
            this.registry = registry;
            this.operationsToRetry = operationsToRetry;
            this.configuration = configuration;
            this.name = name ?? Thread.CurrentThread.Name;
            if (string.IsNullOrEmpty(name))
                this.name = "<unknown>";
            registry.Add(this);
        }

        public void Open(IsolationLevel isolationLevel)
        {
            connection = new SqlConnection(registry.ConnectionString);
            connection.OpenWithRetry();
            transaction = connection.BeginTransaction(isolationLevel);
        }

        public async Task OpenAsync(IsolationLevel isolationLevel)
        {
            connection = new SqlConnection(registry.ConnectionString);
            await connection.OpenWithRetryAsync();
            transaction = connection.BeginTransaction(isolationLevel);
        }
        
        [Pure]
        public TDocument Load<TDocument>(string id) where TDocument : class, IId
        {
            return Stream<TDocument>(PrepareLoad<TDocument>(id)).FirstOrDefault();
        }

        public async Task<TDocument> LoadAsync<TDocument>(string id) where TDocument : class, IId
        {
            var results = StreamAsync<TDocument>(PrepareLoad<TDocument>(id));
            await foreach (var row in results)
                return row;
            return null;
        }

        public List<TDocument> Load<TDocument>(IEnumerable<string> ids) where TDocument : class, IId
            => LoadStream<TDocument>(ids).ToList();

        [Pure]
        public async Task<List<TDocument>> LoadAsync<TDocument>(IEnumerable<string> ids) where TDocument : class, IId
        {
            var results = new List<TDocument>();
            await foreach (var item in LoadStreamAsync<TDocument>(ids))
            {
                results.Add(item);
            }

            return results;
        }

        [Pure]
        public TDocument LoadRequired<TDocument>(string id) where TDocument : class, IId
        {
            var result = Load<TDocument>(id);
            if (result == null)
                throw new ResourceNotFoundException(id);
            return result;
        }
        
        [Pure]
        public async Task<TDocument> LoadRequiredAsync<TDocument>(string id) where TDocument : class, IId
        {
            var result = await LoadAsync<TDocument>(id);
            if (result == null)
                throw new ResourceNotFoundException(id);
            return result;
        }

        public List<TDocument> LoadRequired<TDocument>(IEnumerable<string> ids) where TDocument : class, IId
        {
            var idList = ids.Distinct().ToList();
            var results = Load<TDocument>(idList);
            if (results.Count != idList.Count)
            {
                var firstMissing = idList.FirstOrDefault(id => results.All(r => r.Id != id));
                throw new ResourceNotFoundException(firstMissing);
            }
            
            return results;
        }

        [Pure]
        public IEnumerable<TDocument> LoadStream<TDocument>(IEnumerable<string> ids) where TDocument : class, IId
        {
            var idList = ids.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            return idList.Count == 0 ? new List<TDocument>() : Stream<TDocument>(PrepareLoadMany<TDocument>(idList));
        }
        
        [Pure]
        public async IAsyncEnumerable<TDocument> LoadStreamAsync<TDocument>(IEnumerable<string> ids) where TDocument : class, IId
        {
            var idList = ids.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            if (idList.Count == 0)
                yield break;

            await foreach (var item in StreamAsync<TDocument>(PrepareLoadMany<TDocument>(idList)))
            {
                yield return item;
            }
        }

        public ITableSourceQueryBuilder<TRecord> TableQuery<TRecord>() where TRecord : class
        {
            return new TableSourceQueryBuilder<TRecord>(configuration.Mappings.Resolve(typeof(TRecord)).TableName, this, tableAliasGenerator, uniqueParameterNameGenerator, new CommandParameterValues(), new Parameters(), new ParameterDefaults());
        }

        public ISubquerySourceBuilder<TRecord> RawSqlQuery<TRecord>(string query) where TRecord : class
        {
            return new SubquerySourceBuilder<TRecord>(new RawSql(query), this, tableAliasGenerator, uniqueParameterNameGenerator, new CommandParameterValues(), new Parameters(), new ParameterDefaults());
        }

        public int ExecuteNonQuery(string query, CommandParameterValues args, TimeSpan? commandTimeout = null)
        {
            return ExecuteNonQuery(new PreparedCommand(query, args, RetriableOperation.None, commandTimeout: commandTimeout));
        }

        public IEnumerable<TRecord> Stream<TRecord>(string query, CommandParameterValues args, TimeSpan? commandTimeout = null)
        {
            return Stream<TRecord>(new PreparedCommand(query, args, RetriableOperation.Select, commandTimeout: commandTimeout));
        }

        public IEnumerable<TRecord> Stream<TRecord>(PreparedCommand command)
        {
            using var reader = ExecuteReader(command);
            foreach (var item in ProcessReader<TRecord>(reader, command))
                yield return item;
        }
        
        public async IAsyncEnumerable<TRecord> StreamAsync<TRecord>(PreparedCommand command)
        {
            await using var reader = await ExecuteReaderAsync(command);
            await foreach (var result in ProcessReaderAsync<TRecord>(reader, command))
                yield return result;
        }

        IEnumerable<TRecord> ProcessReader<TRecord>(DbDataReader reader, PreparedCommand command)
        {
            using var timed = new TimedSection(Log, ms => $"Reader took {ms}ms to process the results set in transaction '{name}'", 300);
            var strategy = configuration.ReaderStrategyRegistry.Resolve<TRecord>(command);
            while (reader.Read())
            {
                var (instance, success) = strategy(reader);
                if (success)
                    yield return instance;
            }
        }

        async IAsyncEnumerable<TRecord> ProcessReaderAsync<TRecord>(DbDataReader reader, PreparedCommand command)
        {
            using var timed = new TimedSection(Log, ms => $"Reader took {ms}ms to process the results set in transaction '{name}'", 300);
            var strategy = configuration.ReaderStrategyRegistry.Resolve<TRecord>(command);

            while (await reader.ReadAsync())
            {
                var (instance, success) = strategy(reader);
                if (success)
                    yield return instance;
                yield return instance;
            }
        }
        
        public IEnumerable<TResult> Stream<TResult>(string query, CommandParameterValues args, Func<IProjectionMapper, TResult> projectionMapper, TimeSpan? commandTimeout = null)
        {
            using var reader = ExecuteReader(new PreparedCommand(query, args, RetriableOperation.Select, commandTimeout: commandTimeout));
            var mapper = new ProjectionMapper(reader, configuration.JsonSerializerSettings, configuration.Mappings);
            while (reader.Read())
            {
                yield return projectionMapper(mapper);
            }
        }

        public async IAsyncEnumerable<TResult> StreamAsync<TResult>(string query, CommandParameterValues args, Func<IProjectionMapper, TResult> projectionMapper, TimeSpan? commandTimeout = null)
        {
            await using var reader = ExecuteReader(new PreparedCommand(query, args, RetriableOperation.Select, commandTimeout: commandTimeout));
            var mapper = new ProjectionMapper(reader, configuration.JsonSerializerSettings, configuration.Mappings);
            while (await reader.ReadAsync())
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

        [Pure]
        public T ExecuteScalar<T>(string query, CommandParameterValues args, RetriableOperation operation, TimeSpan? commandTimeout = null)
        {
            return ExecuteScalar<T>(new PreparedCommand(query, args, operation, commandTimeout: commandTimeout));
        }

        public void ExecuteReader(string query, Action<IDataReader> readerCallback, TimeSpan? commandTimeout = null)
        {
            ExecuteReader(query, null, readerCallback, commandTimeout);
        }

        public void ExecuteReader(string query, object args, Action<IDataReader> readerCallback, TimeSpan? commandTimeout = null)
        {
            ExecuteReader(query, new CommandParameterValues(args), readerCallback, commandTimeout);
        }

        public void ExecuteReader(string query, CommandParameterValues args, Action<IDataReader> readerCallback, TimeSpan? commandTimeout = null)
        {
            using var reader = ExecuteReader(new PreparedCommand(query, args, commandTimeout: commandTimeout));
            readerCallback(reader);
        }

        public int ExecuteNonQuery(PreparedCommand preparedCommand)
        {
            using var command = CreateCommand(preparedCommand);
            return command.ExecuteNonQuery();
        }

        public async Task<int> ExecuteNonQueryAsync(PreparedCommand preparedCommand)
        {
            using var command = CreateCommand(preparedCommand);
            return await command.ExecuteNonQueryAsync();
        }
        
        public T ExecuteScalar<T>(PreparedCommand preparedCommand)
        {
            using var command = CreateCommand(preparedCommand);
            var result = command.ExecuteScalar();
            return (T) AmazingConverter.Convert(result, typeof(T));
        }
        
        public async Task<T> ExecuteScalarAsync<T>(PreparedCommand preparedCommand)
        {
            using var command = CreateCommand(preparedCommand);
            var result = await command.ExecuteScalarAsync();
            return (T) AmazingConverter.Convert(result, typeof(T));
        }
        
        public DbDataReader ExecuteReader(PreparedCommand preparedCommand)
        {
            using var command = CreateCommand(preparedCommand);
            return command.ExecuteReader();
        }
        
        public async Task<DbDataReader> ExecuteReaderAsync(PreparedCommand preparedCommand)
        {
            using var command = CreateCommand(preparedCommand);
            return await command.ExecuteReaderAsync();
        }

        PreparedCommand PrepareLoad<TDocument>(string id)
        {
            var mapping = configuration.Mappings.Resolve(typeof(TDocument));
            var tableName = mapping.TableName;
            var args = new CommandParameterValues {{"Id", id}};
            return new PreparedCommand($"SELECT TOP 1 * FROM dbo.[{tableName}] WHERE [Id] = @Id", args, RetriableOperation.Select, mapping);
        }

        PreparedCommand PrepareLoadMany<TDocument>(IEnumerable<string> idList)
        {
            var mapping = configuration.Mappings.Resolve(typeof(TDocument));
            var tableName = mapping.TableName;
            
            var param = new CommandParameterValues();
            param.AddTable("criteriaTable", idList);
            var statement = $"SELECT s.* FROM dbo.[{tableName}] s INNER JOIN @criteriaTable t on t.[ParameterValue] = s.[Id]";
            return new PreparedCommand(statement, param, RetriableOperation.Select, mapping);
        }
        
        CommandExecutor CreateCommand(PreparedCommand command)
        {
            var operationName = command.Operation == RetriableOperation.None || command.Operation == RetriableOperation.All ? "Custom query" : command.Operation.ToString();
            var timedSection = new TimedSection(Log, ms => $"{operationName} took {ms}ms in transaction '{name}': {command.Statement}", 300);
            var sqlCommand = configuration.CommandFactory.CreateCommand(connection, transaction, command.Statement, command.ParameterValues, command.Mapping, command.CommandTimeout);
            AddCommandTrace(command.Statement);
            return new CommandExecutor(sqlCommand, command, GetRetryPolicy(command.Operation), timedSection, this);
        }
        
        static int GetOrdinal(IDataReader dr, string columnName)
        {
            for (var i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
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
            transaction?.Dispose();
            connection?.Dispose();
            registry.Remove(this);
        }
                
        class ProjectionMapper : IProjectionMapper
        {
            readonly IDataReader reader;
            readonly JsonSerializerSettings jsonSerializerSettings;
            readonly IDocumentMapRegistry mappings;

            public ProjectionMapper(IDataReader reader, JsonSerializerSettings jsonSerializerSettings, IDocumentMapRegistry mappings)
            {
                this.mappings = mappings;
                this.reader = reader;
                this.jsonSerializerSettings = jsonSerializerSettings;
            }

            public TResult Map<TResult>(string prefix)
            {
                var mapping = mappings.Resolve(typeof(TResult));
                var json = reader[GetColumnName(prefix, "JSON")].ToString();

                var instanceType = mapping.InstanceTypeResolver.TypeResolverFromReader((colName) => GetOrdinal(reader, GetColumnName(prefix, colName)))(reader);

                var instance = JsonConvert.DeserializeObject(json, instanceType, jsonSerializerSettings);
                foreach (var column in mappings.Resolve(instance.GetType()).IndexedColumns)
                {
                    column.ReaderWriter.Write(instance, reader[GetColumnName(prefix, column.ColumnName)]);
                }

                mapping.IdColumn.ReaderWriter.Write(instance, reader[GetColumnName(prefix, mapping.IdColumn.ColumnName)]);

                return (TResult) instance;
            }

            public TColumn Read<TColumn>(Func<IDataReader, TColumn> callback)
            {
                return callback(reader);
            }

            public void Read(Action<IDataReader> callback)
            {
                callback(reader);
            }

            string GetColumnName(string prefix, string name)
            {
                return string.IsNullOrWhiteSpace(prefix) ? name : prefix + "_" + name;
            }
        }
    }
}
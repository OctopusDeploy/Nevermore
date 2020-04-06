using System;
using System.Collections.Generic;
using System.Data;
#if NETFRAMEWORK
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading;
using Nevermore.AST;
using Nevermore.Contracts;
using Nevermore.Diagnositcs;
using Nevermore.Diagnostics;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;
using Nevermore.Transient;
using Nevermore.Util;
using Newtonsoft.Json;

namespace Nevermore
{
    public class ReadRelationalTransaction : IReadTransaction
    {
        // Getting a typed ILog causes JIT compilation - we should only do this once
        internal static readonly ILog Log = LogProvider.For<RelationalTransaction>();

        protected readonly RelationalTransactionRegistry registry;
        protected readonly RetriableOperation retriableOperation;
        protected readonly JsonSerializerSettings jsonSerializerSettings;
        protected readonly RelationalMappings mappings;
        protected readonly IDbConnection connection;
        protected readonly IDbTransaction transaction;
        protected readonly ISqlCommandFactory sqlCommandFactory;
        protected readonly IRelatedDocumentStore relatedDocumentStore;
        protected readonly string name;
        protected readonly ITableAliasGenerator tableAliasGenerator = new TableAliasGenerator();
        protected readonly IUniqueParameterNameGenerator uniqueParameterNameGenerator = new UniqueParameterNameGenerator();
        internal readonly DataModificationQueryBuilder dataModificationQueryBuilder;
        protected readonly ObjectInitialisationOptions objectInitialisationOptions;

        // To help track deadlocks
        protected readonly List<string> commandTrace = new List<string>();

        public DateTime CreatedTime { get; } = DateTime.Now;
        
        public ReadRelationalTransaction(
            RelationalTransactionRegistry registry,
            RetriableOperation retriableOperation,
            IsolationLevel isolationLevel,
            ISqlCommandFactory sqlCommandFactory,
            JsonSerializerSettings jsonSerializerSettings,
            RelationalMappings mappings,
            IRelatedDocumentStore relatedDocumentStore,
            string name = null,
            ObjectInitialisationOptions objectInitialisationOptions = ObjectInitialisationOptions.None
        )
        {
            this.registry = registry;
            this.retriableOperation = retriableOperation;
            this.sqlCommandFactory = sqlCommandFactory;
            this.jsonSerializerSettings = jsonSerializerSettings;
            this.mappings = mappings;
            this.relatedDocumentStore = relatedDocumentStore;
            this.name = name ?? Thread.CurrentThread.Name;
            this.objectInitialisationOptions = objectInitialisationOptions;
            if (string.IsNullOrEmpty(name))
                this.name = "<unknown>";
            
            dataModificationQueryBuilder = new DataModificationQueryBuilder(mappings, jsonSerializerSettings);

            try
            {
                registry.Add(this);
                connection = new SqlConnection(registry.ConnectionString);
                connection.OpenWithRetry();
                transaction = connection.BeginTransaction(isolationLevel);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        [Pure]
        public T Load<T>(string id) where T : class, IId
        {
            // It's tempting to use TableQuery<T>().Where...... etc., but in this case we know exactly the query we need,
            // so it's a lot faster to go direct. This shaves about 7% off the load time.
            var mapping = mappings.Get(typeof(T));
            var tableName = mapping.TableName;
            var args = new CommandParameterValues {{"Id", id}};
            return ExecuteReader<T>($"SELECT TOP 1 * FROM dbo.[{tableName}] WHERE [Id] = @Id", mapping, args).FirstOrDefault();
        }

        [Pure]
        public IEnumerable<T> LoadStream<T>(IEnumerable<string> ids) where T : class, IId
        {
            var idList = ids.Where(id => !string.IsNullOrWhiteSpace(id)).Distinct().ToList();
            if (idList.Count == 0)
                return new List<T>();
            
            var mapping = mappings.Get(typeof(T));
            var tableName = mapping.TableName;
            
            var param = new CommandParameterValues();
            param.AddTable("criteriaTable", idList);
            return ExecuteReader<T>(
                $"SELECT s.* FROM dbo.[{tableName}] s INNER JOIN @criteriaTable t on t.[ParameterValue] = s.[Id] order by s.[Id]",
                param);
        }

        [Pure]
        public T[] Load<T>(IEnumerable<string> ids) where T : class, IId
            => LoadStream<T>(ids).ToArray();

        [Pure]
        public T LoadRequired<T>(string id) where T : class, IId
        {
            var result = Load<T>(id);
            if (result == null)
                throw new ResourceNotFoundException(id);
            return result;
        }

        [Pure]
        public T[] LoadRequired<T>(IEnumerable<string> ids) where T : class, IId
        {
            var allIds = ids.ToArray();
            var results = QueryBuilderWhereExtensions.Parameter<T>(TableQuery<T>()
                    .Where("[Id] IN @ids"), "ids", allIds)
                .Stream().ToArray();

            var items = allIds.Zip(results, Tuple.Create<string, T>);
            foreach (var pair in items)
                if (pair.Item2 == null)
                    throw new ResourceNotFoundException(pair.Item1);
            return results;
        }

        protected void AddCommandTrace(string commandText)
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
            using (new TimedSection(Log, ms => $"Executing non query took {ms}ms in transaction '{name}': {query}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args, commandTimeout: commandTimeout))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    return command.ExecuteNonQueryWithRetry(GetRetryPolicy(RetriableOperation.Select));
                }
                catch (SqlException ex)
                {
                    throw WrapException(command, ex);
                }
                catch (Exception ex)
                {
                    Log.DebugException($"Exception in relational transaction '{name}'", ex);
                    throw;
                }
            }
        }

        [Pure]
        public IEnumerable<T> ExecuteReader<T>(string query, CommandParameterValues args, TimeSpan? commandTimeout = null)
        {
            var mapping = mappings.Get(typeof(T));
            return ExecuteReader<T>(query, mapping, args, commandTimeout);
        }
        
        IEnumerable<T> ExecuteReader<T>(string query, DocumentMap mapping, CommandParameterValues args, TimeSpan? commandTimeout = null)
        {
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args, mapping, commandTimeout))
            {
                AddCommandTrace(command.CommandText);
                return Stream<T>(command, mapping);
            }
        }

        [Pure]
        public IEnumerable<T> ExecuteReaderWithProjection<T>(string query, CommandParameterValues args, Func<IProjectionMapper, T> projectionMapper, TimeSpan? commandTimeout = null)
        {
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args, commandTimeout: commandTimeout))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    return Stream(command, projectionMapper);
                }
                catch (SqlException ex)
                {
                    throw WrapException(command, ex);
                }
                catch (Exception ex)
                {
                    Log.DebugException($"Exception in relational transaction '{name}'", ex);
                    throw;
                }
            }
        }

        [Pure]
        IEnumerable<T> Stream<T>(IDbCommand command, DocumentMap mapping)
        {
            IDataReader reader = null;
            try
            {
                long msUntilFirstRecord = -1;
                using (var timedSection = new TimedSection(Log, ms => $"Reader took {ms}ms ({msUntilFirstRecord}ms until the first record) in transaction '{name}': {command.CommandText}", 300))
                {

                    try
                    {
                        reader = command.ExecuteReaderWithRetry();
                    }
                    catch (SqlException ex)
                    {
                        throw WrapException(command, ex);
                    }
                    catch (Exception ex)
                    {
                        Log.DebugException($"Exception in relational transaction '{name}'", ex);
                        throw;
                    }

                    msUntilFirstRecord = timedSection.ElapsedMilliseconds;
                    var idIndex = GetOrdinal(reader, "Id");
                    var jsonIndex = GetOrdinal(reader, "JSON");
                    var typeResolver = mapping.InstanceTypeResolver.TypeResolverFromReader(s => GetOrdinal(reader, s));

                    while (reader.Read())
                    {
                        T instance;
                        var instanceType = typeResolver(reader);

                        if (jsonIndex >= 0)
                        {
                            var json = reader[(int) jsonIndex].ToString();
                            var deserialized = JsonConvert.DeserializeObject(json, instanceType, jsonSerializerSettings);
                            // This is to handle polymorphic queries. e.g. Query<AzureAccount>()
                            // If the deserialized object is not the desired type, then we are querying for a specific sub-type
                            // and this record is a different sub-type, and should be excluded from the result-set.
                            if (deserialized is T)
                            {
                                instance = (T) deserialized;
                            }
                            else
                                continue;
                        }
                        else
                        {
                            instance = (T) Activator.CreateInstance(instanceType, objectInitialisationOptions.HasFlag(ObjectInitialisationOptions.UseNonPublicConstructors));
                        }

                        var specificMapping = mappings.Get(instance.GetType());
                        var columnIndexes = specificMapping.IndexedColumns.ToDictionary<ColumnMapping, ColumnMapping, int>(c => c, c => GetOrdinal(reader, c.ColumnName));

                        foreach (var index in columnIndexes)
                        {
                            if (index.Value >= 0)
                            {
                                index.Key.ReaderWriter.Write(instance, reader[index.Value]);
                            }
                        }

                        if (idIndex >= 0)
                        {
                            mapping.IdColumn.ReaderWriter.Write(instance, reader[(int) idIndex]);
                        }

                        yield return instance;
                    }
                }
            }
            finally
            {
                reader?.Dispose();
            }
        }

        protected static void DetectAndThrowIfKnownException(SqlException ex, DocumentMap mapping)
        {
            if (ex.Number == 2627 || ex.Number == 2601)
            {
                var uniqueRule = mapping.UniqueConstraints.FirstOrDefault(u => ex.Message.Contains(u.ConstraintName));
                if (uniqueRule != null)
                {
                    throw new UniqueConstraintViolationException(uniqueRule.Message);
                }
            }
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

        IEnumerable<T> Stream<T>(IDbCommand command, Func<IProjectionMapper, T> projectionMapper)
        {
            long msUntilFirstRecord = -1;

            using (var timedSection = new TimedSection(Log,
                ms =>
                    $"Reader took {ms}ms ({msUntilFirstRecord}ms until the first record) in transaction '{name}': {command.CommandText}",
                300))
            using (var reader = command.ExecuteReaderWithRetry())
            {
                msUntilFirstRecord = timedSection.ElapsedMilliseconds;

                var mapper = new ProjectionMapper(reader, jsonSerializerSettings, mappings);
                while (reader.Read())
                {
                    yield return projectionMapper(mapper);
                }
            }
        }

        [Pure]
        public ITableSourceQueryBuilder<T> TableQuery<T>() where T : class, IId
        {
            return new TableSourceQueryBuilder<T>(mappings.Get(typeof(T)).TableName, this, tableAliasGenerator, uniqueParameterNameGenerator, new CommandParameterValues(), new Parameters(), new ParameterDefaults());
        }

        [Pure]
        public ISubquerySourceBuilder<T> RawSqlQuery<T>(string query) where T : class, IId
        {
            return new SubquerySourceBuilder<T>(new RawSql(query), this, tableAliasGenerator, uniqueParameterNameGenerator, new CommandParameterValues(), new Parameters(), new ParameterDefaults());
        }

        [Pure]
        public T ExecuteScalar<T>(string query, CommandParameterValues args, TimeSpan? commandTimeout = null)
        {
            using (new TimedSection(Log, ms => $"Scalar took {ms}ms in transaction '{name}': {query}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args, commandTimeout: commandTimeout))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    var result = command.ExecuteScalarWithRetry(GetRetryPolicy(RetriableOperation.Select));
                    return (T) AmazingConverter.Convert(result, typeof(T));
                }
                catch (SqlException ex)
                {
                    throw WrapException(command, ex);
                }
                catch (Exception ex)
                {
                    Log.DebugException($"Exception in relational transaction '{name}'", ex);
                    throw;
                }
            }
        }

        public void ExecuteReader(string query, Action<IDataReader> readerCallback, TimeSpan? commandTimeout = null)
        {
            ExecuteReader(query, null, readerCallback, commandTimeout);
        }

        public void ExecuteReader(
            string query,
            object args,
            Action<IDataReader> readerCallback,
            TimeSpan? commandTimeout = null)
        {
            ExecuteReader(query, new CommandParameterValues(args), readerCallback, commandTimeout);
        }

        public void ExecuteReader(
            string query,
            CommandParameterValues args,
            Action<IDataReader> readerCallback,
            TimeSpan? commandTimeout = null)
        {
            using (new TimedSection(Log, ms => $"Executing reader took {ms}ms in transaction '{name}': {query}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args,
                commandTimeout: commandTimeout))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    using (var result = command.ExecuteReaderWithRetry(GetRetryPolicy(RetriableOperation.Select)))
                    {
                        readerCallback(result);
                    }
                }
                catch (SqlException ex)
                {
                    throw WrapException(command, ex);
                }
                catch (Exception ex)
                {
                    Log.DebugException($"Exception in relational transaction '{name}'", ex);
                    throw;
                }
            }
        }

        public void Dispose()
        {
            transaction?.Dispose();
            connection?.Dispose();
            registry.Remove(this);
        }

        protected RetryPolicy GetRetryPolicy(RetriableOperation operation)
        {
            if (retriableOperation == RetriableOperation.None)
                return RetryPolicy.NoRetry;

            return (retriableOperation & operation) != 0 ? RetryManager.Instance.GetDefaultSqlCommandRetryPolicy() : RetryPolicy.NoRetry;
        }

        protected Exception WrapException(IDbCommand command, Exception ex)
        {
            var sqlEx = ex as SqlException;
            if (sqlEx != null && sqlEx.Number == 1205) // deadlock
            {
                var builder = new StringBuilder();
                builder.AppendLine(ex.Message);
                builder.AppendLine("Current transactions: ");
                registry.WriteCurrentTransactions(builder);
                throw new Exception(builder.ToString());
            }

            Log.DebugException($"Error while executing SQL command in transaction '{name}'", ex);

            return new Exception($"Error while executing SQL command in transaction '{name}': {ex.Message}{Environment.NewLine}The command being executed was:{Environment.NewLine}{command.CommandText}", ex);
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

        class ProjectionMapper : IProjectionMapper
        {
            readonly IDataReader reader;
            readonly JsonSerializerSettings jsonSerializerSettings;
            readonly RelationalMappings mappings;

            public ProjectionMapper(IDataReader reader, JsonSerializerSettings jsonSerializerSettings, RelationalMappings mappings)
            {
                this.mappings = mappings;
                this.reader = reader;
                this.jsonSerializerSettings = jsonSerializerSettings;
            }

            public TResult Map<TResult>(string prefix)
            {
                var mapping = mappings.Get(typeof(TResult));
                var json = reader[GetColumnName(prefix, "JSON")].ToString();

                var instanceType = mapping.InstanceTypeResolver.TypeResolverFromReader((colName) => GetOrdinal(reader, GetColumnName(prefix, colName)))(reader);

                var instance = JsonConvert.DeserializeObject(json, instanceType, jsonSerializerSettings);
                foreach (var column in mappings.Get(instance.GetType()).IndexedColumns)
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
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Nevermore.Transient;
using System.Text;
using System.Threading;
using Nevermore.AST;
using Nevermore.Contracts;
using Nevermore.Diagnositcs;
using Nevermore.Diagnostics;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;

namespace Nevermore
{
    [DebuggerDisplay("{ToString()}")]
    public class RelationalTransaction : IRelationalTransaction
    {
        // Getting a typed ILog causes JIT compilation - we should only do this once
        static readonly ILog Log = LogProvider.For<RelationalTransaction>();
        static readonly ConcurrentDictionary<DocumentMap, string> InsertStatementTemplates = new ConcurrentDictionary<DocumentMap, string>();
        static readonly ConcurrentDictionary<DocumentMap, string> UpdateStatementTemplates = new ConcurrentDictionary<DocumentMap, string>();

        readonly RelationalTransactionRegistry registry;
        readonly RetriableOperation retriableOperation;
        readonly JsonSerializerSettings jsonSerializerSettings;
        readonly RelationalMappings mappings;
        readonly IKeyAllocator keyAllocator;
        readonly IDbConnection connection;
        readonly IDbTransaction transaction;
        readonly ISqlCommandFactory sqlCommandFactory;
        readonly IRelatedDocumentStore relatedDocumentStore;
        readonly string name;
        readonly ITableAliasGenerator tableAliasGenerator = new TableAliasGenerator();

        // To help track deadlocks
        readonly List<string> commandTrace = new List<string>();

        public DateTime CreatedTime { get; } = DateTime.Now;

        public RelationalTransaction(
            RelationalTransactionRegistry registry,
            RetriableOperation retriableOperation,
            IsolationLevel isolationLevel,
            ISqlCommandFactory sqlCommandFactory,
            JsonSerializerSettings jsonSerializerSettings,
            RelationalMappings mappings,
            IKeyAllocator keyAllocator,
            IRelatedDocumentStore relatedDocumentStore,
            string name = null
            )
        {
            this.registry = registry;
            this.retriableOperation = retriableOperation;
            this.sqlCommandFactory = sqlCommandFactory;
            this.jsonSerializerSettings = jsonSerializerSettings;
            this.mappings = mappings;
            this.keyAllocator = keyAllocator;
            this.relatedDocumentStore = relatedDocumentStore;
            this.name = name ?? Thread.CurrentThread.Name;
            if (string.IsNullOrEmpty(name))
                this.name = "<unknown>";

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

        public IDeleteQueryBuilder<TDocument> DeleteQuery<TDocument>() where TDocument : class, IId
        {
            return new DeleteQueryBuilder<TDocument>(this, mappings.Get(typeof(TDocument)).TableName, Enumerable.Empty<IWhereClause>(), new CommandParameterValues());
        }

        [Pure]
        public T Load<T>(string id) where T : class, IId
        {
            return TableQuery<T>()
                .Where("[Id] = @id")
                .Parameter("id", id)
                .First();
        }

        [Pure]
        public IEnumerable<T> LoadStream<T>(IEnumerable<string> ids) where T : class, IId
        {
            var blocks = ids
                .Distinct()
                .Select((id, index) => (id: id, index: index))
                .GroupBy(x => x.index / 500, y => y.id)
                .ToArray();

            foreach (var block in blocks)
            {
                var results = TableQuery<T>()
                    .Where("[Id] IN @ids")
                    .Parameter("ids", block.ToArray())
                    .Stream();

                foreach (var result in results)
                    yield return result;
            }
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
            var results = TableQuery<T>()
                .Where("[Id] IN @ids")
                .Parameter("ids", allIds)
                .Stream().ToArray();

            var items = allIds.Zip(results, Tuple.Create);
            foreach (var pair in items)
                if (pair.Item2 == null) throw new ResourceNotFoundException(pair.Item1);
            return results;
        }

        public void Insert<TDocument>(TDocument instance) where TDocument : class, IId
        {
            Insert(null, instance, null);
        }

        public void Insert<TDocument>(string tableName, TDocument instance) where TDocument : class, IId
        {
            Insert(tableName, instance, null);
        }

        public void Insert<TDocument>(TDocument instance, string customAssignedId) where TDocument : class, IId
        {
            Insert(null, instance, customAssignedId);
        }

        public void InsertWithHint<TDocument>(TDocument instance, string tableHint) where TDocument : class, IId
        {
            Insert(null, instance, null, tableHint);
        }

        public void Insert<TDocument>(string tableName, TDocument instance, string customAssignedId, string tableHint = null, int? commandTimeoutSeconds = null) where TDocument : class, IId
        {
            var mapping = mappings.Get(instance.GetType());
            var statement = InsertStatementTemplates.GetOrAdd(mapping, t => string.Format(
                "INSERT INTO dbo.[{0}] {1} ({2}) values ({3})",
                tableName ?? mapping.TableName,
                tableHint ?? "",
                string.Join(", ", mapping.IndexedColumns.Select(c => c.ColumnName).Union(new[] { "Id", "JSON" })),
                string.Join(", ", mapping.IndexedColumns.Select(c => "@" + c.ColumnName).Union(new[] { "@Id", "@JSON" }))
                ));

            var parameters = InstanceToParameters(instance, mapping);
            if (string.IsNullOrWhiteSpace(instance.Id))
            {
                parameters["Id"] = string.IsNullOrEmpty(customAssignedId) ? AllocateId(mapping) : customAssignedId;
            }
            else if (customAssignedId != null && customAssignedId != instance.Id)
            {
                throw new ArgumentException("Do not pass a different Id when one is already set on the document");
            }

            using (new TimedSection(Log, ms => $"Insert took {ms}ms in transaction '{name}': {statement}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, statement, parameters, mapping, commandTimeoutSeconds))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    command.ExecuteNonQueryWithRetry(GetRetryPolicy(RetriableOperation.Insert));

                    // Copy the assigned Id back onto the document
                    mapping.IdColumn.ReaderWriter.Write(instance, (string)parameters["Id"]);

                    relatedDocumentStore.PopulateRelatedDocuments(this, instance);
                }
                catch (SqlException ex)
                {
                    DetectAndThrowIfKnownException(ex, mapping);
                    throw WrapException(command, ex);
                }
                catch (Exception ex)
                {
                    Log.DebugException($"Exception in relational transaction '{name}'", ex);
                    throw;
                }
            }
        }

        public void InsertMany<TDocument>(string tableName, IReadOnlyCollection<TDocument> instances, bool includeDefaultModelColumns = true, string tableHint = null) where TDocument : class, IId
        {
            if (!instances.Any())
                return;

            var mapping = mappings.Get(instances.First().GetType()); // All instances share the same mapping.

            var parameters = new CommandParameterValues();
            var valueStatements = new List<string>();
            var instanceCount = 0;
            foreach (var instance in instances)
            {
                var instancePrefix = $"{instanceCount}__";
                var instanceParameters = InstanceToParameters(instance, mapping, instancePrefix);
                if (string.IsNullOrWhiteSpace(instance.Id))
                    instanceParameters[$"{instancePrefix}Id"] = AllocateId(mapping);

                parameters.AddRange(instanceParameters);

                var defaultIndexColumnPlaceholders = new string[] { };
                if (includeDefaultModelColumns)
                    defaultIndexColumnPlaceholders = new[] { $"@{instancePrefix}Id", $"@{instancePrefix}JSON" };

                valueStatements.Add($"({string.Join(", ", mapping.IndexedColumns.Select(c => $"@{instancePrefix}{c.ColumnName}").Union(defaultIndexColumnPlaceholders))})");

                instanceCount++;
            }

            var defaultIndexColumns = new string[] { };
            if (includeDefaultModelColumns)
                defaultIndexColumns = new[] { "Id", "JSON" };

            var statement = string.Format(
                "INSERT INTO dbo.[{0}] {1} ({2}) values {3}",
                tableName ?? mapping.TableName,
                tableHint ?? "",
                string.Join(", ", mapping.IndexedColumns.Select(c => c.ColumnName).Union(defaultIndexColumns)),
                string.Join(", ", valueStatements)
                );

            using (new TimedSection(Log, ms => $"Insert took {ms}ms in transaction '{name}': {statement}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, statement, parameters, mapping))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    command.ExecuteNonQueryWithRetry(GetRetryPolicy(RetriableOperation.Insert));
                    instanceCount = 0;
                    foreach (var instance in instances)
                    {
                        var instancePrefix = $"{instanceCount}__";

                        // Copy the assigned Id back onto the document
                        mapping.IdColumn.ReaderWriter.Write(instance, (string)parameters[$"{instancePrefix}Id"]);

                        relatedDocumentStore.PopulateRelatedDocuments(this, instance);
                        instanceCount++;
                    }
                }
                catch (SqlException ex)
                {
                    DetectAndThrowIfKnownException(ex, mapping);
                    throw WrapException(command, ex);
                }
                catch (Exception ex)
                {
                    Log.DebugException($"Exception in relational transaction '{name}'", ex);
                    throw;
                }
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

        public string AllocateId(Type documentType)
        {
            var mapping = mappings.Get(documentType);
            return AllocateId(mapping);
        }

        string AllocateId(DocumentMap mapping)
        {
            if (!string.IsNullOrEmpty(mapping.SingletonId))
                return mapping.SingletonId;

            return AllocateId(mapping.TableName, mapping.IdPrefix);
        }

        public string AllocateId(string tableName, string idPrefix)
        {
            var key = keyAllocator.NextId(tableName);
            return $"{idPrefix}-{key}";
        }

        public void Update<TDocument>(TDocument instance, string tableHint = null, int? commandTimeoutSeconds = null) where TDocument : class, IId
        {
            var mapping = mappings.Get(instance.GetType());

            var updates = string.Join(", ", mapping.IndexedColumns.Select(c => "[" + c.ColumnName + "] = @" + c.ColumnName).Union(new[] { "[JSON] = @JSON" }));
            var statement = UpdateStatementTemplates.GetOrAdd(mapping, t => string.Format(
                "UPDATE dbo.[{0}] {1} SET {2} WHERE Id = @Id",
                mapping.TableName,
                tableHint ?? "",
                updates));

            using (new TimedSection(Log, ms => $"Update took {ms}ms in transaction '{name}': {statement}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, statement, InstanceToParameters(instance, mapping), mapping, commandTimeoutSeconds))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    // Explicitly no retries on mutating database operations
                    command.ExecuteNonQueryWithRetry(GetRetryPolicy(RetriableOperation.Update));
                    relatedDocumentStore.PopulateRelatedDocuments(this, instance);
                }
                catch (SqlException ex)
                {
                    DetectAndThrowIfKnownException(ex, mapping);
                    throw WrapException(command, ex);
                }
                catch (Exception ex)
                {
                    Log.DebugException($"Exception in relational transaction '{name}'", ex);
                    throw;
                }
            }
        }

        // Delete does not require TDocument to implement IId because during recursive document delete we have only objects
        public void Delete<TDocument>(TDocument instance, int? commandTimeoutSeconds = null) where TDocument : class
        {
            var mapping = mappings.Get(instance.GetType());
            var id = (string)mapping.IdColumn.ReaderWriter.Read(instance);
            DeleteInternal(mapping, id, commandTimeoutSeconds);
        }

        public void DeleteById<TDocument>(string id, int? commandTimeoutSeconds = null) where TDocument : class
        {
            var mapping = mappings.Get(typeof(TDocument));
            DeleteInternal(mapping, id, commandTimeoutSeconds);
        }

        public void DeleteInternal(DocumentMap mapping, string id, int? commandTimeoutSeconds)
        {
            var statement = $"DELETE from dbo.[{mapping.TableName}] WHERE Id = @Id";

            using (new TimedSection(Log, ms => $"Delete took {ms}ms in transaction '{name}': {statement}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, statement, new CommandParameterValues { { "Id", id } }, mapping, commandTimeoutSeconds))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    // We can retry deletes because deleting something that doesn't exist will silently do nothing
                    command.ExecuteNonQueryWithRetry(GetRetryPolicy(RetriableOperation.Delete), "Delete " + mapping.TableName);
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

        public void ExecuteRawDeleteQuery(string query, CommandParameterValues args, int? commandTimeoutSeconds = null)
        {
            using (new TimedSection(Log, ms => $"Executing DELETE query took {ms}ms in transaction '{name}': {query}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args, commandTimeoutSeconds: commandTimeoutSeconds))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    // We can retry deletes because deleting something that doesn't exist will silently do nothing
                    command.ExecuteNonQueryWithRetry(GetRetryPolicy(RetriableOperation.Delete), "ExecuteDeleteQuery " + query);
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


        public int ExecuteNonQuery(string query, CommandParameterValues args, int? commandTimeoutSeconds = null)
        {
            using (new TimedSection(Log, ms => $"Executing non query took {ms}ms in transaction '{name}': {query}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args, commandTimeoutSeconds: commandTimeoutSeconds))
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
        public IEnumerable<T> ExecuteReader<T>(string query, CommandParameterValues args, int? commandTimeoutSeconds = null)
        {
            var mapping = mappings.Get(typeof(T));

            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args, mapping, commandTimeoutSeconds))
            {
                AddCommandTrace(command.CommandText);
                return Stream<T>(command, mapping);
            }
        }

        [Pure]
        public IEnumerable<T> ExecuteReaderWithProjection<T>(string query, CommandParameterValues args, Func<IProjectionMapper, T> projectionMapper, int? commandTimeoutSeconds = null)
        {
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args, commandTimeoutSeconds: commandTimeoutSeconds))
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
                            var json = reader[jsonIndex].ToString();
                            var deserialized = JsonConvert.DeserializeObject(json, instanceType, jsonSerializerSettings);
                            // This is to handle polymorphic queries. e.g. Query<AzureAccount>()
                            // If the deserialized object is not the desired type, then we are querying for a specific sub-type
                            // and this record is a different sub-type, and should be excluded from the result-set.
                            if (deserialized is T)
                            {
                                instance = (T)deserialized;
                            }
                            else
                                continue;
                        }
                        else
                        {
                            instance = (T)Activator.CreateInstance(instanceType);
                        }

                        var specificMapping = mappings.Get(instanceType);
                        var columnIndexes = specificMapping.IndexedColumns.ToDictionary(c => c, c => GetOrdinal(reader, c.ColumnName));

                        foreach (var index in columnIndexes)
                        {
                            if (index.Value >= 0)
                            {
                                index.Key.ReaderWriter.Write(instance, reader[index.Value]);
                            }
                        }

                        if (idIndex >= 0)
                        {
                            mapping.IdColumn.ReaderWriter.Write(instance, reader[idIndex]);
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

        static void DetectAndThrowIfKnownException(SqlException ex, DocumentMap mapping)
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
            using (var reader = command.ExecuteReaderWithRetry())
            {
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
            return new TableSourceQueryBuilder<T>(mappings.Get(typeof(T)).TableName, this, tableAliasGenerator, new CommandParameterValues(), new Parameters(), new ParameterDefaults());
        }

        CommandParameterValues InstanceToParameters(object instance, DocumentMap mapping, string prefix = null)
        {
            var result = new CommandParameterValues
            {
                [$"{prefix}Id"] = mapping.IdColumn.ReaderWriter.Read(instance)
            };

            var mType = mapping.InstanceTypeResolver.GetTypeFromInstance(instance);

            result[$"{prefix}JSON"] = JsonConvert.SerializeObject(instance, mType, jsonSerializerSettings);

            foreach (var c in mappings.Get(mType).IndexedColumns)
            {
                var value = c.ReaderWriter.Read(instance);
                if (value != null && value != DBNull.Value && value is string && c.MaxLength > 0)
                {
                    var attemptedLength = ((string)value).Length;
                    if (attemptedLength > c.MaxLength)
                    {
                        throw new StringTooLongException(string.Format("An attempt was made to store {0} characters in the {1}.{2} column, which only allows {3} characters.", attemptedLength, mapping.TableName, c.ColumnName, c.MaxLength));
                    }
                }
                else if (value != null && value != DBNull.Value && value is DateTime && value.Equals(DateTime.MinValue))
                {
                    value = SqlDateTime.MinValue.Value;
                }

                result[$"{prefix}{c.ColumnName}"] = value;
            }
            return result;
        }

        [Pure]
        public T ExecuteScalar<T>(string query, CommandParameterValues args, int? commandTimeoutSeconds = null)
        {
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args, commandTimeoutSeconds: commandTimeoutSeconds))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    var result = command.ExecuteScalarWithRetry(GetRetryPolicy(RetriableOperation.Select));
                    return (T)AmazingConverter.Convert(result, typeof(T));
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

        public void ExecuteReader(string query, Action<IDataReader> readerCallback)
        {
            ExecuteReader(query, null, readerCallback);
        }

        public void ExecuteReader(string query, object args, Action<IDataReader> readerCallback)
        {
            ExecuteReader(query, new CommandParameterValues(args), readerCallback);
        }

        public void ExecuteReader(string query, CommandParameterValues args, Action<IDataReader> readerCallback)
        {
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args))
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

        public void Commit()
        {
            transaction.Commit();
        }

        public void Dispose()
        {
            transaction?.Dispose();
            connection?.Dispose();
            registry.Remove(this);
        }

        RetryPolicy GetRetryPolicy(RetriableOperation operation)
        {
            if (retriableOperation == RetriableOperation.None)
                return RetryPolicy.NoRetry;

            return (retriableOperation & operation) != 0 ?
                RetryManager.Instance.GetDefaultSqlCommandRetryPolicy() :
                RetryPolicy.NoRetry;
        }

        Exception WrapException(IDbCommand command, Exception ex)
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
                foreach (var column in mappings.Get(instanceType).IndexedColumns)
                {
                    column.ReaderWriter.Write(instance, reader[GetColumnName(prefix, column.ColumnName)]);
                }

                mapping.IdColumn.ReaderWriter.Write(instance, reader[GetColumnName(prefix, mapping.IdColumn.ColumnName)]);

                return (TResult)instance;
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
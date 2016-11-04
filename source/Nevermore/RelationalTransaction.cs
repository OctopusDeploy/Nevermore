using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Linq;
using Newtonsoft.Json;
using Nevermore;
using Nevermore.Transient;
using System.Text;
using Nevermore.Contracts;
using Nevermore.Diagnositcs;
using Nevermore.Diagnostics;
using Nevermore.Mapping;
using Nevermore.RelatedDocuments;

namespace Nevermore
{
    public class RelationalTransaction : IRelationalTransaction
    {
        
        static readonly ConcurrentDictionary<string, string> InsertStatementTemplates = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        static readonly ConcurrentDictionary<string, string> UpdateStatementTemplates = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        readonly RetriableOperation retriableOperation;
        readonly JsonSerializerSettings jsonSerializerSettings;
        readonly RelationalMappings mappings;
        readonly IKeyAllocator keyAllocator;
        readonly IDbConnection connection;
        readonly IDbTransaction transaction;
        readonly ISqlCommandFactory sqlCommandFactory;
        readonly IRelatedDocumentStore relatedDocumentStore;
        readonly ILog log = LogProvider.For<RelationalTransaction>();

        // To help track deadlocks
        readonly List<string> commandTrace = new List<string>();

        static readonly Collection<RelationalTransaction> CurrentTransactions = new Collection<RelationalTransaction>();
        readonly DateTime createdTime = DateTime.Now;

        public RelationalTransaction(
            string connectionString,
            RetriableOperation retriableOperation,
            IsolationLevel isolationLevel,
            ISqlCommandFactory sqlCommandFactory,
            JsonSerializerSettings jsonSerializerSettings,
            RelationalMappings mappings,
            IKeyAllocator keyAllocator,
            IRelatedDocumentStore relatedDocumentStore)
        {
            this.retriableOperation = retriableOperation;
            this.sqlCommandFactory = sqlCommandFactory;
            this.jsonSerializerSettings = jsonSerializerSettings;
            this.mappings = mappings;
            this.keyAllocator = keyAllocator;
            this.relatedDocumentStore = relatedDocumentStore;

            lock (CurrentTransactions)
            {
                CurrentTransactions.Add(this);
                if (CurrentTransactions.Count == 90)
                    LogHighNumberOfTransactions();
            }

            connection = new SqlConnection(connectionString);
            connection.OpenWithRetry();
            transaction = connection.BeginTransaction(isolationLevel);
        }
        
        public T Load<T>(string id) where T : class, IId
        {
            return Query<T>()
                .Where("[Id] = @id")
                .Parameter("id", id)
                .First();
        }

        public T[] Load<T>(IEnumerable<string> ids) where T : class, IId
        {
            return Query<T>()
                .Where("[Id] IN @ids")
                .Parameter("ids", ids.ToArray())
                .Stream().ToArray();
        }

        public T LoadRequired<T>(string id) where T : class, IId
        {
            var result = Load<T>(id);
            if (result == null)
                throw new ResourceNotFoundException(id);
            return result;
        }

        public T[] LoadRequired<T>(IEnumerable<string> ids) where T : class, IId
        {
            var allIds = ids.ToArray();
            var results = Query<T>()
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

        public void Insert<TDocument>(string tableName, TDocument instance, string customAssignedId, string tableHint = null) where TDocument : class, IId
        {
            var mapping = mappings.Get(instance.GetType());
            var statement = InsertStatementTemplates.GetOrAdd(mapping.TableName, t => string.Format(
                "INSERT INTO dbo.[{0}] {1} ({2}) values ({3})",
                tableName ?? mapping.TableName,
                tableHint ?? "",
                string.Join(", ", mapping.IndexedColumns.Select(c => c.ColumnName).Union(new[] { "Id", "Json" })),
                string.Join(", ", mapping.IndexedColumns.Select(c => "@" + c.ColumnName).Union(new[] { "@Id", "@Json" }))
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

            using (new TimedSection(log, ms => $"Insert took {ms}ms: {statement}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, statement, parameters, mapping))
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
            }
        }

        public void InsertMany<TDocument>(string tableName, IReadOnlyCollection<TDocument> instances, bool includeDefaultModelColumns = true, string tableHint = null) where TDocument : class, IId
        {
            if (!instances.Any())
                return;

            var mapping = mappings.Get(instances.First().GetType()); // All instances share the same mapping.

            var parameters = new CommandParameters();
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
                    defaultIndexColumnPlaceholders = new[] { $"@{instancePrefix}Id", $"@{instancePrefix}Json" };

                valueStatements.Add($"({string.Join(", ", mapping.IndexedColumns.Select(c => $"@{instancePrefix}{c.ColumnName}").Union(defaultIndexColumnPlaceholders))})");

                instanceCount++;
            }
            
            var defaultIndexColumns = new string[] { };
            if (includeDefaultModelColumns)
                defaultIndexColumns = new[] { "Id", "Json" };

            var statement = string.Format(
                "INSERT INTO dbo.[{0}] {1} ({2}) values {3}",
                tableName ?? mapping.TableName,
                tableHint ?? "",
                string.Join(", ", mapping.IndexedColumns.Select(c => c.ColumnName).Union(defaultIndexColumns)),
                string.Join(", ", valueStatements)
                );

            using (new TimedSection(log, ms => $"Insert took {ms}ms: {statement}", 300))
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
            }
        }

        void AddCommandTrace(string commandText)
        {
            lock (CurrentTransactions)
            {
                if (commandTrace.Count == 100)
                    log.DebugFormat("A possible N+1 or long running transaction detected, this is a diagnostic message only does not require end-user action.\r\nStarted: {0:s}\r\nStack: {1}\r\n\r\n{2}", createdTime, new StackTrace(), string.Join("\r\n", commandTrace));

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

            var key = keyAllocator.NextId(mapping.TableName);
            return $"{mapping.IdPrefix}-{key}";
        }

        public void Update<TDocument>(TDocument instance, string tableHint = null) where TDocument : class, IId
        {
            var mapping = mappings.Get(instance.GetType());

            var updates = string.Join(", ", mapping.IndexedColumns.Select(c => "[" + c.ColumnName + "] = @" + c.ColumnName).Union(new []{ "[Json] = @Json" }));
            var statement = UpdateStatementTemplates.GetOrAdd(mapping.TableName, t => string.Format(
                "UPDATE dbo.[{0}] {1} SET {2} WHERE Id = @Id",
                mapping.TableName,
                tableHint ?? "",
                updates));

            using (new TimedSection(log, ms => $"Update took {ms}ms: {statement}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, statement, InstanceToParameters(instance, mapping), mapping))
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
            }
        }

        // Delete does not require TDocument to implement IId because during recursive document delete we have only objects
        public void Delete<TDocument>(TDocument instance) where TDocument : class
        {
            var mapping = mappings.Get(instance.GetType());
            var id = (string)mapping.IdColumn.ReaderWriter.Read(instance);

            var statement = string.Format("DELETE from dbo.[{0}] WHERE Id = @Id", mapping.TableName);

            using (new TimedSection(log, ms => $"Delete took {ms}ms: {statement}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, statement, new CommandParameters {{"Id", id}}, mapping))
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
            }
        }

        public void ExecuteRawDeleteQuery(string query, CommandParameters args)
        {
            using (new TimedSection(log, ms => $"Executing DELETE query took {ms}ms: {query}", 300))
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args))
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
            }
        }

        public IEnumerable<T> ExecuteReader<T>(string query, CommandParameters args)
        {
            var mapping = mappings.Get(typeof(T));

            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args, mapping))
            {
                AddCommandTrace(command.CommandText);
                try
                {
                    return Stream<T>(command, mapping);
                }
                catch (SqlException ex)
                {
                    throw WrapException(command, ex);
                }
            }
        }

        public IEnumerable<T> ExecuteReaderWithProjection<T>(string query, CommandParameters args, Func<IProjectionMapper, T> projectionMapper)
        {
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args))
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
            }
        }

        IEnumerable<T> Stream<T>(IDbCommand command, DocumentMap mapping)
        {
            long msUntilFirstRecord = -1;
            using (var timedSection = new TimedSection(log, ms => $"Reader took {ms}ms ({msUntilFirstRecord}ms until the first record): {command.CommandText}", 300))
            using (var reader = command.ExecuteReaderWithRetry())
            {
                msUntilFirstRecord = timedSection.ElapsedMilliseconds;
                var idIndex = GetOrdinal(reader, "Id");
                var jsonIndex = GetOrdinal(reader, "Json");
                var columnIndexes = mapping.IndexedColumns.ToDictionary(c => c, c => GetOrdinal(reader, c.ColumnName));

                while (reader.Read())
                {
                    T instance;

                    if (jsonIndex >= 0)
                    {
                        var json = reader[jsonIndex].ToString();

                        var deserialized = JsonConvert.DeserializeObject(json, mapping.Type, jsonSerializerSettings);

                        // This is to handle polymorphic queries. e.g. Query<AzureAccount>()
                        // If the deserialized object is not the desired type, then we are querying for a specific sub-type
                        // and this record is a different sub-type, and should be excluded from the result-set. 
                        if (deserialized is T)
                            instance = (T)deserialized;
                        else
                            continue;
                    }
                    else
                    {
                        instance = Activator.CreateInstance<T>();
                    }

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
                if (dr.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
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

        public IQueryBuilder<T> Query<T>() where T : class, IId
        {
            return new QueryBuilder<T>(this, mappings.Get(typeof(T)).TableName);
        }

        CommandParameters InstanceToParameters(object instance, DocumentMap mapping, string prefix = null)
        {
            var result = new CommandParameters();
            result[$"{prefix}Id"] = mapping.IdColumn.ReaderWriter.Read(instance);
            result[$"{prefix}Json"] = JsonConvert.SerializeObject(instance, mapping.Type, jsonSerializerSettings);

            foreach (var c in mapping.IndexedColumns)
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

        public T ExecuteScalar<T>(string query, CommandParameters args)
        {
            using (var command = sqlCommandFactory.CreateCommand(connection, transaction, query, args))
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
            }
        }

        public void ExecuteReader(string query, Action<IDataReader> readerCallback)
        {
            ExecuteReader(query, null, readerCallback);
        }

        public void ExecuteReader(string query, object args, Action<IDataReader> readerCallback)
        {
            ExecuteReader(query, new CommandParameters(args), readerCallback);
        }

        public void ExecuteReader(string query, CommandParameters args, Action<IDataReader> readerCallback)
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
            }
        }

        public void Commit()
        {
            transaction.Commit();
        }

        public void Dispose()
        {
            transaction.Dispose();
            connection.Dispose();

            lock (CurrentTransactions)
                CurrentTransactions.Remove(this);
        }

        RetryPolicy GetRetryPolicy(RetriableOperation operation)
        {
            if (retriableOperation == RetriableOperation.None)
                return RetryPolicy.NoRetry;

            return (retriableOperation & operation) != 0 ?
                RetryManager.Instance.GetDefaultSqlCommandRetryPolicy() :
                RetryPolicy.NoRetry;
        }

        static Exception WrapException(IDbCommand command, Exception ex)
        {
            var sqlEx = ex as SqlException;
            if (sqlEx != null && sqlEx.Number == 1205) // deadlock
            {
                var builder = new StringBuilder();
                builder.AppendLine(ex.Message);
                builder.AppendLine("Current transactions: ");
                WriteCurrentTransactions(builder);
                throw new Exception(builder.ToString());
            }

            return new Exception("Error while executing SQL command: " + ex.Message + Environment.NewLine + "The command being executed was:" + Environment.NewLine + command.CommandText, ex);
        }

        void LogHighNumberOfTransactions()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("There are a high number of transactions active. The below information may help the Octopus team diagnose the problem:");
            sb.AppendLine($"Now: {DateTime.Now:s}");
            WriteCurrentTransactions(sb);
            log.Debug(sb.ToString());
        }

        static void WriteCurrentTransactions(StringBuilder sb)
        {
            lock (CurrentTransactions)
                foreach (var trn in CurrentTransactions.OrderBy(t => t.createdTime))
                {
                    sb.AppendLine();
                    sb.AppendLine($"Transaction with {trn.commandTrace.Count} commands started at {trn.createdTime:s} ({(DateTime.Now - trn.createdTime).TotalSeconds:n2} seconds ago)");
                    foreach (var command in trn.commandTrace)
                        sb.AppendLine(command);
                }
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

                var instance = JsonConvert.DeserializeObject<TResult>(json, jsonSerializerSettings);
                foreach (var column in mapping.IndexedColumns)
                {
                    column.ReaderWriter.Write(instance, reader[GetColumnName(prefix, column.ColumnName)]);
                }

                mapping.IdColumn.ReaderWriter.Write(instance, reader[GetColumnName(prefix, mapping.IdColumn.ColumnName)]);

                return instance;
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
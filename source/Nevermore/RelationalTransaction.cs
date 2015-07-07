using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using Newtonsoft.Json;

namespace Nevermore
{
    public class RelationalTransaction : IRelationalTransaction
    {
        static readonly ConcurrentDictionary<string, string> InsertStatementTemplates = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        static readonly ConcurrentDictionary<string, string> UpdateStatementTemplates = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        readonly JsonSerializerSettings jsonSerializerSettings;
        readonly RelationalMappings mappings;
        readonly IKeyAllocator keyAllocator;
        readonly SqlConnection connection;
        readonly SqlTransaction transaction;

        public RelationalTransaction(string connectionString, IsolationLevel isolationLevel, JsonSerializerSettings jsonSerializerSettings, RelationalMappings mappings, IKeyAllocator keyAllocator)
        {
            this.jsonSerializerSettings = jsonSerializerSettings;
            this.mappings = mappings;
            this.keyAllocator = keyAllocator;

            connection = new SqlConnection(connectionString);
            connection.Open();
            transaction = connection.BeginTransaction(isolationLevel);
        }

        public T Load<T>(string id) where T : class
        {
            return Query<T>()
                .Where("Id = @id")
                .Parameter("id", id)
                .First();
        }

        public T[] Load<T>(IEnumerable<string> ids) where T : class
        {
            return Query<T>()
                .Where("Id in @ids")
                .Parameter("ids", ids.ToArray())
                .Stream().ToArray();
        }

        public T LoadRequired<T>(string id) where T : class
        {
            var result = Load<T>(id);
            if (result == null)
                throw new ResourceNotFoundException(id);
            return result;
        }

        public T[] LoadRequired<T>(IEnumerable<string> ids) where T : class
        {
            var allIds = ids.ToArray();
            var results = Query<T>()
                .Where("Id in @ids")
                .Parameter("ids", allIds)
                .Stream().ToArray();

            var items = allIds.Zip<string, T, Tuple<string, T>>(results, Tuple.Create);
            foreach (var pair in items)
                if (pair.Item2 == null) throw new ResourceNotFoundException(pair.Item1);
            return results;
        }

        public void Insert<TDocument>(TDocument instance) where TDocument : class
        {
            Insert(null, instance, null);
        }

        public void Insert<TDocument>(string tableName, TDocument instance) where TDocument : class
        {
            Insert(tableName, instance, null);
        }

        public void Insert<TDocument>(TDocument instance, string customAssignedId) where TDocument : class
        {
            Insert(null, instance, customAssignedId);
        }

        public void Insert<TDocument>(string tableName, TDocument instance, string customAssignedId) where TDocument : class
        {
            var mapping = mappings.Get(instance.GetType());
            var statement = InsertStatementTemplates.GetOrAdd(mapping.TableName, t => string.Format(
                "INSERT INTO dbo.[{0}] ({1}) values ({2})",
                tableName ?? mapping.TableName,
                string.Join(", ", mapping.IndexedColumns.Select(c => c.ColumnName).Concat(new[] {"Id", "Json"})),
                string.Join(", ", mapping.IndexedColumns.Select(c => "@" + c.ColumnName).Concat(new[] {"@Id", "@Json"}))
                ));

            var parameters = InstanceToParameters(instance, mapping);
            if (parameters["Id"] == null || string.IsNullOrWhiteSpace((string)parameters["Id"]))
            {
                var id = string.IsNullOrEmpty(customAssignedId) ? AllocateId(mapping) : customAssignedId;
                parameters["Id"] = id;
            }

            using (var command = CreateCommand(statement, parameters))
            {
                try
                {
                    command.ExecuteNonQuery();
                    mapping.IdColumn.ReaderWriter.Write(instance, parameters["Id"]);
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 2627 || ex.Number == 2601)
                    {
                        var uniqueRule = mapping.UniqueConstraints.FirstOrDefault(u => ex.Message.Contains(u.ConstraintName));
                        if (uniqueRule != null)
                        {
                            throw new UniqueConstraintViolationException(uniqueRule.Message);
                        }
                    }

                    throw WrapException(command, ex);
                }
            }
        }

        public string AllocateId(Type documentType)
        {
            var mapping = mappings.Get(documentType);
            return AllocateId(mapping);
        }

        string AllocateId(DocumentMap mapping)
        {
            var key = keyAllocator.NextId(mapping.TableName);
            var generatedId = mapping.IdPrefix + "-" + key;
            return generatedId;
        }

        public void Update<TDocument>(TDocument instance) where TDocument : class
        {
            var mapping = mappings.Get(instance.GetType());

            var statement = UpdateStatementTemplates.GetOrAdd(mapping.TableName, t => string.Format(
                "UPDATE dbo.[{0}] SET {1} Json = @Json WHERE Id = @Id",
                mapping.TableName,
                mapping.IndexedColumns.Count > 0 ? string.Join(", ", mapping.IndexedColumns.Select(c => "[" + c.ColumnName + "] = @" + c.ColumnName)) + ", " : ""));

            using (var command = CreateCommand(statement, InstanceToParameters(instance, mapping)))
            {
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    throw WrapException(command, ex);
                }
            }
        }

        public void Delete<TDocument>(TDocument instance) where TDocument : class
        {
            var mapping = mappings.Get(instance.GetType());

            var statement = string.Format("DELETE from dbo.[{0}] WHERE Id = @Id", mapping.TableName);

            using (var command = CreateCommand(statement, new CommandParameters {{"Id", mapping.IdColumn.ReaderWriter.Read(instance)}}))
            {
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    throw WrapException(command, ex);
                }
            }
        }

        public IEnumerable<T> ExecuteReader<T>(string query, CommandParameters args)
        {
            var mapping = mappings.Get(typeof (T));

            using (var command = CreateCommand(query, args))
            {
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
            using (var command = CreateCommand(query, args))
            {
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

        IEnumerable<T> Stream<T>(SqlCommand command, DocumentMap mapping)
        {
            using (var reader = command.ExecuteReader())
            {
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

        static int GetOrdinal(SqlDataReader dr, string columnName)
        {
            for (var i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals(columnName, StringComparison.InvariantCultureIgnoreCase))
                    return i;
            }
            return -1;
        }

        IEnumerable<T> Stream<T>(SqlCommand command, Func<IProjectionMapper, T> projectionMapper)
        {
            using (var reader = command.ExecuteReader())
            {
                var mapper = new ProjectionMapper(reader, jsonSerializerSettings, mappings);
                while (reader.Read())
                {
                    yield return projectionMapper(mapper);
                }
            }
        }

        public IQueryBuilder<T> Query<T>() where T : class
        {
            return new QueryBuilder<T>(this, mappings.Get(typeof (T)).TableName);
        }

        CommandParameters InstanceToParameters(object instance, DocumentMap mapping)
        {
            var result = new CommandParameters();
            result["Id"] = mapping.IdColumn.ReaderWriter.Read(instance);
            result["Json"] = JsonConvert.SerializeObject(instance, mapping.Type, jsonSerializerSettings);

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

                result[c.ColumnName] = value;
            }
            return result;
        }

        public T ExecuteScalar<T>(string query, CommandParameters args)
        {
            using (var command = CreateCommand(query, args))
            {
                try
                {
                    var result = command.ExecuteScalar();
                    return (T)AmazingConverter.Convert(result, typeof (T));
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
            using (var command = CreateCommand(query, args))
            {
                try
                {
                    using (var result = command.ExecuteReader())
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

        SqlCommand CreateCommand(string statement, CommandParameters args)
        {
            var command = new SqlCommand(statement, connection, transaction);
            if (args != null)
            {
                args.ContributeTo(command);
            }
            return command;
        }

        public void Commit()
        {
            transaction.Commit();
        }

        public void Dispose()
        {
            transaction.Dispose();
            connection.Dispose();
        }

        static Exception WrapException(SqlCommand command, Exception ex)
        {
            return new Exception("Error while executing SQL command: " + ex.Message + Environment.NewLine + "The command being executed was:" + Environment.NewLine + command.CommandText, ex);
        }

        class ProjectionMapper : IProjectionMapper
        {
            readonly SqlDataReader reader;
            readonly JsonSerializerSettings jsonSerializerSettings;
            readonly RelationalMappings mappings;

            public ProjectionMapper(SqlDataReader reader, JsonSerializerSettings jsonSerializerSettings, RelationalMappings mappings)
            {
                this.mappings = mappings;
                this.reader = reader;
                this.jsonSerializerSettings = jsonSerializerSettings;
            }

            public TResult Map<TResult>(string prefix)
            {
                var mapping = mappings.Get(typeof (TResult));
                var json = reader[GetColumnName(prefix, "JSON")].ToString();

                var instance = JsonConvert.DeserializeObject<TResult>(json, jsonSerializerSettings);
                foreach (var column in mapping.IndexedColumns)
                {
                    column.ReaderWriter.Write(instance, reader[GetColumnName(prefix, column.ColumnName)]);
                }

                mapping.IdColumn.ReaderWriter.Write(instance, reader[GetColumnName(prefix, mapping.IdColumn.ColumnName)]);

                return instance;
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
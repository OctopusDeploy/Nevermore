using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Nevermore.Advanced.TypeHandlers;
using Nevermore.Mapping;

namespace Nevermore.Advanced.ReaderStrategies
{
    /// <summary>
    /// The Document reader strategy is how we map query results from documents in Nevermore. It's probably the most
    /// commonly used strategy, and it's called when you use Load, Stream, Query, and so on. 
    /// </summary>
    public class DocumentReaderStrategy : IReaderStrategy
    {
        readonly RelationalStoreConfiguration configuration;
        
        public DocumentReaderStrategy(RelationalStoreConfiguration configuration)
        {
            this.configuration = configuration;
        }
        
        public bool CanRead(Type type)
        {
            return configuration.Mappings.ResolveOptional(type, out _);
        }

        public Func<PreparedCommand, Func<DbDataReader, (TRecord, bool)>> CreateReader<TRecord>()
        {
            // This is executed once per type that we query, and it's where we cache the different columns that we may
            // to map someday (though we don't know what order they will appear in the result set yet.)
            var type = typeof(TRecord);
            var typePlan = TypePlan.Create(type, configuration);

            return command =>
            {
                // This is called at the start of a query. 
                var rowCount = 0;
                
                ReaderPlan plan = null;
                
                return reader =>
                {
                    // This is called for each row in the reader. For the first row, we need to create and cache our 
                    // ReaderPlan, since it's the first time we know what order the columns will appear. We cache it per
                    // SQL query (SELECT * | SELECT Col2, Col1...) since each query may return different columns in
                    // different orders.
                    rowCount++;

                    if (rowCount == 1)
                    {
                        plan = typePlan.CreateReaderPlan(reader, command);
                    }

                    return ReadDocument<TRecord>(reader, plan);
                };
            };
        }

        (TDocument, bool) ReadDocument<TDocument>(DbDataReader reader, ReaderPlan plan)
        {
            var instanceType = typeof(TDocument);
            object deserialized = null;
            
            // We use CommandBehavior.SequentialAccess when reading from our data reader, so we must read all fields
            // in the order they come in. But until we've read the JSON column, we can't start assigning values. So 
            // instead, we buffer all values we read until we've read the JSON, then apply the bound values afterwards. 
            var bufferedValues = new object[plan.ExpectedFieldCount];
            for (var i = 0; i < plan.ExpectedFieldCount; i++)
            {
                if (i == plan.TypeIndex)
                {
                    // The 'Type' column tells us what type to deserialize. It may or may not be mapped. If it's mapped (perhaps 
                    // to an enum or other type) we use the mapping to read it. 
                    object typeColumnValue;
                    if (plan.BoundExpectedColumns[i] != null)
                    {
                        typeColumnValue = plan.BoundExpectedColumns[i].Read(reader);
                        bufferedValues[i] = typeColumnValue;
                    }
                    else
                    {
                        // If the type column is not mapped, we assume it is a string.
                        typeColumnValue = reader.GetString(i);
                    }

                    if (typeColumnValue != null)
                    {
                        var resolvedInstanceType = ResolveInstanceType(instanceType, typeColumnValue);
                        if (resolvedInstanceType == null)
                            throw new InvalidOperationException($"The type column has a value of '{typeColumnValue}' ({typeColumnValue.GetType().Name}), but no type resolver was able to map it to a concrete type to deserialize. Either register an instance type resolver that knows how to interpret the value, or consider fixing the data.");
                        
                        if (resolvedInstanceType == typeof(void))
                        {
                            // Mappers can use typeof(void) if they want to explicitly hide the result. A use case for this 
                            // might be when we know there's a type, but the assembly isn't loaded (like in an extension).
                            return (default, false);
                        }

                        instanceType = resolvedInstanceType;
                    }
                }
                else if (i == plan.JsonIndex && !reader.IsDBNull(i))
                {
                    // The JSON column is typically the largest column on a document, and contains one big string.
                    // We have two approaches for reading the JSON. We start by simply reading it to a string with
                    // GetString(). This is typically faster for small objects, and the values are GC'd quickly. 
                    // Alternatively, we can also stream the data, which saves memory and prevents large strings going
                    // to the LOH. But, that's slower for small documents. 
                    // We initially assume that most documents are small, but if in our plan we encounter a large
                    // document, we remember that and use streaming next time. This will apply from row 2 onwards for
                    // the first time the query is run, and for every row the next time the query runs, since we cache
                    // our reader plans. 
                    // As a worst case, we'll send the first large document we encounter per SQL query to the LOH, then
                    // stream the rest.
                    // The 1K cutoff was determined by querying generating different sizes of document, then using
                    // different cutoff values, and comparing the results to balance speed and memory allocations. 
                    // In Octofront, roughly 75% of all documents fall into the <1K category. 
                    // In a large Octopus database, roughly 80% of all documents fall into the <1K category. 
                    // Documents that are larger than 1K tend to be clustered in specific tables. 
                    if (!plan.TypePlan.ExpectsLargeDocuments)
                    {
                        // For small documents, we'll just read it as a string and deserialize. This tends to be faster
                        var text = reader.GetString(i);
                        if (text.Length >= NevermoreDefaults.LargeDocumentCutoffChars) 
                            // We'll know for next time
                            plan.TypePlan.ExpectsLargeDocuments = true;

                        if (deserialized == null || plan.TypePlan.JsonStorageFormat == JsonStorageFormat.MixedPreferText)
                            deserialized = configuration.Serializer.DeserializeSmallText(text, instanceType);
                    }
                    else
                    {
                        // Large documents will be streamed. Since it's a text column, we have to call GetChars.
                        // DataReaderTextStream exposes that character stream as a stream that our serializer can read
                        // (as serializers typically prefer byte[] streams)
                        using var dataStream = new DataTextReader(reader, plan.JsonIndex);
                        if (deserialized == null || plan.TypePlan.JsonStorageFormat == JsonStorageFormat.MixedPreferText)
                            deserialized = configuration.Serializer.DeserializeLargeText(dataStream, instanceType);
                    }
                }
                else if (i == plan.JsonBlobIndex && !reader.IsDBNull(i))
                {
                    using var stream = reader.GetStream(i);
                    if (deserialized == null || plan.TypePlan.JsonStorageFormat == JsonStorageFormat.MixedPreferCompressed)
                        deserialized = configuration.Serializer.DeserializeCompressed(stream, instanceType);
                }
                else if (plan.BoundExpectedColumns[i] != null)
                {
                    // We may not have deserialized the type yet, but because we use sequential access to the data
                    // reader, we need to read the value anyway. So store and buffer it.
                    bufferedValues[i] = plan.BoundExpectedColumns[i].Read(reader);
                }
            }

            if (deserialized == null)
            {
                if (plan.JsonBlobIndex >= 0 && plan.JsonIndex >= 0)
                    throw new InvalidOperationException("The result set contained both a [JSON] and [JSONBlob] column, but on this row both were null, so the document could not be deserialized.");
                if (plan.JsonBlobIndex >= 0)
                    throw new InvalidOperationException("The result set contained a [JSONBlob] column (no [JSON] column), but on this row the value was null, so the document could not be deserialized.");
                if (plan.JsonIndex >= 0)
                    throw new InvalidOperationException("The result set contained a [JSON] column (no [JSONBlob] column), but on this row the value was null, so the document could not be deserialized.");
            }
            
            if (!(deserialized is TDocument document))
                // This is to handle polymorphic queries. e.g. Query<AzureAccount>()
                // If the deserialized object is not the desired type, then we are querying for a specific sub-type
                // and this record is a different sub-type, and should be excluded from the result-set.
                return (default, false);

            for (var i = 0; i < plan.ExpectedFieldCount; i++)
            {
                if (plan.BoundExpectedColumns[i] != null)
                    plan.BoundExpectedColumns[i].WriteTo(document, bufferedValues[i]);
            }

            return (document, true);
        }

        Type ResolveInstanceType(Type baseType, object columnValue)
        {
            if (columnValue == null || columnValue == DBNull.Value) 
                return null;
            
            return configuration.InstanceTypeRegistry.Resolve(baseType, columnValue);
        }
        
        // We create a TypePlan for every document type. It acts as a cache for the things that don't change when doing
        // any kind of query against this type.
        class TypePlan
        {
            readonly ConcurrentDictionary<int, ReaderPlan> readerPlans = new ConcurrentDictionary<int, ReaderPlan>();

            public readonly Type Type;
            public readonly DocumentMap Mapping;
            public readonly ConcurrentDictionary<string, TypeColumn> Columns;
            public readonly JsonStorageFormat JsonStorageFormat;
            public readonly bool ExpectsJsonText;
            public readonly bool ExpectsJsonCompressed;

            public bool ExpectsLargeDocuments
            {
                get => Mapping.ExpectLargeDocuments;
                set => Mapping.ExpectLargeDocuments = true;
            }

            TypePlan(Type type, DocumentMap mapping, ConcurrentDictionary<string, TypeColumn> columns)
            {
                Type = type;
                Mapping = mapping;
                Columns = columns;
                JsonStorageFormat = mapping.JsonStorageFormat;
                ExpectsJsonText = JsonStorageFormat != JsonStorageFormat.CompressedOnly;
                ExpectsJsonCompressed = JsonStorageFormat != JsonStorageFormat.TextOnly;
            }

            public static TypePlan Create(Type type, IRelationalStoreConfiguration configuration)
            {
                var handlerRegistry = configuration.TypeHandlerRegistry;
                var mapping = configuration.Mappings.Resolve(type);
                var columns = new ConcurrentDictionary<string, TypeColumn>(StringComparer.OrdinalIgnoreCase);
                columns.TryAdd(mapping.IdColumn.ColumnName, new TypeColumn(mapping.IdColumn, CreateDataReaderFunc(mapping.IdColumn, handlerRegistry)));
                foreach (var knownColumn in mapping.IndexedColumns.Select(column => new TypeColumn(column, CreateDataReaderFunc(column, handlerRegistry))))
                {
                    columns.TryAdd(knownColumn.Name, knownColumn);
                }
                return new TypePlan(type, mapping, columns);
            }

            static Func<DbDataReader, int, object> CreateDataReaderFunc(ColumnMapping columnMapping, ITypeHandlerRegistry handlerRegistry)
            {
                var readerParam = Expression.Parameter(typeof(DbDataReader), "reader");
                var indexParam = Expression.Parameter(typeof(int), "index");
                var getter = Expression.Convert(ExpressionHelper.GetValueFromReaderAsType(readerParam, indexParam, columnMapping.Type, handlerRegistry), typeof(object));
                var lambda = Expression.Lambda<Func<DbDataReader, int, object>>(getter, readerParam, indexParam);
                return lambda.Compile();
            }

            public ReaderPlan CreateReaderPlan(DbDataReader reader, PreparedCommand command)
            {
                if (command.Mapping != null && command.Mapping != Mapping)
                    throw new InvalidOperationException("You cannot use a different DocumentMap when reading documents except for the map defined for the type.");

                // A reader plan is built for each query we execute, on the assumption that the combination of  
                // SQL query, field count (in case a column is somehow added) and type of record being read 
                // creates a unique identifier.
                var cacheKey = HashCode.Combine(command.Statement, reader.FieldCount, Type);
                return readerPlans.GetOrAdd(cacheKey, _ => CreatePlan(reader));
            }

            ReaderPlan CreatePlan(DbDataReader reader)
            {
                return ReaderPlan.Create(this, reader);
            }
        }

        // We create these once per document type. They allow us to cache a data reader to a ColumnMapping, although we
        // don't know what order the columns will appear in. 
        class TypeColumn
        {
            readonly ColumnMapping column;
            readonly Func<DbDataReader, int, object> readerFunc;

            public TypeColumn(ColumnMapping column, Func<DbDataReader, int, object> readerFunc)
            {
                this.column = column;
                this.readerFunc = readerFunc;
            }

            public string Name => column.ColumnName;

            public ReaderColumn Bind(int index)
            {
                return new ReaderColumn(index, readerFunc, column.ReaderWriter, column.Direction == ColumnDirection.Both || column.Direction == ColumnDirection.FromDatabase);
            }
        }
        
        // We create a ReaderPlan for every query we execute, and it allows us to cache the work we do to figure out 
        // what columns are returned in what order, and to remember if the query returns big documents or not. This saves
        // us from having to check field names or lookup things every time a query is run.
        class ReaderPlan
        {
            public int ExpectedFieldCount;
            public ReaderColumn[] BoundExpectedColumns;
            public int IdIndex = -1;
            public int JsonIndex = -1;
            public int JsonBlobIndex = -1;
            public int TypeIndex = -1;
            public TypePlan TypePlan;

            ReaderPlan(TypePlan typePlan)
            {
                TypePlan = typePlan;
            }

            public static ReaderPlan Create(TypePlan typePlan, DbDataReader reader)
            {
                var plan = new ReaderPlan(typePlan);
                plan.Initialize(reader);
                return plan;
            }

            void Initialize(DbDataReader reader)
            {
                ExpectedFieldCount = reader.FieldCount;
                BoundExpectedColumns = new ReaderColumn[ExpectedFieldCount];

                var idColumnName = TypePlan.Mapping.IdColumn.ColumnName;
                for (var i = 0; i < ExpectedFieldCount; i++)
                {
                    var fieldName = reader.GetName(i);
                    
                    if (string.Equals(fieldName, idColumnName, StringComparison.OrdinalIgnoreCase)) IdIndex = i;
                    if (string.Equals(fieldName, "JSON", StringComparison.OrdinalIgnoreCase)) JsonIndex = i;
                    if (string.Equals(fieldName, "JSONBlob", StringComparison.OrdinalIgnoreCase)) JsonBlobIndex = i;
                    if (string.Equals(fieldName, "Type", StringComparison.OrdinalIgnoreCase)) TypeIndex = i;

                    if (TypePlan.Columns.TryGetValue(fieldName, out var column))
                        BoundExpectedColumns[i] = column.Bind(i);
                }

                AssertExpectedQueryStructure(reader);
            }

            void AssertExpectedQueryStructure(DbDataReader reader)
            {
                if (IdIndex < 0)
                    throw new InvalidOperationException(
                        $"The class '{TypePlan.Type.Name}' has a document map, but the query does not include the 'Id' column. Queries against this type must include the Id in the select clause. Columns returned: " +
                        DebugListReturnedColumnNames(reader));
                
                if (TypePlan.ExpectsJsonText && JsonIndex < 0 && TypePlan.ExpectsJsonCompressed && JsonBlobIndex < 0)
                    throw new InvalidOperationException(
                        $"The class '{TypePlan.Type.Name}' has a document map with JSON storage set to {TypePlan.JsonStorageFormat.ToString()}, but the query does not include either the 'JSON' or 'JSONBlob' column. Queries against this type must include both columns in the select clause. If you just want a few columns, use Nevermores' 'plain class' or tuple support. Columns returned: " +
                        DebugListReturnedColumnNames(reader));
                
                if (TypePlan.ExpectsJsonText && JsonIndex < 0)
                    throw new InvalidOperationException(
                        $"The class '{TypePlan.Type.Name}' has a document map with JSON storage set to {TypePlan.JsonStorageFormat.ToString()}, but the query does not include the 'JSON' column. Queries against this type must include the JSON in the select clause. If you just want a few columns, use Nevermores' 'plain class' or tuple support. Columns returned: " +
                        DebugListReturnedColumnNames(reader));
                
                if (TypePlan.ExpectsJsonCompressed && JsonBlobIndex < 0)
                    throw new InvalidOperationException(
                        $"The class '{TypePlan.Type.Name}' has a document map with JSON storage set to {TypePlan.JsonStorageFormat.ToString()}, but the query does not include the 'JSONBlob' column. Queries against this type must include the JSONBlob in the select clause. If you just want a few columns, use Nevermores' 'plain class' or tuple support. Columns returned: " +
                        DebugListReturnedColumnNames(reader));

                if (TypeIndex >= 0 && TypePlan.ExpectsJsonText && TypeIndex > JsonIndex)
                    throw new InvalidOperationException(
                        $"When querying the document '{TypePlan.Type.Name}', the 'Type' column must always appear before the 'JSON' column. Change the order in the SELECT clause, or if selecting '*', change the order of columns in the table. Columns returned: " +
                        DebugListReturnedColumnNames(reader));
                
                if (TypeIndex >= 0 && TypePlan.ExpectsJsonCompressed && TypeIndex > JsonBlobIndex)
                    throw new InvalidOperationException(
                        $"When querying the document '{TypePlan.Type.Name}', the 'Type' column must always appear before the 'JSONBlob' column. Change the order in the SELECT clause, or if selecting '*', change the order of columns in the table. Columns returned: " +
                        DebugListReturnedColumnNames(reader));
            }

            static string DebugListReturnedColumnNames(IDataRecord reader)
            {
                var colNames = new List<string>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    colNames.Add(reader.GetName(i));
                }
                
                return string.Join(", ", colNames);
            }
        }
        
        // When a specific query runs against a document type, we finally know the index that the column appears in
        // (when we read the first row). A BoundColumn is added to our ReaderPlan as soon as we know the index.  
        class ReaderColumn
        {
            readonly Func<DbDataReader, int, object> reader;
            readonly IPropertyReaderWriter getterSetter;
            readonly bool writable;
            readonly int index;

            public ReaderColumn(int index, Func<DbDataReader, int, object> reader, IPropertyReaderWriter getterSetter, bool writable)
            {
                this.index = index;
                this.reader = reader;
                this.getterSetter = getterSetter;
                this.writable = writable;
            }

            public object Read(DbDataReader dataReader)
            {
                return reader(dataReader, index);
            }

            public void WriteTo(object instance, object value)
            {
                if (writable)
                {
                    getterSetter.Write(instance, value);
                }
            }
        }
    }
}
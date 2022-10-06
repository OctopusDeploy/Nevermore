using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq;
using Nevermore.Advanced.ReaderStrategies.Compilation;
using Nevermore.Mapping;

namespace Nevermore.Advanced.ReaderStrategies.Documents
{
    /// <summary>
    /// The Document reader strategy is how we map query results from documents in Nevermore. It's probably the most
    /// commonly used strategy, and it's called when you use Load, Stream, Query, and so on. 
    /// </summary>
    internal class DocumentReaderStrategy : IReaderStrategy
    {
        readonly RelationalStoreConfiguration configuration;
        readonly ConcurrentDictionary<string, CompiledExpression<DocumentReaderFunc>> cache = new ConcurrentDictionary<string, CompiledExpression<DocumentReaderFunc>>();
        
        public DocumentReaderStrategy(RelationalStoreConfiguration configuration)
        {
            this.configuration = configuration;
        }
        
        public bool CanRead(Type type)
        {
            return configuration.DocumentMaps.ResolveOptional(type, out _);
        }
        
        public Func<PreparedCommand, Func<DbDataReader, (TRecord, bool)>> CreateReader<TRecord>()
        {
            // This is executed once per type that we query. Keep in mind that there might be multiple concrete types, 
            // and they could all have a base that is mapped. All the code we execute when reading will always be based
            // on the base type. The generated code will call any IInstanceTypeHandlers to deal with inheritance, 
            // but our assertion is that there's only one map for a given base type.
            var mapping = configuration.DocumentMaps.Resolve<TRecord>();
            
            return command =>
            {
                if (command.Mapping != null && command.Mapping != mapping)
                    throw new InvalidOperationException("You cannot use a different DocumentMap when reading documents except for the map defined for the type.");

                CompiledExpression<DocumentReaderFunc> compiled = null;
                var context = new DocumentReaderContext(configuration, mapping);

                var rowNumber = 0;
                return dbDataReader =>
                {
                    rowNumber++;
                    context.Column = -1;

                    if (compiled == null)
                    {
                        compiled = GetCachedCompiledExpression(dbDataReader, mapping);
                    }

                    try
                    {
                        var result = compiled.Execute(dbDataReader, context);
                        if (result is TRecord record)
                            return (record, true);
                        return (default, false);
                    }
                    catch (Exception ex)
                    {
                        throw new ReaderException(rowNumber, context.Column, compiled.ExpressionSource, ex);
                    }
                };
            };
        }

        // For each query, we create and compile an expression. This involves looking at the columns returned from the query, 
        // matching them to the document map columns, then generating and compiling an expression optimized for handling
        // this query. It means that the first query will be slow, but every subsequent query will be faster. 
        // Our cache key is based on the columns being returned in the data reader, plus the type. Note that
        // this is the type that the map uses - the actual class being queried might be e.g., AzureAccount,
        // while the type mapped is the Account base class.
        CompiledExpression<DocumentReaderFunc> GetCachedCompiledExpression(IDataReader dbDataReader, DocumentMap mapping)
        {
            var fieldCount = dbDataReader.FieldCount;
            var fieldNames = new string[fieldCount + 1];
            for (var i = 0; i < fieldCount; i++)
            {
                fieldNames[i] = dbDataReader.GetName(i);
            }

            fieldNames[fieldCount] = mapping.Type.FullName;
            var cacheKey = string.Join("|", fieldNames);
            return cache.GetOrAdd(cacheKey, _ => Compile(mapping, dbDataReader));
        }

        CompiledExpression<DocumentReaderFunc> Compile(DocumentMap map, IDataReader firstRow)
        {
            var builder = new DocumentReaderExpressionBuilder(map, configuration.TypeHandlers);
            
            var idColumnName = map.IdColumn.ColumnName;
            var typeResolutionColumnName = map.TypeResolutionColumn?.ColumnName;
            
            for (var i = 0; i < firstRow.FieldCount; i++)
            {
                var fieldName = firstRow.GetName(i);
                var column = map.Columns.FirstOrDefault(c => string.Equals(fieldName, c.ColumnName, StringComparison.OrdinalIgnoreCase));

                if (string.Equals(fieldName, idColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    builder.Id(i, map.IdColumn);
                }
                else if (typeResolutionColumnName != null && string.Equals(fieldName, typeResolutionColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    builder.TypeColumn(i, column);
                }
                else if (string.Equals(fieldName, "JSON", StringComparison.OrdinalIgnoreCase))
                {
                    builder.JsonColumn(i);
                }
                else if (string.Equals(fieldName, "JSONBlob", StringComparison.OrdinalIgnoreCase))
                {
                    builder.JsonBlobColumn(i);
                }
                else if (column != null)
                {
                    builder.Column(i, column);
                }
            }

            var expression = builder.Build();
            return ExpressionCompiler.Compile(expression);
        }
    }
}
using System;
using System.Data;
using System.Linq;
using System.Reflection;
using Nevermore.Mapping;

namespace Nevermore.Advanced.ReaderStrategies.Documents
{
    internal static class DocumentReaderCompiler
    {
        public static ICompiledDocumentReaderPlan CompilePlan(DocumentMap map, IDataRecord firstRow, IRelationalStoreConfiguration configuration)
        {
            var openGenericMethod = typeof(DocumentReaderCompiler).GetMethod(nameof(CompilePlanInternal), BindingFlags.NonPublic | BindingFlags.Static);
            var method = openGenericMethod.MakeGenericMethod(map.Type);

            try
            {
                return (ICompiledDocumentReaderPlan) method.Invoke(null, new object[] {map, firstRow, configuration});
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is InvalidOperationException ioe) 
                    throw ioe;
                throw;
            }
        }

        static ICompiledDocumentReaderPlan CompilePlanInternal<TRecord>(DocumentMap map, IDataRecord firstRow, IRelationalStoreConfiguration configuration) where TRecord : class
        {
            var builder = new DocumentReaderExpressionBuilder<TRecord>(map, configuration.TypeHandlerRegistry);
            
            var idColumnName = map.IdColumn.ColumnName;
            
            for (var i = 0; i < firstRow.FieldCount; i++)
            {
                var fieldName = firstRow.GetName(i);
                var column = map.Columns.FirstOrDefault(c => string.Equals(fieldName, c.ColumnName, StringComparison.OrdinalIgnoreCase));

                if (string.Equals(fieldName, idColumnName, StringComparison.OrdinalIgnoreCase))
                {
                    builder.Id(i, map.IdColumn);
                }
                else if (string.Equals(fieldName, "Type", StringComparison.OrdinalIgnoreCase))
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

            var func = builder.Build();
            return new CompiledDocumentReaderPlan<TRecord>(configuration, map, func);
        }
    }
}
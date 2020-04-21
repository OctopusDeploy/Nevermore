using System;
using System.Data.Common;
using System.Linq.Expressions;
using Nevermore.Advanced.ReaderStrategies.Compilation;

namespace Nevermore.Advanced.ReaderStrategies.Primitives
{
    internal class PrimitiveReaderStrategy : IReaderStrategy
    {
        readonly RelationalStoreConfiguration configuration;

        public PrimitiveReaderStrategy(RelationalStoreConfiguration configuration)
        {
            this.configuration = configuration;
        }
        
        public bool CanRead(Type type)
        {
            return
                type == typeof(string)
                || type == typeof(decimal)
                || type == typeof(byte[])
                || type.IsPrimitive
                || type.IsValueType
                || type.IsEnum
                || (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        public Func<PreparedCommand, Func<DbDataReader, (TRecord, bool)>> CreateReader<TRecord>()
        {
            var readerArg = Expression.Parameter(typeof(DbDataReader), "reader");
            var valueGetter = ExpressionHelper.GetValueFromReaderAsType(readerArg, Expression.Constant(0), typeof(TRecord), configuration.TypeHandlers);

            var lambda = Expression.Lambda<Func<DbDataReader, TRecord>>(valueGetter, readerArg);
            var compiled = ExpressionCompiler.Compile(lambda);

            return command =>
            {
                var rowNumber = 0;
                return reader =>
                {
                    rowNumber++;
                    if (reader.FieldCount != 1)
                    {
                        throw new InvalidOperationException($"Error reading row {rowNumber}. Cannot convert this result set to type {typeof(TRecord).Name}. This result set contains {reader.FieldCount} columns. The result set can only contain a single column.");
                    }

                    try
                    {
                        return (compiled.Execute(reader), true);
                    }
                    catch (Exception ex)
                    {
                        throw new ReaderException(rowNumber, 0, compiled.ExpressionSource, ex);
                    }
                }; 
            };
        }
    }
}
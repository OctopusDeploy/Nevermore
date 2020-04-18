using System;
using System.Data.Common;
using System.Linq.Expressions;
using Nevermore.Util;

namespace Nevermore.Advanced.ReaderStrategies
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
            var reader = Expression.Parameter(typeof(DbDataReader), "reader");
            var valueGetter = ExpressionHelper.GetValueFromReaderAsType(reader, Expression.Constant(0), typeof(TRecord), configuration.TypeHandlerRegistry);
            var lambda = Expression.Lambda<Func<DbDataReader, TRecord>>(valueGetter, reader).Compile();

            return command => 
            { 
                return reader =>
                {
                    if (reader.FieldCount != 1)
                    {
                        throw new InvalidOperationException($"Cannot convert this result set to type {typeof(TRecord).Name}. This result set contains {reader.FieldCount} columns. The result set can only contain a single column.");
                    }

                    return (lambda(reader), true);
                }; 
            };
        }
    }
}
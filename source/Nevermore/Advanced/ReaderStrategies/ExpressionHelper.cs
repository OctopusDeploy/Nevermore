using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;
using Nevermore.Advanced.TypeHandlers;

namespace Nevermore.Advanced.ReaderStrategies
{
    internal static class ExpressionHelper
    {
        public static Expression GetValueFromReaderAsType(Expression reader, Expression index, Type propertyType, ITypeHandlerRegistry customTypeHandlerRegistry)
        {
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance;

            // For value types where we know they cannot be null
            // E.g.,   foo.Age = reader.GetInt32(i)
            var neverNull = new Func<string, Expression>(nameOfGetMethod => Expression.Call(reader, typeof(IDataRecord).GetMethod(nameOfGetMethod, bindingFlags), index));
            
            // For reference types
            // E.g.,   foo.Name = reader.IsDBNull(i) ? null : reader.GetString(i);
            var maybeNull = new Func<string, Expression>(nameOfGetMethod => 
                Expression.Condition(
                    Expression.Call(reader, typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), bindingFlags), index),
                    Expression.Constant(null, propertyType),
                    Expression.Call(reader, typeof(IDataRecord).GetMethod(nameOfGetMethod, bindingFlags), index)));
            
            // For nullable types (whether the method return is slightly different, and needs to be cast)
            // E.g.,   foo.LuckyNumber = reader.IsDBNull(i) ? null : (int?)reader.GetInt32(i); 
            var maybeNullWithCast = new Func<string, Expression>(nameOfGetMethod => 
                Expression.Condition(
                    Expression.Call(reader, typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), bindingFlags), index),
                    Expression.Constant(null, propertyType),
                    Expression.Convert(
                        Expression.Call(reader, typeof(IDataRecord).GetMethod(nameOfGetMethod, bindingFlags), index), 
                        propertyType)));
            
            // Optimize for common types
            if (propertyType == typeof(string)) return maybeNull(nameof(IDataRecord.GetString));
            if (propertyType == typeof(DateTime)) return neverNull(nameof(IDataRecord.GetDateTime));
            if (propertyType == typeof(DateTime?)) return maybeNullWithCast(nameof(IDataRecord.GetDateTime));
            if (propertyType == typeof(int)) return neverNull(nameof(IDataRecord.GetInt32));
            if (propertyType == typeof(int?)) return maybeNullWithCast(nameof(IDataRecord.GetInt32));
            if (propertyType == typeof(decimal)) return neverNull(nameof(IDataRecord.GetDecimal));
            if (propertyType == typeof(decimal?)) return maybeNullWithCast(nameof(IDataRecord.GetDecimal));
            if (propertyType == typeof(char)) return neverNull(nameof(IDataRecord.GetChar));
            if (propertyType == typeof(char?)) return maybeNullWithCast(nameof(IDataRecord.GetChar));
            if (propertyType == typeof(long)) return neverNull(nameof(IDataRecord.GetInt64));
            if (propertyType == typeof(long?)) return maybeNullWithCast(nameof(IDataRecord.GetInt64));
            if (propertyType == typeof(short)) return neverNull(nameof(IDataRecord.GetInt16));
            if (propertyType == typeof(short?)) return maybeNullWithCast(nameof(IDataRecord.GetInt16));
            if (propertyType == typeof(Guid)) return neverNull(nameof(IDataRecord.GetGuid));
            if (propertyType == typeof(Guid?)) return maybeNullWithCast(nameof(IDataRecord.GetGuid));
            if (propertyType == typeof(float)) return neverNull(nameof(IDataRecord.GetFloat));
            if (propertyType == typeof(float?)) return maybeNullWithCast(nameof(IDataRecord.GetFloat));

            // We allow custom type handlers, but not for the primitive types shown above - we deal with so many 
            // of these that we want them to be fast!
            var typeHandler = customTypeHandlerRegistry.Resolve(propertyType);
            if (typeHandler != null)
            {
                var handler = Expression.Constant(typeHandler, typeof(ITypeHandler));
                return Expression.Convert(Expression.Call(handler, typeof(ITypeHandler).GetMethod(nameof(ITypeHandler.ReadDatabase), bindingFlags), reader, index), propertyType);
            }
            
            // Fallback:
            // E.g.,   foo.SomeValue = reader.IsDBNull(i) ? default(float?) : (float?) reader[i];
            return Expression.Condition(
                Expression.Call(reader, typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), bindingFlags), index),
                Expression.Default(propertyType),
                Expression.Convert(
                    Expression.Call(reader, typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), bindingFlags), index), 
                    propertyType));
        }
    }
}
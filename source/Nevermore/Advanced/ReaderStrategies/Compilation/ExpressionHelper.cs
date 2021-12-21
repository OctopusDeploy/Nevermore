using System;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Nevermore.Advanced.TypeHandlers;

namespace Nevermore.Advanced.ReaderStrategies.Compilation
{
    internal static class ExpressionHelper
    {
        const BindingFlags PublicStaticFlags = BindingFlags.Public | BindingFlags.Static;
        const BindingFlags PublicInstanceFlags = BindingFlags.Public | BindingFlags.Instance;

        // ReSharper disable once InconsistentNaming
        static class ITypeHandlerMethods
        {
            static ITypeHandlerMethods()
            {
                ReadDatabase = typeof(ITypeHandler).GetMethod(nameof(ITypeHandler.ReadDatabase), PublicInstanceFlags);
            }

            public static readonly MethodInfo ReadDatabase;
        }

        static class EnumMethods
        {
            static EnumMethods()
            {
                var methodQuery =
                    from method in typeof(Enum).GetMethods(PublicStaticFlags)
                    let parameters = method.GetParameters()
                    where method.Name == nameof(Enum.Parse) &&
                          method.IsGenericMethod &&
                          parameters.Length == 2 &&
                          parameters[0].ParameterType == typeof(string) &&
                          parameters[1].ParameterType == typeof(bool)
                    select method;

                ParseGeneric = methodQuery.Single();
            }

            public static readonly MethodInfo ParseGeneric;
        }

        // ReSharper disable once InconsistentNaming
        static class IDataRecordMethods
        {
            static IDataRecordMethods()
            {
                IsDBNull = typeof(IDataRecord).GetMethod(nameof(IDataRecord.IsDBNull), PublicInstanceFlags);
                GetString = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetString), PublicInstanceFlags);
                GetDateTime = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetDateTime), PublicInstanceFlags);
                GetInt16 = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetInt16), PublicInstanceFlags);
                GetInt32 = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetInt32), PublicInstanceFlags);
                GetInt64 = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetInt64), PublicInstanceFlags);
                GetFloat = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetFloat), PublicInstanceFlags);
                GetDecimal = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetDecimal), PublicInstanceFlags);
                GetChar = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetChar), PublicInstanceFlags);
                GetGuid = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetGuid), PublicInstanceFlags);
                GetValue = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetValue), PublicInstanceFlags);
                GetFieldType = typeof(IDataRecord).GetMethod(nameof(IDataRecord.GetFieldType), PublicInstanceFlags);
            }

            public static readonly MethodInfo IsDBNull;
            public static readonly MethodInfo GetString;
            public static readonly MethodInfo GetDateTime;
            public static readonly MethodInfo GetInt16;
            public static readonly MethodInfo GetInt32;
            public static readonly MethodInfo GetInt64;
            public static readonly MethodInfo GetFloat;
            public static readonly MethodInfo GetDecimal;
            public static readonly MethodInfo GetChar;
            public static readonly MethodInfo GetGuid;
            public static readonly MethodInfo GetValue;
            public static readonly MethodInfo GetFieldType;
        }

        public static Expression GetValueFromReaderAsType(Expression reader, Expression index, Type propertyType, ITypeHandlerRegistry customTypeHandlerRegistry)
        {
            // For value types where we know they cannot be null
            // E.g.,   foo.Age = reader.GetInt32(i)
            var neverNull = new Func<MethodInfo, Expression>(getMethod => Expression.Call(reader, getMethod, index));
            
            // For reference types
            // E.g.,   foo.Name = reader.IsDBNull(i) ? null : reader.GetString(i);
            var maybeNull = new Func<MethodInfo, Expression>(getMethod => 
                Expression.Condition(
                    Expression.Call(reader, IDataRecordMethods.IsDBNull, index),
                    Expression.Constant(null, propertyType),
                    Expression.Call(reader, getMethod, index)));
            
            // For nullable types (whether the method return is slightly different, and needs to be cast)
            // E.g.,   foo.LuckyNumber = reader.IsDBNull(i) ? null : (int?)reader.GetInt32(i); 
            var maybeNullWithCast = new Func<MethodInfo, Expression>(getMethod => 
                Expression.Condition(
                    Expression.Call(reader, IDataRecordMethods.IsDBNull, index),
                    Expression.Constant(null, propertyType),
                    Expression.Convert(Expression.Call(reader, getMethod, index), propertyType)));
            
            // Optimize for common types
            if (propertyType == typeof(string)) return maybeNull(IDataRecordMethods.GetString);
            if (propertyType == typeof(DateTime)) return neverNull(IDataRecordMethods.GetDateTime);
            if (propertyType == typeof(DateTime?)) return maybeNullWithCast(IDataRecordMethods.GetDateTime);
            if (propertyType == typeof(int)) return neverNull(IDataRecordMethods.GetInt32);
            if (propertyType == typeof(int?)) return maybeNullWithCast(IDataRecordMethods.GetInt32);
            if (propertyType == typeof(decimal)) return neverNull(IDataRecordMethods.GetDecimal);
            if (propertyType == typeof(decimal?)) return maybeNullWithCast(IDataRecordMethods.GetDecimal);
            if (propertyType == typeof(char)) return neverNull(IDataRecordMethods.GetChar);
            if (propertyType == typeof(char?)) return maybeNullWithCast(IDataRecordMethods.GetChar);
            if (propertyType == typeof(long)) return neverNull(IDataRecordMethods.GetInt64);
            if (propertyType == typeof(long?)) return maybeNullWithCast(IDataRecordMethods.GetInt64);
            if (propertyType == typeof(short)) return neverNull(IDataRecordMethods.GetInt16);
            if (propertyType == typeof(short?)) return maybeNullWithCast(IDataRecordMethods.GetInt16);
            if (propertyType == typeof(Guid)) return neverNull(IDataRecordMethods.GetGuid);
            if (propertyType == typeof(Guid?)) return maybeNullWithCast(IDataRecordMethods.GetGuid);
            if (propertyType == typeof(float)) return neverNull(IDataRecordMethods.GetFloat);
            if (propertyType == typeof(float?)) return maybeNullWithCast(IDataRecordMethods.GetFloat);

            // Enum or Nullable<Enum>
            if (propertyType.IsEnum || (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(Nullable<>) && propertyType.GetGenericArguments()[0].IsEnum))
            {
                var underlyingEnumType = propertyType.IsEnum ? propertyType : propertyType.GetGenericArguments()[0]; 
                
                // reader.IsDBNull(0)
                //     ? default(TEnum?)
                //     : (TEnum?)(reader.GetFieldType(0) == typeof(string)
                //         ? (Enum.Parse<TEnum>(reader.GetString(0)))
                //         : ((TEnum)reader.GetValue(0)));
                return Expression.Condition(
                    Expression.Call(reader, IDataRecordMethods.IsDBNull, index),
                    Expression.Default(propertyType),
                    Expression.Convert(
                        Expression.Condition(
                            Expression.Equal(
                                Expression.Call(reader, IDataRecordMethods.GetFieldType, index),
                                Expression.Constant(typeof(string), typeof(Type))),
                            Expression.Call(null,
                                EnumMethods.ParseGeneric.MakeGenericMethod(underlyingEnumType),
                                Expression.Call(reader, IDataRecordMethods.GetString, index),
                                Expression.Constant(true) // ignoreCase
                            ),
                            Expression.Convert(
                                Expression.Call(reader, IDataRecordMethods.GetValue, index),
                                underlyingEnumType)),
                        propertyType
                        ));
            }

            // We allow custom type handlers, but not for the primitive types shown above - we deal with so many 
            // of these that we want them to be fast!
            var typeHandler = customTypeHandlerRegistry.Resolve(propertyType);
            if (typeHandler != null)
            {
                var handler = Expression.Constant(typeHandler, typeof(ITypeHandler));
                return Expression.Convert(Expression.Call(handler, ITypeHandlerMethods.ReadDatabase, reader, index), propertyType);
            }
            
            // Fallback:
            // E.g.,   foo.SomeValue = reader.IsDBNull(i) ? default(float?) : (float?) reader[i];
            return Expression.Condition(
                Expression.Call(reader, IDataRecordMethods.IsDBNull, index),
                Expression.Default(propertyType),
                Expression.Convert(
                    Expression.Call(reader, IDataRecordMethods.GetValue, index), 
                    propertyType));
        }
    }
}
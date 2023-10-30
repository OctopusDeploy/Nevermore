using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Nevermore.Util
{
    public static class TypeExtensions
    {
        static readonly HashSet<Type> KnownScalarTypes = new()
        {
            typeof(DateTimeOffset)
        };

        public static Type GetSequenceType(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsGenericTypeDefinition)
                throw new ArgumentException();

            var enumerableType = typeInfo
                .ImplementedInterfaces
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IEnumerable<>));

            if (enumerableType == null)
                throw new ArgumentException("Provided type is not an IEnumerable.");

            return enumerableType.GetGenericArguments().Single();
        }

        public static object GetDefaultValue(this Type type)
        {
            return type.IsValueType
                ? Activator.CreateInstance(type)
                : null;
        }

        public static bool IsScalar(this Type type)
        {
            // there are some types that can be consider scalars but have a TypeCode of Object
            if (KnownScalarTypes.Contains(type))
            {
                return true;
            }

            return Type.GetTypeCode(type) switch
            {
                TypeCode.Object => false,
                _ => true
            };
        }

        public static string GetDbType(this Type type)
        {
            return Type.GetTypeCode(type) switch
            {
                TypeCode.String => "nvarchar(max)",
                TypeCode.Int16 => "int",
                TypeCode.Double => "double",
                TypeCode.Boolean => "bit",
                TypeCode.Char => "char(max)",
                TypeCode.DateTime => "datetime2",
                TypeCode.Decimal => "decimal(18,8)",
                TypeCode.Int32 => "int",
                TypeCode.Int64 => "bigint",
                TypeCode.SByte => "tinyint",
                TypeCode.Single => "float",
                _ => throw new ArgumentOutOfRangeException()
            };
        }
    }
}
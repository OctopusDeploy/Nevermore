using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Nevermore.Contracts;

namespace Nevermore.Mapping
{
    public static class DatabaseTypeConverter
    {
        static readonly Dictionary<Type, DbType> TypeMap;

        static DatabaseTypeConverter()
        {
            TypeMap = new Dictionary<Type, DbType>();
            TypeMap[typeof (byte)] = DbType.Byte;
            TypeMap[typeof (sbyte)] = DbType.SByte;
            TypeMap[typeof (short)] = DbType.Int16;
            TypeMap[typeof (ushort)] = DbType.UInt16;
            TypeMap[typeof (int)] = DbType.Int32;
            TypeMap[typeof (uint)] = DbType.UInt32;
            TypeMap[typeof (long)] = DbType.Int64;
            TypeMap[typeof (ulong)] = DbType.UInt64;
            TypeMap[typeof (float)] = DbType.Single;
            TypeMap[typeof (double)] = DbType.Double;
            TypeMap[typeof (Decimal)] = DbType.Decimal;
            TypeMap[typeof (bool)] = DbType.Boolean;
            TypeMap[typeof (string)] = DbType.String;
            TypeMap[typeof (char)] = DbType.StringFixedLength;
            TypeMap[typeof (Guid)] = DbType.Guid;
            TypeMap[typeof (DateTime)] = DbType.DateTime;
            TypeMap[typeof (DateTimeOffset)] = DbType.DateTimeOffset;
            TypeMap[typeof (TimeSpan)] = DbType.Time;
            TypeMap[typeof (byte[])] = DbType.Binary;
            TypeMap[typeof (byte?)] = DbType.Byte;
            TypeMap[typeof (sbyte?)] = DbType.SByte;
            TypeMap[typeof (short?)] = DbType.Int16;
            TypeMap[typeof (ushort?)] = DbType.UInt16;
            TypeMap[typeof (int?)] = DbType.Int32;
            TypeMap[typeof (uint?)] = DbType.UInt32;
            TypeMap[typeof (long?)] = DbType.Int64;
            TypeMap[typeof (ulong?)] = DbType.UInt64;
            TypeMap[typeof (float?)] = DbType.Single;
            TypeMap[typeof (double?)] = DbType.Double;
            TypeMap[typeof (Decimal?)] = DbType.Decimal;
            TypeMap[typeof (bool?)] = DbType.Boolean;
            TypeMap[typeof (char?)] = DbType.StringFixedLength;
            TypeMap[typeof (Guid?)] = DbType.Guid;
            TypeMap[typeof (DateTime?)] = DbType.DateTime;
            TypeMap[typeof (DateTimeOffset?)] = DbType.DateTimeOffset;
            TypeMap[typeof (TimeSpan?)] = DbType.Time;
            TypeMap[typeof (object)] = DbType.Object;
        }

        public static void SetMapping(Type propertyType, DbType dbType)
        {
            TypeMap[propertyType] = dbType;
        }

        public static DbType AsDbType(Type propertyType)
        {
            if (propertyType.GetTypeInfo().IsEnum)
            {
                return DbType.String;
            }

            if (propertyType == typeof (ReferenceCollection))
            {
                return DbType.String;
            }

            if (typeof(ITinyType<object>).IsAssignableFrom(propertyType))
            {
                var innerType = GetTinyTypeInnerType(propertyType);
                return AsDbType(innerType);
            }

            DbType result;
            if (!TypeMap.TryGetValue(propertyType, out result))
                throw new KeyNotFoundException("Cannot map database type from: " + propertyType.FullName);
            return result;
        }

        static Type GetTinyTypeInnerType(Type propertyType)
        {
            var tinyTypeInterface = propertyType
                .GetInterfaces()
                .Single(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITinyType<>));

            return tinyTypeInterface.GenericTypeArguments.Single();
        }
    }
}
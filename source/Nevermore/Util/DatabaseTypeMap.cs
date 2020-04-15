using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace Nevermore.Util
{
    // TODO: Remove this
    internal static class DatabaseTypeConverter
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

        public static DbType? AsDbType(Type propertyType)
        {
            if (propertyType.GetTypeInfo().IsEnum)
            {
                return DbType.String;
            }

            DbType result;
            if (!TypeMap.TryGetValue(propertyType, out result))
                return null;
            return result;
        }
    }
}
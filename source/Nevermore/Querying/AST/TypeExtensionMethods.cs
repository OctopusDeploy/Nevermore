using System;

namespace Nevermore.Querying.AST
{
    public static class TypeExtensionMethods
    {
        public static string ToDbType(this Type type)
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
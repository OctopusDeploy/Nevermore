using System;
using System.Collections.Generic;

namespace Nevermore.Advanced.Queryable
{
    public static class TypeExtensions
    {
        static readonly HashSet<Type> KnownScalarTypes = new()
        {
            typeof(DateTimeOffset)
        };


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
    }
}
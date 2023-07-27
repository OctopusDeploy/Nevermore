using System;
using System.Collections.Generic;
using System.Reflection;

namespace Nevermore.Advanced.Queryable
{
    internal static class PropertyInfoExtensions
    {
        static readonly HashSet<Type> KnownScalarTypes = new()
        {
            typeof(DateTimeOffset)
        };

        public static bool Matches(this PropertyInfo propertyInfo, PropertyInfo other)
        {
            return propertyInfo.Name.Equals(other.Name) &&
                   propertyInfo.PropertyType.IsAssignableFrom(other.PropertyType);
        }

        public static bool IsScalar(this PropertyInfo propertyInfo)
        {
            // there are some types that can be consider scalars but have a TypeCode of Object
            if (KnownScalarTypes.Contains(propertyInfo.PropertyType))
            {
                return true;
            }

            return Type.GetTypeCode(propertyInfo.PropertyType) switch
            {
                TypeCode.Object => false,
                _ => true
            };
        }
    }
}
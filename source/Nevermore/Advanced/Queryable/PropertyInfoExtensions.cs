using System;
using System.Reflection;

namespace Nevermore.Advanced.Queryable
{
    internal static class PropertyInfoExtensions
    {
        public static bool Matches(this PropertyInfo propertyInfo, PropertyInfo other)
        {
            return propertyInfo.Name.Equals(other.Name) &&
                   propertyInfo.PropertyType.IsAssignableFrom(other.PropertyType);
        }
    }
}
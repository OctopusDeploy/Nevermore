using System;

namespace Nevermore.Util
{
    static class StronglyTypeStringExtensionMethods
    {
        public static bool IsStronglyTypedString(this Type type)
        {
            var property = type.GetProperty("Value");
            return type.GetProperties().Length == 1 && property != null && property.PropertyType == typeof(string);
        }
    }
}
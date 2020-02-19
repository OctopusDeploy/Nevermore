using System;
using System.Linq;
using System.Reflection;
using Octopus.TinyTypes;

namespace Nevermore.Util
{
    internal static class TinyTypeExtensions
    {
        public static Type GetTinyTypeInterface(this Type type)
        {
            return type
                .GetInterfaces()
                .SingleOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ITinyType<>));
        }

        public static object GetTinyTypeValue(this Type tinyType, object value)
        {
            if (value == null) 
                throw new ArgumentNullException(nameof(value));
            
            var property = tinyType.GetProperty(nameof(ITinyType<object>.Value), BindingFlags.Instance | BindingFlags.Public);
            if (property == null)
                throw new InvalidOperationException("Value property not found on ITinyType<> implementation" + value.GetType());

            return property.GetValue(value);
        }
    }
}
using System;
using System.Collections;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Nevermore.Contracts;

namespace Nevermore.Mapping
{
    /// <summary>
    /// The one and only <see cref="AmazingConverter" />. Can convert from absolutely anything to absolutely
    /// anything.
    /// </summary>
    public static class AmazingConverter
    {
        /// <summary>
        /// If it can be converted, the <see cref="AmazingConverter" /> will figure out how. Given a source
        /// object, tries its best to convert it to the target type.
        /// </summary>
        /// <param name="source">The source.</param>
        /// <param name="targetType">The type to convert the source object to.</param>
        /// <returns></returns>
        public static object Convert(object source, Type targetType)
        {

            var typeInfo = targetType.GetTypeInfo();

            if (source == null || source == DBNull.Value)
            {
                // Returns the default(T) of the type
                return typeInfo.IsValueType
                    ? Activator.CreateInstance(targetType)
                    : null;
            }

            var sourceType = source.GetType();

            // Try casting
            if (typeInfo.IsAssignableFrom(sourceType))
                return source;

            // Enums!
            if (typeInfo.IsEnum && sourceType == typeof(string))
                return Enum.Parse(targetType, (string)source, true);

            // Try type descriptors
            var targetConverter = TypeDescriptor.GetConverter(targetType);
            if (targetConverter.CanConvertFrom(sourceType))
            {
                return targetConverter.ConvertFrom(source);
            }

            var sourceConverter = TypeDescriptor.GetConverter(sourceType);
            if (sourceConverter.CanConvertTo(targetType))
            {
                return sourceConverter.ConvertTo(source, targetType);
            }

            // Find an implicit assignment converter
            var implicitAssignment = typeInfo.GetMethods(BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Static)
                .FirstOrDefault(x => x.Name == "op_Implicit" &&
                    typeInfo.IsAssignableFrom(x.ReturnType));

            if (implicitAssignment != null)
            {
                return implicitAssignment.Invoke(null, new[] { source });
            }

            var enumerable = source as IEnumerable;
            if (enumerable != null)
            {
                if (targetType.IsArray)
                {
                    var list = enumerable.Cast<object>().ToArray();
                    var elt = targetType.GetElementType();
                    var result = Array.CreateInstance(elt, list.Length);
                    for (var i = 0; i < list.Length; i++)
                    {
                        var el = Convert(list[i], elt);
                        result.SetValue(el, i);
                    }

                    return result;
                }
            }

            if (targetType == typeof(string))
            {
                return source.ToString();
            }

            var s = source as string;

            if (s != null && targetType == typeof(Uri))
                return new Uri(s);

            if (s != null && typeof(IIdWrapper).IsAssignableFrom(targetType))
                return Activator.CreateInstance(targetType, source);

            // Hope and pray
            return source;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore
{
    public static class ListExtensions
    {
        /// <summary>
        /// Same as Contains but in the opposite calling structure. In Nevermore LINQ queries, translates to "WHERE X IN (@param1, @param2)...".
        /// In other cases calls collection.Contains(value). 
        /// </summary>
        /// <param name="value">The value to search for</param>
        /// <param name="collection">The collection to search within.</param>
        /// <returns>True if <paramref name="value"/> exists in <paramref name="collection"/></returns>
        public static bool In<T>(this T value, IEnumerable<T> collection) where T : struct, IConvertible
        {
            return collection.Contains(value);
        }
        
        /// <summary>
        /// Same as Contains but in the opposite calling structure. In Nevermore LINQ queries, translates to "WHERE X IN (@param1, @param2)...".
        /// In other cases calls collection.Contains(value). 
        /// </summary>
        /// <param name="value">The value to search for</param>
        /// <param name="collection">The collection to search within.</param>
        /// <returns>True if <paramref name="value"/> exists in <paramref name="collection"/></returns>
        public static bool In<T>(this T? value, IEnumerable<T?> collection) where T : struct, IConvertible
        {
            return collection.Contains(value);
        }
        
        /// <summary>
        /// Same as Contains but in the opposite calling structure. In Nevermore LINQ queries, translates to "WHERE X IN (@param1, @param2)...".
        /// In other cases calls collection.Contains(value). 
        /// </summary>
        /// <param name="value">The value to search for</param>
        /// <param name="collection">The collection to search within.</param>
        /// <returns>True if <paramref name="value"/> exists in <paramref name="collection"/></returns>
        public static bool In(this string value, IEnumerable<string> collection)
        {
            return collection.Contains(value);
        }
        
        /// <summary>
        /// Same as !Contains but in the opposite calling structure. In Nevermore LINQ queries, translates to "WHERE X NOT IN (@param1, @param2)...".
        /// In other cases calls !collection.Contains(value). 
        /// </summary>
        /// <param name="value">The value to search for</param>
        /// <param name="collection">The collection to search within.</param>
        /// <returns>True if <paramref name="value"/> exists in <paramref name="collection"/></returns>
        public static bool NotIn<T>(this T value, IEnumerable<T> collection) where T : struct, IConvertible
        {
            return !collection.Contains(value);
        }
        
        
        /// <summary>
        /// Same as !Contains but in the opposite calling structure. In Nevermore LINQ queries, translates to "WHERE X NOT IN (@param1, @param2)...".
        /// In other cases calls !collection.Contains(value). 
        /// </summary>
        /// <param name="value">The value to search for</param>
        /// <param name="collection">The collection to search within.</param>
        /// <returns>True if <paramref name="value"/> exists in <paramref name="collection"/></returns>
        public static bool NotIn<T>(this T? value, IEnumerable<T?> collection) where T : struct, IConvertible
        {
            return !collection.Contains(value);
        }
        
        /// <summary>
        /// Same as !Contains but in the opposite calling structure. In Nevermore LINQ queries, translates to "WHERE X NOT IN (@param1, @param2)...".
        /// In other cases calls !collection.Contains(value). 
        /// </summary>
        /// <param name="value">The value to search for</param>
        /// <param name="collection">The collection to search within.</param>
        /// <returns>True if <paramref name="value"/> exists in <paramref name="collection"/></returns>
        public static bool NotIn(this string value, IEnumerable<string> collection)
        {
            return !collection.Contains(value);
        }
    }
}
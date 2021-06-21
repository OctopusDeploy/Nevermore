using System;

namespace Nevermore.Mapping
{
    public interface IStringBasedPrimitivePrimaryKeyHandler : IPrimitivePrimaryKeyHandler
    {
        /// <summary>
        /// Set a function that when given the TableName will return key prefix string.
        /// </summary>
        /// <param name="idPrefix">The function to call back to get the prefix.</param>
        void SetPrefix(Func<string, string> idPrefix);

        /// <summary>
        /// Given a tableName, get the prefix for the key.
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns>The key prefix for the given tableName</returns>
        string GetPrefix(string tableName);

        /// <summary>
        /// Set a function that format a key value, given a prefix and a key number.
        /// </summary>
        /// <param name="format">The function to call back to format the id.</param>
        void SetFormat(Func<(string idPrefix, int key), string> format);

        object FormatKey(string tableName, int key);
    }
}
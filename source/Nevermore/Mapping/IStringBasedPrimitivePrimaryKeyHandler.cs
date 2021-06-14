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
        /// Set a function that format a key value, given a prefix and a key number.
        /// </summary>
        /// <param name="format">The function to call back to format the id.</param>
        void SetFormat(Func<(string idPrefix, int key), string> format);
    }
}
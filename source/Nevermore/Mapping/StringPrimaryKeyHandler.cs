#nullable enable
using System;

namespace Nevermore.Mapping
{
    class StringPrimaryKeyHandler : PrimitivePrimaryKeyHandler<string>, IStringBasedPrimitivePrimaryKeyHandler
    {
        Func<string, string> idPrefixFunc;
        Func<(string idPrefix, int key), string> formatFunc;
        public StringPrimaryKeyHandler(Func<string, string>? idPrefix = null, Func<(string idPrefix, int key), string>? format = null)
        {
            idPrefixFunc = idPrefix ?? (x => $"{x}s");
            formatFunc = format ?? (x => $"{x.idPrefix}-{x.key}");
        }

        /// <summary>
        /// Set a function that when given the TableName will return key prefix string.
        /// </summary>
        /// <param name="idPrefix">The function to call back to get the prefix.</param>
        public void SetPrefix(Func<string, string> idPrefix)
        {
            idPrefixFunc = idPrefix;
        }

        public string GetPrefix(string tableName)
        {
            return idPrefixFunc(tableName);
        }

        /// <summary>
        /// Set a function that format a key value, given a prefix and a key number.
        /// </summary>
        /// <param name="format">The function to call back to format the id.</param>
        public void SetFormat(Func<(string idPrefix, int key), string> format)
        {
            formatFunc = format;
        }

        public override object FormatKey(string tableName, int key)
        {
            return formatFunc((GetPrefix(tableName), key));
        }
    }
}
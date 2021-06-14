#nullable enable
using System;

namespace Nevermore.Mapping
{
    class StringPrimaryKeyHandler : PrimitivePrimaryKeyHandler<string>, IStringBasedPrimitivePrimaryKeyHandler
    {
        Func<(string tableName, int key), string> idPrefixFunc;
        public StringPrimaryKeyHandler(Func<(string tableName, int key), string>? idPrefix = null)
        {
            idPrefixFunc = idPrefix ?? (x => $"{x.tableName}s-{x.key}");
        }

        public void SetIdPrefix(Func<(string tableName, int key), string> idPrefix)
        {
            idPrefixFunc = idPrefix;
        }

        public override object FormatKey(string tableName, int key)
        {
            return idPrefixFunc((tableName, key));
        }
    }
}
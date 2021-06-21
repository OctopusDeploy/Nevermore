#nullable enable
using System;

namespace Nevermore.Mapping
{
    class StringPrimaryKeyHandler : PrimaryKeyHandler<string>
    {
        readonly Func<string, string> idPrefix;
        readonly Func<(string idPrefix, int key), string> format;
        
        public StringPrimaryKeyHandler(Func<string, string>? idPrefix = null, Func<(string idPrefix, int key), string>? format = null)
        {
            this.idPrefix = idPrefix ?? (tableName => $"{tableName}s");
            this.format = format ?? (x => $"{x.idPrefix}-{x.key}");
        }

        public override object GetNextKey(IKeyAllocator keyAllocator, string tableName)
        {
            var nextKey = keyAllocator.NextId(tableName);
            return format((idPrefix(tableName), nextKey));
        }
    }
}
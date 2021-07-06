#nullable enable
using System;
using System.Data;
using Microsoft.Data.SqlClient.Server;

namespace Nevermore.Mapping
{
    public sealed class StringPrimaryKeyHandler : PrimaryKeyHandler<string>
    {
        readonly Func<(string idPrefix, int key), string> format;

        public StringPrimaryKeyHandler(string? idPrefix = null, Func<(string idPrefix, int key), string>? format = null)
        {
            IdPrefix = idPrefix;
            this.format = format ?? (x => $"{x.idPrefix}-{x.key}");
        }

        public override SqlMetaData GetSqlMetaData(string name)
            =>  new SqlMetaData(name, SqlDbType.NVarChar, 300);

        public string? IdPrefix { get; private set; }

        public override object GetNextKey(IKeyAllocator keyAllocator, string tableName)
        {
            var nextKey = keyAllocator.NextId(tableName);
            return format((IdPrefix ?? $"{tableName}s", nextKey));
        }
    }
}
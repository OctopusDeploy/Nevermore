#nullable enable
using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient.Server;

namespace Nevermore.Mapping
{
    public sealed class StringPrimaryKeyHandler : AsyncPrimaryKeyHandler<string>
    {
        readonly Func<(string idPrefix, long key), string> format;

        public StringPrimaryKeyHandler(string? idPrefix = null, Func<(string idPrefix, long key), string>? format = null)
        {
            IdPrefix = idPrefix;
            this.format = format ?? (x => $"{x.idPrefix}-{x.key}");
        }

        public override SqlMetaData GetSqlMetaData(string name) => new(name, SqlDbType.NVarChar, 300);

        public string? IdPrefix { get; }

        public override object GetNextKey(IKeyAllocator keyAllocator, string tableName)
        {
            var nextKey = keyAllocator.NextId(tableName);
            return format((IdPrefix ?? $"{tableName}s", nextKey));
        }

        public override async ValueTask<object> GetNextKeyAsync(IKeyAllocator keyAllocator, string tableName, CancellationToken cancellationToken)
        {
            var nextKey = await keyAllocator.NextIdAsync(tableName, cancellationToken).ConfigureAwait(false);
            return format((IdPrefix ?? $"{tableName}s", nextKey));
        }
    }
}
using System;
using System.Collections.Concurrent;

namespace Nevermore
{
    public class TableColumnsCache : ConcurrentDictionary<string, string[]>, ITableColumnsCache
    {
        public string[] GetOrAdd(string schemaName, string tableName, Func<string, string, string[]> valueFactory)
        {
            var key = $"{schemaName}.{tableName}";

            var columnNames = GetOrAdd(key, (_) => valueFactory(schemaName, tableName));

            return columnNames;
        }
    }
}
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore
{
    public class TableColumnsCache : ITableColumnsCache
    {
        readonly TableColumnNameResolver tableColumnNameResolver;
        readonly ConcurrentDictionary<string, List<string>> mappingColumnNamesSortedWithJsonLastCache = new();

        public TableColumnsCache(TableColumnNameResolver tableColumnNameResolver)
        {
            this.tableColumnNameResolver = tableColumnNameResolver;
        }

        public IReadOnlyList<string> GetMappingTableColumnNamesSortedWithJsonLast(string schemaName, string tableName)
        {
            var key = $"{schemaName}.{tableName}";
            if (mappingColumnNamesSortedWithJsonLastCache.ContainsKey(key))
            {
                return mappingColumnNamesSortedWithJsonLastCache[key];
            }

            var columnNames = tableColumnNameResolver.GetColumnNames(tableName);
            mappingColumnNamesSortedWithJsonLastCache.TryAdd(key, tableColumnNameResolver.GetColumnNames(tableName));

            return columnNames;
        }
    }
}
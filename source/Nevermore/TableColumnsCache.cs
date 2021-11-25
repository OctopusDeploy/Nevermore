using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore
{
    public class TableColumnsCache : ITableColumnsCache
    {
        readonly IRelationalStore store;
        readonly ConcurrentDictionary<string, List<string>> mappingColumnNamesSortedWithJsonLastCache = new();

        public TableColumnsCache(IRelationalStore store)
        {
            this.store = store;
        }

        public void SetValue(string key, string tableName)
        {
            mappingColumnNamesSortedWithJsonLastCache.TryAdd(key, GetColumnNames(tableName));
        }
        
        public IEnumerable<string> GetMappingTableColumnNamesSortedWithJsonLast(string schemaName, string tableName)
        {
            var key = $"{schemaName}.{tableName}";
            if (mappingColumnNamesSortedWithJsonLastCache.ContainsKey(key))
            {
                return mappingColumnNamesSortedWithJsonLastCache[key];
            }

            var columnNames = GetColumnNames(tableName);
            SetValue(key, tableName);

            return columnNames;
        }

        protected virtual List<string> GetColumnNames(string tableName)
        {
            using var transaction = store.BeginTransaction();
            var getColumnNamesWithJsonLastQuery = @$"
SELECT c.name
FROM sys.tables AS t
INNER JOIN sys.all_columns AS c ON c.object_id = t.object_id
WHERE t.name = '{tableName}'
ORDER BY (CASE WHEN c.name = 'JSON' THEN 1 ELSE 0 END) ASC, c.column_id
";
            return transaction.Stream<string>(getColumnNamesWithJsonLastQuery).ToList();
        }
    }
}
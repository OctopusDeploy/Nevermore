using System.Linq;

namespace Nevermore
{
    public class CachingTableColumnNameResolver : ITableColumnNameResolver
    {
        readonly ITableColumnNameResolver inner;
        readonly ITableColumnsCache tableColumnsCache;

        public CachingTableColumnNameResolver(ITableColumnNameResolver inner, ITableColumnsCache tableColumnsCache)
        {
            this.inner = inner;
            this.tableColumnsCache = tableColumnsCache;
        }

        public string[] GetColumnNames(string schemaName, string tableName)
        {
            var columnNames = tableColumnsCache.GetOrAdd(schemaName, tableName, inner.GetColumnNames);
            return columnNames.ToArray();
        }
    }
}
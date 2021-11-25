using System.Collections.Generic;

namespace Nevermore
{
    public interface ITableColumnsCache
    {
        IEnumerable<string> GetMappingTableColumnNamesSortedWithJsonLast(string schemaName, string tableName);
    }
}
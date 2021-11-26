using System.Collections.Generic;

namespace Nevermore
{
    public interface ITableColumnsCache
    {
        IReadOnlyList<string> GetMappingTableColumnNamesSortedWithJsonLast(string schemaName, string tableName);
    }
}
using System.Collections.Generic;

namespace Nevermore.Tests
{
    public class MockTableColumnsCache<TRecord> : ITableColumnsCache
    {
        public MockTableColumnsCache()
        {
            
        }
        
        public IEnumerable<string> GetMappingTableColumnNamesSortedWithJsonLast(string schemaName, string tableName)
        {
            //TODO:
            return new List<string>();
        }
    }
}
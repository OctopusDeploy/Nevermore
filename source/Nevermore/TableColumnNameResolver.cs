using System.Collections.Generic;
using System.Linq;

namespace Nevermore
{
    public class TableColumnNameResolver
    {
        readonly IRelationalStore store;

        public TableColumnNameResolver(IRelationalStore store)
        {
            this.store = store;
        }

        public virtual List<string> GetColumnNames(string tableName)
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
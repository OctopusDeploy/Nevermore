using System;
using System.Linq;

namespace Nevermore.TableColumnNameResolvers
{
    public class JsonLastTableColumnNameResolver : ITableColumnNameResolver
    {
        readonly IRelationalStore store;

        public JsonLastTableColumnNameResolver(IRelationalStore store)
        {
            this.store = store;
        }

        public string[] GetColumnNames(string schemaName, string tableName)
        {
            var schemaClause = string.IsNullOrEmpty(schemaName)
                ? ""
                : "AND s.name = @schemaName";

            var getColumnNamesWithJsonLastQuery = @$"
SELECT c.name
FROM (
    SELECT object_id, schema_id, name FROM sys.tables
    UNION ALL 
    SELECT object_id, schema_id, name FROM sys.views
) as t
INNER JOIN sys.all_columns AS c ON c.object_id = t.object_id
INNER JOIN sys.schemas AS s ON t.schema_id = s.schema_id
WHERE t.name = @tableName {schemaClause}
ORDER BY (CASE WHEN c.name = 'JSON' THEN 1 ELSE 0 END) ASC, c.column_id";

            var parameters = new CommandParameterValues
            {
                {nameof(tableName), tableName},
                {nameof(schemaName), schemaName}
            };

            // We open our own transaction here rather than reusing an existing one as it's entirely likely that
            // something will already be reading from an existing one when we attempt to lazily load the available
            // columns for another query. That would cause a deadlock, which is generally suboptimal.
            // By creating our own transaction we can guarantee that we're not going to cause either a deadlock
            // or a "multiple active record sets" condition when we load column names.
            using var queryExecutor = store.BeginReadTransaction();
            var columnNames = queryExecutor.Stream<string>(getColumnNamesWithJsonLastQuery, parameters).ToArray();

            if (!columnNames.Any())
                throw new Exception($"No columns found for table or view '{schemaName}.{tableName}'. The table or view likely does not exist in that schema, or the user does not have view definition SQL permission.");

            return columnNames;
        }
    }
}
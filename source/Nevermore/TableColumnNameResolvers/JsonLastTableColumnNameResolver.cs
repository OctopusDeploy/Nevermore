using System;
using System.Linq;

namespace Nevermore.TableColumnNameResolvers
{
    public class JsonLastTableColumnNameResolver : ITableColumnNameResolver
    {
        readonly IReadQueryExecutor queryExecutor;

        public JsonLastTableColumnNameResolver(IReadQueryExecutor queryExecutor)
        {
            this.queryExecutor = queryExecutor;
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
                { nameof(tableName), tableName },
                { nameof(schemaName), schemaName }
            };

            var columnNames = queryExecutor.Stream<string>(getColumnNamesWithJsonLastQuery, parameters).ToArray();

            if (!columnNames.Any())
                throw new Exception($"No columns found for table or view '{schemaName}.{tableName}'. The table or view likely does not exist in that schema, or the user does not have view definition SQL permission.");

            return columnNames;
        }
    }
}
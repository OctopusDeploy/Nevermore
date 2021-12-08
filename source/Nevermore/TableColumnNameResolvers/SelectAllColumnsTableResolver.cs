namespace Nevermore.TableColumnNameResolvers
{
    // Can be used as a replacement if the consumers either don't using table types
    // or if their tables are structured to have the type column before the json column
    public class SelectAllColumnsTableResolver : ITableColumnNameResolver {
        public string[] GetColumnNames(string schemaName, string tableName)
        {
            return new[] {"*"};
        }
    }
}
namespace Nevermore.TableColumnNameResolvers
{
    public interface ITableColumnNameResolver
    {
        string[] GetColumnNames(string schemaName, string tableName);
    }
}
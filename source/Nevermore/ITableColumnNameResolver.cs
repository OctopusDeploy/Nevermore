namespace Nevermore
{
    public interface ITableColumnNameResolver
    {
        string[] GetColumnNames(string schemaName, string tableName);
    }
}
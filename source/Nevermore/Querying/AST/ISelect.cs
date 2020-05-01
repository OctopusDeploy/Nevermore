namespace Nevermore.Querying.AST
{
    public interface ISelect
    {
        string Schema { get; }
        string GenerateSql();
    }
}
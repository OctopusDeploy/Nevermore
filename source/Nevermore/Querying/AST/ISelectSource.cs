namespace Nevermore.Querying.AST
{
    public interface ISelectSource
    {
        string Schema { get; }
        string GenerateSql();
    }

    public interface IAliasedSelectSource : ISelectSource
    {
        string Alias { get; }
    }
}
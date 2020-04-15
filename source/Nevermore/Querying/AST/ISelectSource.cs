namespace Nevermore.Querying.AST
{
    public interface ISelectSource
    {
        string GenerateSql();
    }

    public interface IAliasedSelectSource : ISelectSource
    {
        string Alias { get; }
    }
}
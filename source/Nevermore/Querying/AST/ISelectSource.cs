namespace Nevermore.Querying.AST
{
    public interface ISelectSource : IExpression
    {
        string Schema { get; }
    }

    public interface IAliasedSelectSource : ISelectSource
    {
        string Alias { get; }
    }
}
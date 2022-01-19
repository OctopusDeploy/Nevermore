namespace Nevermore.Querying.AST
{
    public interface ISelect : IExpression
    {
        string Schema { get; }
    }
}
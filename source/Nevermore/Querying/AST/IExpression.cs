namespace Nevermore.Querying.AST
{
    public interface IExpression
    {
        string GenerateSql();
    }
}
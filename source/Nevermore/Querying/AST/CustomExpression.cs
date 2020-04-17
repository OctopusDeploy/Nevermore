namespace Nevermore.Querying.AST
{
    public class CustomExpression : IExpression
    {
        readonly string customExpression;

        public CustomExpression(string customExpression)
        {
            this.customExpression = customExpression;
        }

        public string GenerateSql()
        {
            return customExpression;
        }
    }
}
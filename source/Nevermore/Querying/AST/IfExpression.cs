namespace Nevermore.Querying.AST
{
    public class IfExpression : IExpression
    {
        readonly IExpression predicate;
        readonly ISelect trueValue;
        readonly ISelect falseValue;

        public IfExpression(IExpression predicate, ISelect trueValue, ISelect falseValue)
        {
            this.predicate = predicate;
            this.trueValue = trueValue;
            this.falseValue = falseValue;
        }

        public string GenerateSql()
        {
            return $@"IF {predicate.GenerateSql()}
    {trueValue.GenerateSql()}
ELSE
    {falseValue.GenerateSql()}";
        }
    }
}
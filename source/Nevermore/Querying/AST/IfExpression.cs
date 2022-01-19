namespace Nevermore.Querying.AST
{
    public class IfExpression : IExpression
    {
        readonly IExpression predicate;
        readonly ISelect trueValue;
        readonly ISelect falseValue;
        readonly IOption option;

        public IfExpression(IExpression predicate, ISelect trueValue, ISelect falseValue, IOption option)
        {
            this.predicate = predicate;
            this.trueValue = trueValue;
            this.falseValue = falseValue;
            this.option = option;
        }

        public string GenerateSql()
        {
            return $@"IF {predicate.GenerateSql()}
    {trueValue.GenerateSql()}
ELSE
    {falseValue.GenerateSql()}{option.GenerateSql()}";
        }
    }
}
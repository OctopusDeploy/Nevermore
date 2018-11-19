namespace Nevermore.AST
{
    public class ExistsExpression : IExpression
    {
        readonly ISelect conditionSelect;

        public ExistsExpression(ISelect conditionSelect)
        {
            this.conditionSelect = conditionSelect;
        }

        public string GenerateSql()
        {
            return $"EXISTS({conditionSelect.GenerateSql()})";
        }
    }
}
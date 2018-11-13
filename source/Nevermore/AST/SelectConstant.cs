namespace Nevermore.AST
{
    public class SelectConstant : ISelect 
    {
        readonly Parameter parameter;

        public SelectConstant(Parameter parameter)
        {
            this.parameter = parameter;
        }

        public string GenerateSql()
        {
            return $"SELECT {parameter.ParameterName}";
        }
    }
}
namespace Nevermore.Querying.AST
{
    public class SelectConstant : ISelect 
    {
        readonly Parameter parameter;

        public SelectConstant(Parameter parameter)
        {
            this.parameter = parameter;
        }

        public string Schema => null;

        public string GenerateSql()
        {
            return $"SELECT @{parameter.ParameterName}";
        }
    }
}
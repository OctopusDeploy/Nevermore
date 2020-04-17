namespace Nevermore
{
    public class UnaryWhereParameter
    {
        readonly Parameter parameter;

        public UnaryWhereParameter(string fieldName, UnarySqlOperand operand, Parameter parameter)
        {
            this.parameter = parameter;
            FieldName = fieldName;
            Operand = operand;
        }

        public string FieldName { get; }
        public UnarySqlOperand Operand { get; }
        public string ParameterName => parameter.ParameterName;
    }
}

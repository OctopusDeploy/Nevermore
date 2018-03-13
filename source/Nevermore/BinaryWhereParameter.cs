using Nevermore.AST;

namespace Nevermore
{
    public class BinaryWhereParameter
    {
        readonly Parameter firstParameter;
        readonly Parameter secondParameter;

        public BinaryWhereParameter(string fieldName, BinarySqlOperand operand, Parameter firstParameter, Parameter secondParameter)
        {
            this.firstParameter = firstParameter;
            this.secondParameter = secondParameter;
            FieldName = fieldName;
            Operand = operand;
        }

        public string FieldName { get; }
        public BinarySqlOperand Operand { get; }
        public string FirstParameterName => firstParameter.ParameterName;
        public string SecondParameterName => secondParameter.ParameterName;
    }
}
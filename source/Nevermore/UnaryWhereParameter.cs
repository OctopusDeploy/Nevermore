using System.Collections.Generic;
using System.Linq;
using Nevermore.AST;

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

    public class ArrayWhereParameter
    {
        readonly IReadOnlyList<Parameter> parameterNames;

        public ArrayWhereParameter(string fieldName, ArraySqlOperand operand, IReadOnlyList<Parameter> parameterNames)
        {
            this.parameterNames = parameterNames;
            FieldName = fieldName;
            Operand = operand;
        }

        public string FieldName { get; }
        public ArraySqlOperand Operand { get; }
        public IEnumerable<string> ParameterNames => parameterNames.Select(p => p.ParameterName);
    }
}

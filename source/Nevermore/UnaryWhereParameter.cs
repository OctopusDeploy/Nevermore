using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore
{
    public class UnaryWhereParameter
    {
        readonly string parameterName;

        public UnaryWhereParameter(string fieldName, UnarySqlOperand operand, string parameterName)
        {
            this.parameterName = parameterName;
            FieldName = fieldName;
            Operand = operand;
        }

        public string FieldName { get; }
        public UnarySqlOperand Operand { get; }
        public virtual string ParameterName => parameterName.ToLower();
    }

    public class BinaryWhereParameter
    {
        readonly string firstParameterName;
        readonly string secondParameterName;

        public BinaryWhereParameter(string fieldName, BinarySqlOperand operand, string firstParameterName, string secondParameterName)
        {
            this.firstParameterName = firstParameterName;
            this.secondParameterName = secondParameterName;
            FieldName = fieldName;
            Operand = operand;
        }

        public string FieldName { get; }
        public BinarySqlOperand Operand { get; }
        public string FirstParameterName => firstParameterName.ToLower();
        public string SecondParameterName => secondParameterName.ToLower();
    }

    public class ArrayWhereParameter
    {
        readonly IReadOnlyList<string> parameterNames;

        public ArrayWhereParameter(string fieldName, ArraySqlOperand operand, IReadOnlyList<string> parameterNames)
        {
            this.parameterNames = parameterNames;
            FieldName = fieldName;
            Operand = operand;
        }

        public string FieldName { get; }
        public ArraySqlOperand Operand { get; }

        public IEnumerable<string> ParameterNames => parameterNames;
    }
}

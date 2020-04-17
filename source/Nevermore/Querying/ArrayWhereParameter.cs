using System.Collections.Generic;
using System.Linq;

namespace Nevermore.Querying
{
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
using Nevermore.Querying.AST;

namespace Nevermore.Querying
{
    public class SubQueryWhereParameter
    {
        public string FieldName { get; }
        public ArraySqlOperand Operand { get; }
        public ISelect SubQuery { get; }

        public SubQueryWhereParameter(string fieldName, ArraySqlOperand operand, ISelect subQuery)
        {
            FieldName = fieldName;
            Operand = operand;
            SubQuery = subQuery;
        }
    }
}
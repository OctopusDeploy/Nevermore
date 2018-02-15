using System;

namespace Nevermore.Joins
{
    public enum JoinOperand
    {
        Equal
    }

    //public class JoinClause
    //{
    //    readonly string leftField;
    //    readonly string rightField;
    //    readonly JoinOperand operand;

    //    public JoinClause(string leftField, JoinOperand operand, string rightField)
    //    {
    //        this.leftField = leftField;
    //        this.rightField = rightField;
    //        this.operand = operand;
    //    }

    //    string GetQueryOperand()
    //    {
    //        switch (operand)
    //        {
    //            case JoinOperand.Equal:
    //                return "=";
    //            default:
    //                throw new NotSupportedException("Operand " + operand + " is not supported!");
    //        }
    //    }

    //    public override string ToString()
    //    {
    //        var queryOperand = GetQueryOperand();
    //        return $"{leftField} {queryOperand} {rightField}";
    //    }

    //    public string ToString(string leftTableAlias, string rightTableAlias)
    //    {
    //        var queryOperand = GetQueryOperand();
    //        return $"{leftTableAlias}.[{leftField}] {queryOperand} {rightTableAlias}.[{rightField}]";
    //    }
    //}
}
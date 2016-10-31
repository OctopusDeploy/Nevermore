using System.Collections.Generic;

namespace Nevermore.Joins
{
    public interface IJoin
    {
        IQueryGenerator RightQuery { get; }
        ICollection<JoinClause> JoinClauses { get; }
        JoinType JoinType { get; }
        Join On(string leftField, JoinOperand operand, string rightField);
        Join On(JoinClause joinClause);
    }

    public enum JoinType
    {
        InnerJoin
    }
}
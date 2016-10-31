using System.Collections.Generic;

namespace Nevermore.Joins
{
    public class Join : IJoin
    {
        public IQueryGenerator RightQuery { get; }
        public ICollection<JoinClause> JoinClauses { get; } = new HashSet<JoinClause>();
        public JoinType JoinType { get; private set; }

        public Join(JoinType joinType, IQueryGenerator rightQuery)
        {
            JoinType = joinType;
            RightQuery = rightQuery;
        }

        public Join On(string leftField, JoinOperand operand, string rightField)
        {
            var joinClause = new JoinClause(leftField, operand, rightField);
            return On(joinClause);
        }

        public Join On(JoinClause joinClause)
        {
            JoinClauses.Add(joinClause);
            return this;
        }
    }
}
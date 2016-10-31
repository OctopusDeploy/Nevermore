using System.Linq;

namespace Nevermore.Joins
{
    public static class JoinExtensions
    {
        public static IQueryBuilder<TLeft> InnerJoin<TLeft, TRight>(this IQueryBuilder<TLeft> left, IQueryBuilder<TRight> right)
            where TLeft : class where TRight : class
        {            
            var join = new Join(JoinType.InnerJoin, right.QueryGenerator);
            left.Join(join);

            return left;
        }

        public static IQueryBuilder<TLeft> On<TLeft>(this IQueryBuilder<TLeft> left, string leftField, JoinOperand operand, string rightField)
            where TLeft : class
        {
            var join = left.QueryGenerator.Joins.Last();
            join.On(leftField, operand, rightField);

            return left;
        } 
    }
}
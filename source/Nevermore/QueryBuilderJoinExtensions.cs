using Nevermore.AST;

namespace Nevermore
{
    public static class QueryBuilderJoinExtensions
    {
        public static IJoinSourceQueryBuilder<TRecord> InnerJoin<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            IAliasedSelectSource source)
        {
            return queryBuilder.Join(source, JoinType.InnerJoin);
        }

        public static IJoinSourceQueryBuilder<TRecord> LeftHashJoin<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            IAliasedSelectSource source)
        {
            return queryBuilder.Join(source, JoinType.LeftHashJoin);
        }
    }
}
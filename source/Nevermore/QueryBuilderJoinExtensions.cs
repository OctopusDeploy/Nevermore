using Nevermore.AST;

namespace Nevermore
{
    public static class QueryBuilderJoinExtensions
    {
        public static IJoinSourceQueryBuilder<TRecord> InnerJoin<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            ITableSourceQueryBuilder<TRecord> rightHandQueryBuilder) where TRecord : class
        {
            return queryBuilder.Join(rightHandQueryBuilder.AsAliasedSource(), JoinType.InnerJoin, rightHandQueryBuilder.ParameterValues, rightHandQueryBuilder.Parameters, rightHandQueryBuilder.ParameterDefaults);
        }

        public static IJoinSourceQueryBuilder<TRecord> InnerJoin<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            ISubquerySourceBuilder<TRecord> rightHandQueryBuilder) where TRecord : class
        {
            return queryBuilder.Join(rightHandQueryBuilder.AsSource(), JoinType.InnerJoin, rightHandQueryBuilder.ParameterValues, rightHandQueryBuilder.Parameters, rightHandQueryBuilder.ParameterDefaults);
        }

        public static IJoinSourceQueryBuilder<TRecord> LeftHashJoin<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            IAliasedSelectSource source) where TRecord : class
        {
            return queryBuilder.Join(source, JoinType.LeftHashJoin, queryBuilder.ParameterValues, queryBuilder.Parameters, queryBuilder.ParameterDefaults);
        }
    }
}
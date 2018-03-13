using Nevermore.AST;

namespace Nevermore
{
    public static class QueryBuilderJoinExtensions
    {
        /// <summary>
        /// Adds an inner join to the query.
        /// The query that has been built up so far in the left hand side query builder may be converted to a subquery to capture more complex modifications, such as where clauses.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder which represents the left hand side of the join</param>
        /// <param name="rightHandQueryBuilder">The query builder which represents the right hand side of the join</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IJoinSourceQueryBuilder<TRecord> InnerJoin<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            ITableSourceQueryBuilder<TRecord> rightHandQueryBuilder) where TRecord : class
        {
            return queryBuilder.Join(rightHandQueryBuilder.AsAliasedSource(), JoinType.InnerJoin, rightHandQueryBuilder.ParameterValues, rightHandQueryBuilder.Parameters, rightHandQueryBuilder.ParameterDefaults);
        }

        /// <summary>
        /// Adds an inner join to the query.
        /// The query that has been built up so far in the left hand side query builder may be converted to a subquery to capture more complex modifications, such as where clauses.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder which represents the left hand side of the join</param>
        /// <param name="rightHandQueryBuilder">The query builder which represents the right hand side of the join</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IJoinSourceQueryBuilder<TRecord> InnerJoin<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            ISubquerySourceBuilder<TRecord> rightHandQueryBuilder) where TRecord : class
        {
            return queryBuilder.Join(rightHandQueryBuilder.AsSource(), JoinType.InnerJoin, rightHandQueryBuilder.ParameterValues, rightHandQueryBuilder.Parameters, rightHandQueryBuilder.ParameterDefaults);
        }

        /// <summary>
        /// Adds a left hash join to the query.
        /// The query that has been built up so far in the left hand side query builder may be converted to a subquery to capture more complex modifications, such as where clauses.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder which represents the left hand side of the join</param>
        /// <param name="source">The source which represents the right hand side of the join</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IJoinSourceQueryBuilder<TRecord> LeftHashJoin<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            IAliasedSelectSource source) where TRecord : class
        {
            return queryBuilder.Join(source, JoinType.LeftHashJoin, queryBuilder.ParameterValues, queryBuilder.Parameters, queryBuilder.ParameterDefaults);
        }
    }
}
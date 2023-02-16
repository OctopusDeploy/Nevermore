using Nevermore.Querying.AST;

namespace Nevermore
{
    public static class QueryBuilderJoinExtensions
    {
        /// <summary>
        /// Adds an inner join to the query.
        /// The query that has been built up so far in the left hand side query builder may be converted to a subquery to capture more complex modifications, such as where clauses.
        /// </summary>
        /// <typeparam name="TRecordLeft">The record type of the left hand side query builder</typeparam>
        /// <typeparam name="TRecordRight">The record type of the right hand side query builder</typeparam>
        /// <param name="queryBuilder">The query builder which represents the left hand side of the join</param>
        /// <param name="rightHandQueryBuilder">The query builder which represents the right hand side of the join</param>
        /// <param name="joinAlias">The alias to use for the left hand side</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IJoinSourceQueryBuilder<TRecordLeft> InnerJoin<TRecordLeft, TRecordRight>(this IQueryBuilder<TRecordLeft> queryBuilder,
            ITableSourceQueryBuilder<TRecordRight> rightHandQueryBuilder, 
            string joinAlias = null) 
            where TRecordLeft : class 
            where TRecordRight : class
        {
            return queryBuilder.Join(rightHandQueryBuilder.AsAliasedSource(), JoinType.InnerJoin, rightHandQueryBuilder.ParameterValues, rightHandQueryBuilder.Parameters, rightHandQueryBuilder.ParameterDefaults, joinAlias);
        }

        /// <summary>
        /// Adds an inner join to the query.
        /// The query that has been built up so far in the left hand side query builder may be converted to a subquery to capture more complex modifications, such as where clauses.
        /// </summary>
        /// <typeparam name="TRecordLeft">The record type of the left hand side query builder</typeparam>
        /// <typeparam name="TRecordRight">The record type of the right hand side query builder</typeparam>
        /// <param name="queryBuilder">The query builder which represents the left hand side of the join</param>
        /// <param name="rightHandQueryBuilder">The query builder which represents the right hand side of the join</param>
        /// <param name="joinAlias">The alias to use for the left hand side</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IJoinSourceQueryBuilder<TRecordLeft> InnerJoin<TRecordLeft, TRecordRight>(this IQueryBuilder<TRecordLeft> queryBuilder,
            ISubquerySourceBuilder<TRecordRight> rightHandQueryBuilder, 
            string joinAlias = null) 
            where TRecordLeft : class 
            where TRecordRight : class
        {
            return queryBuilder.Join(rightHandQueryBuilder.AsSource(), JoinType.InnerJoin, rightHandQueryBuilder.ParameterValues, rightHandQueryBuilder.Parameters, rightHandQueryBuilder.ParameterDefaults, joinAlias);
        }

        /// <summary>
        /// Adds a left hash join to the query.
        /// The query that has been built up so far in the left hand side query builder may be converted to a subquery to capture more complex modifications, such as where clauses.
        /// </summary>
        /// <typeparam name="TRecordLeft">The record type of the left hand side query builder</typeparam>
        /// <typeparam name="TRecordRight">The record type of the right hand side query builder</typeparam>
        /// <param name="queryBuilder">The query builder which represents the left hand side of the join</param>
        /// <param name="rightHandQueryBuilder">The query builder which represents the right hand side of the join</param>
        /// <param name="joinAlias">The alias to use for the left hand side</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IJoinSourceQueryBuilder<TRecordLeft> LeftHashJoin<TRecordLeft, TRecordRight>(this IQueryBuilder<TRecordLeft> queryBuilder,
            ITableSourceQueryBuilder<TRecordRight> rightHandQueryBuilder, 
            string joinAlias = null)
            where TRecordLeft : class
            where TRecordRight : class
        {
            return queryBuilder.Join(rightHandQueryBuilder.AsAliasedSource(), JoinType.LeftHashJoin, rightHandQueryBuilder.ParameterValues, rightHandQueryBuilder.Parameters, rightHandQueryBuilder.ParameterDefaults, joinAlias);
        }

        /// <summary>
        /// Adds a left hash join to the query.
        /// The query that has been built up so far in the left hand side query builder may be converted to a subquery to capture more complex modifications, such as where clauses.
        /// </summary>
        /// <typeparam name="TRecordLeft">The record type of the left hand side query builder</typeparam>
        /// <typeparam name="TRecordRight">The record type of the right hand side query builder</typeparam>
        /// <param name="queryBuilder">The query builder which represents the left hand side of the join</param>
        /// <param name="rightHandQueryBuilder">The query builder which represents the right hand side of the join</param>
        /// <param name="joinAlias">The alias to use for the left hand side</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IJoinSourceQueryBuilder<TRecordLeft> LeftHashJoin<TRecordLeft, TRecordRight>(this IQueryBuilder<TRecordLeft> queryBuilder,
            ISubquerySourceBuilder<TRecordRight> rightHandQueryBuilder, 
            string joinAlias = null)
            where TRecordLeft : class
            where TRecordRight : class
        {
            return queryBuilder.Join(rightHandQueryBuilder.AsSource(), JoinType.LeftHashJoin, rightHandQueryBuilder.ParameterValues, rightHandQueryBuilder.Parameters, rightHandQueryBuilder.ParameterDefaults, joinAlias);
        }
        
        /// <summary>
        /// Adds an cross join to the query.
        /// The query that has been built up so far in the left hand side query builder may be converted to a subquery to capture more complex modifications, such as where clauses.
        /// </summary>
        /// <typeparam name="TRecordLeft">The record type of the left hand side query builder</typeparam>
        /// <typeparam name="TRecordRight">The record type of the right hand side query builder</typeparam>
        /// <param name="queryBuilder">The query builder which represents the left hand side of the join</param>
        /// <param name="rightHandQueryBuilder">The query builder which represents the right hand side of the join</param>
        /// <param name="joinAlias">The alias to use for the left hand side</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IQueryBuilder<TRecordLeft> CrossJoin<TRecordLeft, TRecordRight>(this IQueryBuilder<TRecordLeft> queryBuilder,
            ITableSourceQueryBuilder<TRecordRight> rightHandQueryBuilder, 
            string joinAlias = null) 
            where TRecordLeft : class 
            where TRecordRight : class
        {
            return queryBuilder.Join(rightHandQueryBuilder.AsAliasedSource(), JoinType.CrossJoin, rightHandQueryBuilder.ParameterValues, rightHandQueryBuilder.Parameters, rightHandQueryBuilder.ParameterDefaults, joinAlias);
        }
        
        /// <summary>
        /// Adds an cross join to the query.
        /// The query that has been built up so far in the left hand side query builder may be converted to a subquery to capture more complex modifications, such as where clauses.
        /// </summary>
        /// <typeparam name="TRecordLeft">The record type of the left hand side query builder</typeparam>
        /// <typeparam name="TRecordRight">The record type of the right hand side query builder</typeparam>
        /// <param name="queryBuilder">The query builder which represents the left hand side of the join</param>
        /// <param name="rightHandQueryBuilder">The query builder which represents the right hand side of the join</param>
        /// <param name="joinAlias">The alias to use for the left hand side</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IQueryBuilder<TRecordLeft> CrossJoin<TRecordLeft, TRecordRight>(this IQueryBuilder<TRecordLeft> queryBuilder,
            ISubquerySourceBuilder<TRecordRight> rightHandQueryBuilder, 
            string joinAlias = null) 
            where TRecordLeft : class 
            where TRecordRight : class
        {
            return queryBuilder.Join(rightHandQueryBuilder.AsSource(), JoinType.CrossJoin, rightHandQueryBuilder.ParameterValues, rightHandQueryBuilder.Parameters, rightHandQueryBuilder.ParameterDefaults, joinAlias);
        }
    }
}
using System;
using System.Linq.Expressions;

namespace Nevermore
{
    public static class QueryBuilderOrderByExtensions
    {
        /// <summary>
        /// Adds an order by clause to the query, where the order by clause will be in the default order (ascending).
        /// If no order by clauses are added to the query, the query will be ordered by the Id column in ascending order.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="expression">An expression to return a column from a record. This will be used to determine the name of the column that the query should be ordered by</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IOrderedQueryBuilder<TRecord> OrderBy<TRecord>(this IQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, object>> expression)
            where TRecord : class => queryBuilder.OrderBy(GetColumnName(expression.Body));

        /// <summary>
        /// Adds an order by clause to the query, where the order by clause will be in descending order.
        /// If no order by clauses are explicitly added to the query, the query will be ordered by the Id column in ascending order.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="expression">An expression to return a column from a record. This will be used to determine the name of the column that the query should be ordered by</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IOrderedQueryBuilder<TRecord> OrderByDescending<TRecord>(this IQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, object>> expression) 
            where TRecord : class => queryBuilder.OrderByDescending(GetColumnName(expression.Body));

        /// <summary>
        /// Adds an order by clause to the query, where the order by clause will be in the default order (ascending).
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="expression">An expression to return a column from a record. This will be used to determine the name of the column that the query should be ordered by</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IOrderedQueryBuilder<TRecord> ThenBy<TRecord>(this IOrderedQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, object>> expression) 
            where TRecord : class => queryBuilder.ThenBy(GetColumnName(expression.Body));

        /// <summary>
        /// Adds an order by clause to the query, where the order by clause will be in descending order.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="expression">An expression to return a column from a record. This will be used to determine the name of the column that the query should be ordered by</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IOrderedQueryBuilder<TRecord> ThenByDescending<TRecord>(this IOrderedQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, object>> expression) 
            where TRecord : class => queryBuilder.ThenByDescending(GetColumnName(expression.Body));
        
        static string GetColumnName(Expression expression)
        {
            if (expression is UnaryExpression uexpr)
                expression = uexpr.Operand;

            if (expression is MemberExpression me)
                return me.Member.Name;
            
            throw new NotSupportedException($"Expressions of type {expression.GetType()} are not supported for OrderBy and ThenBy");
        }
    }
}
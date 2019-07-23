using System;
using System.Linq.Expressions;

namespace Nevermore
{
    public static class QueryBuilderColumnExtensions
    {
        /// <summary>
        /// Adds a column to the column selection for the query.
        /// If no columns are explicitly added to the column selection for the query, all columns will be selected.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="expression">An expression to return the column from a record. This will be used to determine the name of the column</param>        
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IQueryBuilder<TRecord> Column<TRecord>(this IQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, object>> expression) where TRecord : class
        {
            return queryBuilder.Column(GetColumnNameFromExpression(expression));
        }

        /// <summary>
        /// Adds a column to the column selection for the query, and aliases the column.
        /// If no columns are explicitly added to the column selection for the query, all columns will be selected.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="expression">An expression to return the column from a record. This will be used to determine the name of the column</param>
        /// <param name="columnAlias">The alias to use for this column</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IQueryBuilder<TRecord> Column<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            Expression<Func<TRecord, object>> expression, string columnAlias) where TRecord : class
        {
            return queryBuilder.Column(GetColumnNameFromExpression(expression), columnAlias);
        }

        /// <summary>
        /// Adds a column to the column selection for the query from a specific table that has been aliased in the query, and then aliases the column.
        /// If no columns are explicitly added to the column selection for the query, all columns will be selected.
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <param name="expression">An expression to return the column from a record. This will be used to determine the name of the column</param>
        /// <param name="columnAlias">The alias to use for this column</param>
        /// <param name="tableAlias">The alias of the table from which the column originates</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        public static IQueryBuilder<TRecord> Column<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            Expression<Func<TRecord, object>> expression, string columnAlias, string tableAlias) where TRecord : class
        {
            return queryBuilder.Column(GetColumnNameFromExpression(expression), columnAlias, tableAlias);
        }
        
        static string GetColumnNameFromExpression<TRecord>(Expression<Func<TRecord, object>> expression)
            where TRecord : class
        {
            switch (expression.Body)
            {
                case MemberExpression memberExpression:
                    return memberExpression.Member.Name;

                // It appears as if C# modifies the expression to wrap it in a `Convert` for some types, so handle this with the UnaryExpression case
                case UnaryExpression unaryExpression:
                    if (unaryExpression.Operand is MemberExpression unaryMemberExpression)
                    {
                        return unaryMemberExpression.Member.Name;
                    }

                    break;
            }

            throw new NotSupportedException(
                $"The predicate supplied is not supported. Only simple MemberExpressions are supported. The predicate is a {expression.Body.GetType()}.");
        }
    }
}
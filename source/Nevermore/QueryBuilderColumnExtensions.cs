using System;
using System.Linq.Expressions;

namespace Nevermore
{
    public static class QueryBuilderColumnExtensions
    {
        public static IQueryBuilder<TRecord> Column<TRecord>(this IQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, object>> expression) where TRecord : class
        {
            return queryBuilder.Column(GetColumnNameFromExpression(expression));
        }

        public static IQueryBuilder<TRecord> Column<TRecord>(this IQueryBuilder<TRecord> queryBuilder,
            Expression<Func<TRecord, object>> expression, string columnAlias) where TRecord : class
        {
            return queryBuilder.Column(GetColumnNameFromExpression(expression), columnAlias);
        }

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
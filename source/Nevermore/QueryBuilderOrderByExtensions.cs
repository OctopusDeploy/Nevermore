using System;
using System.Linq.Expressions;

namespace Nevermore
{
    public static class QueryBuilderOrderByExtensions
    {
        public static IOrderedQueryBuilder<TRecord> OrderBy<TRecord>(this IQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, object>> expression)
            where TRecord : class
            => queryBuilder.OrderBy(GetColumnName(expression.Body));

        public static IOrderedQueryBuilder<TRecord> OrderByDescending<TRecord>(this IQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, object>> expression)
            where TRecord : class
            => queryBuilder.OrderByDescending(GetColumnName(expression.Body));

        public static IOrderedQueryBuilder<TRecord> ThenBy<TRecord>(this IOrderedQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, object>> expression)
            where TRecord : class
            => queryBuilder.ThenBy(GetColumnName(expression.Body));

        public static IOrderedQueryBuilder<TRecord> ThenByDescending<TRecord>(this IOrderedQueryBuilder<TRecord> queryBuilder, Expression<Func<TRecord, object>> expression)
            where TRecord : class
            => queryBuilder.ThenByDescending(GetColumnName(expression.Body));
        
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
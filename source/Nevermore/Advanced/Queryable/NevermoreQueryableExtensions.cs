using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace Nevermore.Advanced.Queryable
{
    public static class NevermoreQueryableExtensions
    {
        static readonly MethodInfo WhereCustomMethodInfo = new Func<IQueryable<object>, string, IQueryable<object>>(WhereCustom).GetMethodInfo().GetGenericMethodDefinition();

        public static IQueryable<TSource> WhereCustom<TSource>(this IQueryable<TSource> source, string whereClause)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(whereClause))
                throw new ArgumentNullException(nameof(whereClause));

            return source.Provider.CreateQuery<TSource>(
                Expression.Call(
                    null,
                    WhereCustomMethodInfo.MakeGenericMethod(typeof(TSource)),
                    source.Expression, Expression.Constant(whereClause)));
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Advanced.Queryable
{
    public static class NevermoreQueryableExtensions
    {
        static readonly MethodInfo WhereCustomMethodInfo = new Func<IQueryable<object>, string, IQueryable<object>>(WhereCustom).GetMethodInfo().GetGenericMethodDefinition();
        static readonly MethodInfo FirstOrDefaultMethodInfo = new Func<IQueryable<object>, object>(System.Linq.Queryable.FirstOrDefault).GetMethodInfo().GetGenericMethodDefinition();
        static readonly MethodInfo FirstOrDefaultWithPredicateMethodInfo = new Func<IQueryable<object>, Expression<Func<object, bool>>, object>(System.Linq.Queryable.FirstOrDefault).GetMethodInfo().GetGenericMethodDefinition();
        static readonly MethodInfo CountMethodInfo = new Func<IQueryable<object>, int>(System.Linq.Queryable.Count).GetMethodInfo().GetGenericMethodDefinition();
        static readonly MethodInfo CountWithPredicateMethodInfo = new Func<IQueryable<object>, Expression<Func<object, bool>>, int>(System.Linq.Queryable.Count).GetMethodInfo().GetGenericMethodDefinition();
        static readonly MethodInfo AnyMethodInfo = new Func<IQueryable<object>, bool>(System.Linq.Queryable.Any).GetMethodInfo().GetGenericMethodDefinition();
        static readonly MethodInfo AnyWithPredicateMethodInfo = new Func<IQueryable<object>, Expression<Func<object, bool>>, bool>(System.Linq.Queryable.Any).GetMethodInfo().GetGenericMethodDefinition();

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

        public static async Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source.Provider is IAsyncQueryProvider asyncQueryProvider)
            {
                var expression = Expression.Call(
                    null,
                    CountMethodInfo.MakeGenericMethod(typeof(TSource)),
                    source.Expression);
                return await asyncQueryProvider.ExecuteAsync<int>(expression, cancellationToken);
            }

            throw new InvalidOperationException("The query provider does not support async operations.");
        }

        public static async Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source.Provider is IAsyncQueryProvider asyncQueryProvider)
            {
                var expression = Expression.Call(
                    null,
                    CountWithPredicateMethodInfo.MakeGenericMethod(typeof(TSource)),
                    source.Expression,
                    predicate);
                return await asyncQueryProvider.ExecuteAsync<int>(expression, cancellationToken);
            }

            throw new InvalidOperationException("The query provider does not support async operations.");
        }

        public static async Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source.Provider is IAsyncQueryProvider asyncQueryProvider)
            {
                var expression = Expression.Call(
                    null,
                    AnyMethodInfo.MakeGenericMethod(typeof(TSource)),
                    source.Expression);
                return await asyncQueryProvider.ExecuteAsync<bool>(expression, cancellationToken);
            }

            throw new InvalidOperationException("The query provider does not support async operations.");
        }

        public static async Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source.Provider is IAsyncQueryProvider asyncQueryProvider)
            {
                var expression = Expression.Call(
                    null,
                    AnyWithPredicateMethodInfo.MakeGenericMethod(typeof(TSource)),
                    source.Expression,
                    predicate);
                return await asyncQueryProvider.ExecuteAsync<bool>(expression, cancellationToken);
            }

            throw new InvalidOperationException("The query provider does not support async operations.");
        }

        public static async Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source.Provider is IAsyncQueryProvider asyncQueryProvider)
            {
                var expression = Expression.Call(
                    null,
                    FirstOrDefaultMethodInfo.MakeGenericMethod(typeof(TSource)),
                    source.Expression);
                return await asyncQueryProvider.ExecuteAsync<TSource>(expression, cancellationToken);
            }

            throw new InvalidOperationException("The query provider does not support async operations.");
        }

        public static async Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source.Provider is IAsyncQueryProvider asyncQueryProvider)
            {
                var expression = Expression.Call(
                    null,
                    FirstOrDefaultWithPredicateMethodInfo.MakeGenericMethod(typeof(TSource)),
                    source.Expression,
                    predicate);
                return await asyncQueryProvider.ExecuteAsync<TSource>(expression, cancellationToken);
            }

            throw new InvalidOperationException("The query provider does not support async operations.");
        }

        public static async Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken cancellationToken = default)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source.Provider is IAsyncQueryProvider asyncQueryProvider)
            {
                return new List<TSource>(await asyncQueryProvider.ExecuteAsync<IEnumerable<TSource>>(source.Expression, cancellationToken));
            }

            throw new InvalidOperationException("The query provider does not support async operations.");
        }
    }
}
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Util;

namespace Nevermore.Advanced.Queryable
{
    internal class QueryProvider : IAsyncQueryProvider
    {
        static readonly MethodInfo GenericCreateQueryMethod = typeof(QueryProvider)
            .GetRuntimeMethods().Single(m => m.Name == nameof(CreateQuery) && m.IsGenericMethod);
        static readonly MethodInfo GenericExecuteMethod = typeof(QueryProvider)
            .GetRuntimeMethods().Single(m => m.Name == nameof(Execute) && m.IsGenericMethod);
        static readonly MethodInfo GenericStreamMethod = typeof(IReadQueryExecutor)
            .GetRuntimeMethod(nameof(IReadQueryExecutor.Stream), new[] { typeof(PreparedCommand) });
        static readonly MethodInfo GenericStreamAsyncMethod = typeof(IReadQueryExecutor)
            .GetRuntimeMethod(nameof(IReadQueryExecutor.StreamAsync), new[] { typeof(PreparedCommand), typeof(CancellationToken) });
        static readonly MethodInfo GenericExecuteScalarMethod = typeof(IReadQueryExecutor)
            .GetRuntimeMethod(nameof(IReadQueryExecutor.ExecuteScalar), new[] { typeof(PreparedCommand) });
        static readonly MethodInfo GenericExecuteScalarAsyncMethod = typeof(IReadQueryExecutor)
            .GetRuntimeMethod(nameof(IReadQueryExecutor.ExecuteScalarAsync), new[] { typeof(PreparedCommand), typeof(CancellationToken) });

        readonly IReadQueryExecutor queryExecutor;
        readonly IRelationalStoreConfiguration configuration;

        public QueryProvider(IReadQueryExecutor queryExecutor, IRelationalStoreConfiguration configuration)
        {
            this.queryExecutor = queryExecutor;
            this.configuration = configuration;
        }

        public IQueryable CreateQuery(Expression expression) =>
            (IQueryable)GenericCreateQueryMethod
                .MakeGenericMethod(expression.Type.GetSequenceType())
                .Invoke(this, new object[] { expression });

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
            => new Query<TElement>(new QueryProvider(queryExecutor, configuration), expression);

        public object Execute(Expression expression)
        {
            return GenericExecuteMethod.MakeGenericMethod(expression.Type)
                .Invoke(this, new object[] { expression });
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var (command, queryType) = Translate(expression);

            if (queryType == QueryType.SelectMany)
            {
                var sequenceType = expression.Type.GetSequenceType();
                return (TResult)GenericStreamMethod.MakeGenericMethod(sequenceType)
                    .Invoke(queryExecutor, new object[] { command });
            }

            if (queryType == QueryType.Count || queryType == QueryType.Exists)
            {
                return (TResult)GenericExecuteScalarMethod.MakeGenericMethod(expression.Type)
                    .Invoke(queryExecutor, new object[] { command });
            }

            var stream = (IEnumerable)GenericStreamMethod.MakeGenericMethod(expression.Type)
                .Invoke(queryExecutor, new object[] { command });

            return queryType switch
            {
                QueryType.SelectFirst => (TResult)stream.Cast<object>().First(),
                QueryType.SelectFirstOrDefault => (TResult)stream.Cast<object>().FirstOrDefault(),
                QueryType.SelectSingle => (TResult)stream.Cast<object>().Single(),
                QueryType.SelectSingleOrDefault => (TResult)stream.Cast<object>().SingleOrDefault(),
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var (command, queryType) = Translate(expression);

            if (queryType == QueryType.SelectMany)
            {
                var sequenceType = expression.Type.GetSequenceType();
                var asyncStream = (IAsyncEnumerable<object>)GenericStreamAsyncMethod.MakeGenericMethod(sequenceType)
                    .Invoke(queryExecutor, new object[] { command, cancellationToken });

                return (TResult)await CreateList(asyncStream, sequenceType).ConfigureAwait(false);
            }

            if (queryType == QueryType.Count || queryType == QueryType.Exists)
            {
                return await ((Task<TResult>)GenericExecuteScalarAsyncMethod.MakeGenericMethod(expression.Type)
                        .Invoke(queryExecutor, new object[] { command, cancellationToken }))
                    .ConfigureAwait(false);
            }

            var stream = (IAsyncEnumerable<object>)GenericStreamAsyncMethod.MakeGenericMethod(expression.Type)
                .Invoke(queryExecutor, new object[] { command, cancellationToken });

            object result = queryType switch
            {
                QueryType.SelectFirst => (TResult)await FirstAsync(stream, cancellationToken).ConfigureAwait(false),
                QueryType.SelectFirstOrDefault => (TResult)await FirstOrDefaultAsync(stream, cancellationToken).ConfigureAwait(false),
                QueryType.SelectSingle => (TResult)await SingleAsync(stream, cancellationToken).ConfigureAwait(false),
                QueryType.SelectSingleOrDefault => (TResult)await SingleOrDefaultAsync(stream, cancellationToken).ConfigureAwait(false),
                _ => throw new ArgumentOutOfRangeException()
            };

            if (result is not null) return (TResult) result;

            // TODO: This NEEDS to go away when we turn nullable on in Nevermore
            // This method needs to be able to return null for instances like `FirstOrDefaultAsync`
            object GetNull() => null;
            return (TResult) GetNull();
        }

        public IAsyncEnumerable<TResult> StreamAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var (command, queryType) = Translate(expression);
            if (queryType is not QueryType.SelectMany)
            {
                throw new InvalidOperationException("Cannot stream the results of a query that selects only a single result.");
            }

            var sequenceType = expression.Type.GetSequenceType();
            return (IAsyncEnumerable<TResult>)GenericStreamAsyncMethod.MakeGenericMethod(sequenceType)
                .Invoke(queryExecutor, new object[] { command, cancellationToken });
        }

        public (PreparedCommand, QueryType) Translate(Expression expression)
        {
            return new QueryTranslator(configuration).Translate(expression);
        }

        static async Task<IList> CreateList(IAsyncEnumerable<object> items, Type elementType)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            IList list = (IList)Activator.CreateInstance(listType);
            await foreach (var item in items.ConfigureAwait(false))
            {
                list.Add(item);
            }

            return list;
        }

        static async ValueTask<T> FirstOrDefaultAsync<T>(IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken)
        {
#pragma warning disable CA2007
            // CA2007 doesn't understand ConfiguredCancelableAsyncEnumerable and incorrectly thinks we need another ConfigureAwait(false) here
            await using var enumerator = enumerable.ConfigureAwait(false).WithCancellation(cancellationToken).GetAsyncEnumerator();
#pragma warning restore CA2007

            if (await enumerator.MoveNextAsync())
            {
                return enumerator.Current;
            }

            return default;
        }

        static async ValueTask<T> FirstAsync<T>(IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken)
        {
#pragma warning disable CA2007
            // CA2007 doesn't understand ConfiguredCancelableAsyncEnumerable and incorrectly thinks we need another ConfigureAwait(false) here
            await using var enumerator = enumerable.ConfigureAwait(false).WithCancellation(cancellationToken).GetAsyncEnumerator();
#pragma warning restore CA2007

            if (!await enumerator.MoveNextAsync())
            {
                throw new InvalidOperationException("Sequence contains no elements.");
            }

            return enumerator.Current;
        }
        
        static async Task<T> SingleOrDefaultAsync<T>(IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken = default)
        {
#pragma warning disable CA2007
            // CA2007 doesn't understand ConfiguredCancelableAsyncEnumerable and incorrectly thinks we need another ConfigureAwait(false) here
            await using var enumerator = enumerable.ConfigureAwait(false).WithCancellation(cancellationToken).GetAsyncEnumerator();
#pragma warning restore CA2007

            if (!await enumerator.MoveNextAsync())
            {
                return default;
            }

            var current = enumerator.Current;

            if (await enumerator.MoveNextAsync())
            {
                throw new InvalidOperationException("Sequence contains more than one element.");
            }

            return current;
        }

        static async Task<T> SingleAsync<T>(IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken = default)
        {
#pragma warning disable CA2007
            // CA2007 doesn't understand ConfiguredCancelableAsyncEnumerable and incorrectly thinks we need another ConfigureAwait(false) here
            await using var enumerator = enumerable.ConfigureAwait(false).WithCancellation(cancellationToken).GetAsyncEnumerator();
#pragma warning restore CA2007

            if (!await enumerator.MoveNextAsync())
            {
                throw new InvalidOperationException("Sequence contains no elements.");
            }

            var current = enumerator.Current;

            if (!await enumerator.MoveNextAsync())
            {
                return current;
            }

            throw new InvalidOperationException("Sequence contains more than one element.");
        }
    }
}
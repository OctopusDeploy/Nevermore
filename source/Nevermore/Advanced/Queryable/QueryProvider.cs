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
        static readonly MethodInfo GenericStreamInternalAsyncMethod = typeof(ReadTransaction)
            .GetRuntimeMethod("StreamInternalAsync", new[] { typeof(PreparedCommand), typeof(CancellationToken) });
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

            if (queryType == QueryType.SelectSingle)
            {
                var stream = (IEnumerable)GenericStreamMethod.MakeGenericMethod(expression.Type)
                    .Invoke(queryExecutor, new object[] { command });
                return (TResult)stream.Cast<object>().FirstOrDefault();
            }

            return (TResult)GenericExecuteScalarMethod.MakeGenericMethod(expression.Type)
                .Invoke(queryExecutor, new object[] { command });
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

            if (queryType == QueryType.SelectSingle)
            {
                if (queryExecutor is ReadTransaction readTransaction && configuration.DocumentMaps.ResolveOptional(typeof(TResult), out var documentMap) && documentMap.ChildTables is { Count: > 0 })
                {
                    var asyncStream = readTransaction.StreamInternalAsync<TResult>(command, cancellationToken);
                    var result = await FirstOrDefaultAsync(asyncStream, cancellationToken).ConfigureAwait(false);

                    if (result is not null) readTransaction.LoadChildTables(result, documentMap);
                    
                    return result;
                }
                else // either not ReadTransaction or doesn't have child Tables
                {
                    var asyncStream = (IAsyncEnumerable<object>)GenericStreamAsyncMethod.MakeGenericMethod(expression.Type)
                        .Invoke(queryExecutor, new object[] { command, cancellationToken });
                    var firstOrDefaultAsync = await FirstOrDefaultAsync(asyncStream, cancellationToken).ConfigureAwait(false);

                    return (TResult) firstOrDefaultAsync;
                }
            }

            return await ((Task<TResult>)GenericExecuteScalarAsyncMethod.MakeGenericMethod(expression.Type)
                    .Invoke(queryExecutor, new object[] { command, cancellationToken }))
                .ConfigureAwait(false);
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
    }
}
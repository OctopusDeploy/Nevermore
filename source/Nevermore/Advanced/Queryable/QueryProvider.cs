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

                return (TResult)await CreateList(asyncStream, sequenceType);
            }

            if (queryType == QueryType.SelectSingle)
            {
                var asyncStream = (IAsyncEnumerable<object>)GenericStreamAsyncMethod.MakeGenericMethod(expression.Type)
                    .Invoke(queryExecutor, new object[] { command, cancellationToken });
                return (TResult)Convert.ChangeType(await FirstOrDefaultAsync(asyncStream, cancellationToken), typeof(TResult));
            }

            return await (Task<TResult>)GenericExecuteScalarAsyncMethod.MakeGenericMethod(expression.Type)
                .Invoke(queryExecutor, new object[] { command, cancellationToken });
        }

        (PreparedCommand, QueryType) Translate(Expression expression)
        {
            return new QueryTranslator(configuration).Translate(expression);
        }

        static async Task<IList> CreateList(IAsyncEnumerable<object> items, Type elementType)
        {
            var listType = typeof(List<>).MakeGenericType(elementType);
            IList list = (IList)Activator.CreateInstance(listType);
            await foreach (var item in items)
            {
                list.Add(item);
            }

            return list;
        }

        static async ValueTask<T> FirstOrDefaultAsync<T>(IAsyncEnumerable<T> enumerable, CancellationToken cancellationToken)
        {
            await using var enumerator = enumerable.ConfigureAwait(false).WithCancellation(cancellationToken).GetAsyncEnumerator();

            if (await enumerator.MoveNextAsync())
            {
                return enumerator.Current;
            }

            return default;
        }
    }
}
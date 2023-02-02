using System;
using System.Collections;
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
            .GetRuntimeMethod(nameof(IReadQueryExecutor.Stream), new[] { typeof(PreparedCommand) } );
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
                return (TResult)ExecuteStream(command, sequenceType);
            }

            if (queryType == QueryType.SelectSingle)
            {
                return (TResult)FirstOrDefault(ExecuteStream(command, expression.Type));
            }

            return (TResult)ExecuteScalar(command, expression.Type);
        }

        public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var (command, queryType) = Translate(expression);
            if (queryType == QueryType.SelectMany)
            {
                var sequenceType = expression.Type.GetSequenceType();
                return (TResult)await ExecuteStreamAsync(command, sequenceType, cancellationToken).ConfigureAwait(false);
            }

            if (queryType == QueryType.SelectSingle)
            {
                return (TResult)FirstOrDefault(await ExecuteStreamAsync(command, expression.Type, cancellationToken).ConfigureAwait(false));
            }

            return await ((Task<TResult>)GenericExecuteScalarAsyncMethod.MakeGenericMethod(expression.Type)
                    .Invoke(queryExecutor, new object[] { command, cancellationToken }))
                .ConfigureAwait(false);
        }

        public (PreparedCommand, QueryType) Translate(Expression expression)
        {
            return new QueryTranslator(configuration).Translate(expression);
        }

        IEnumerable ExecuteStream(PreparedCommand command, Type elementType)
        {
            return (IEnumerable)GenericStreamMethod.MakeGenericMethod(elementType)
                .Invoke(queryExecutor, new object[] { command });
        }

        async Task<IEnumerable> ExecuteStreamAsync(PreparedCommand command, Type elementType, CancellationToken cancellationToken)
        {
            var stream = GenericStreamAsyncMethod.MakeGenericMethod(elementType)
                .Invoke(queryExecutor, new object[] { command, cancellationToken });

            return await AsyncEnumerableAdapter.ConvertToEnumerable(stream, elementType, cancellationToken).ConfigureAwait(false);
        }

        object ExecuteScalar(PreparedCommand command, Type resultType)
        {
            return GenericExecuteScalarMethod.MakeGenericMethod(resultType)
                .Invoke(queryExecutor, new object[] { command });
        }

        static object FirstOrDefault(IEnumerable stream)
        {
            var enumerator = stream.GetEnumerator();
            try
            {
                return enumerator.MoveNext() ? enumerator.Current : default;
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }
    }
}

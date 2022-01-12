using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Advanced.Queryable
{
    internal class QueryProvider<T> : IAsyncQueryProvider
    {
        readonly IReadQueryExecutor queryExecutor;
        readonly IRelationalStoreConfiguration configuration;

        public QueryProvider(IReadQueryExecutor queryExecutor, IRelationalStoreConfiguration configuration)
        {
            this.queryExecutor = queryExecutor;
            this.configuration = configuration;
        }

        public IQueryable CreateQuery(Expression expression)
        {
            throw new NotSupportedException();
        }

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression)
        {
            return new Query<TElement>(new QueryProvider<TElement>(queryExecutor, configuration), expression);
        }

        public object Execute(Expression expression)
        {
            var (command, queryType) = Translate(expression);

            if (queryType == QueryType.SelectMany)
            {
                var stream = queryExecutor.Stream<T>(command);
                return stream;
            }

            if (queryType == QueryType.SelectSingle)
            {
                var stream = queryExecutor.Stream<T>(command);
                return stream.FirstOrDefault();
            }

            if (queryType == QueryType.Count)
            {
                var result = queryExecutor.ExecuteScalar<int>(command);
                return result;
            }

            if (queryType == QueryType.Exists)
            {
                var result = queryExecutor.ExecuteScalar<bool>(command);
                return result;
            }

            throw new Exception("Couldn't figure out what to do");
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return (TResult)Execute(expression);
        }

        public async Task<TResult> ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
        {
            var (command, queryType) = Translate(expression);
            if (queryType == QueryType.SelectMany)
            {
                var stream = await queryExecutor.StreamAsync<T>(command, cancellationToken).ToListAsync(cancellationToken);
                return (TResult)Convert.ChangeType(stream, typeof(List<T>));
            }

            if (queryType == QueryType.SelectSingle)
            {
                var item = await queryExecutor.StreamAsync<T>(command, cancellationToken).FirstOrDefaultAsync(cancellationToken);
                return (TResult)Convert.ChangeType(item, typeof(TResult));
            }

            if (queryType == QueryType.Count)
            {
                var result = await queryExecutor.ExecuteScalarAsync<int>(command, cancellationToken);
                return (TResult)Convert.ChangeType(result, typeof(int));
            }

            if (queryType == QueryType.Exists)
            {
                var result = await queryExecutor.ExecuteScalarAsync<bool>(command, cancellationToken);
                return (TResult)Convert.ChangeType(result, typeof(bool));
            }

            throw new Exception("Couldn't figure out what to do");
        }

        (PreparedCommand, QueryType) Translate(Expression expression)
        {
            return new QueryTranslator<T>(configuration).Translate(expression);
        }
    }
}
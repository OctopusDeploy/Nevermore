using System;
using System.Linq;
using System.Linq.Expressions;

namespace Nevermore.Advanced.Queryable
{
    internal class QueryProvider<T> : IQueryProvider
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
            var command = Translate(expression);
            return queryExecutor.Stream<T>(command);
        }

        public TResult Execute<TResult>(Expression expression)
        {
            var command = Translate(expression);
            var enumerable = queryExecutor.Stream<T>(command);
            return (TResult)enumerable;
        }

        PreparedCommand Translate(Expression expression)
        {
            return new QueryTranslator<T>(configuration).Translate(expression);
        }
    }
}
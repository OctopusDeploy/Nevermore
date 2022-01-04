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
            var (command, singleResult) = Translate(expression);
            var stream = queryExecutor.Stream<T>(command);

            if (singleResult)
            {
                return stream.FirstOrDefault();
            }

            return stream;
        }

        public TResult Execute<TResult>(Expression expression)
        {
            return (TResult)Execute(expression);
        }

        (PreparedCommand, bool) Translate(Expression expression)
        {
            return new QueryTranslator<T>(configuration).Translate(expression);
        }
    }
}
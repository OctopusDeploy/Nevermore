using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

namespace Nevermore.Advanced.Queryable
{
    internal class Query<T> : INevermoreQueryable<T>
    {
        readonly QueryProvider queryProvider;

        public Query(QueryProvider queryProvider)
        {
            this.queryProvider = queryProvider;
            Expression = Expression.Constant(this);
        }

        public Query(QueryProvider queryProvider, Expression expression)
        {
            this.queryProvider = queryProvider;
            Expression = expression;
        }

        public IEnumerator<T> GetEnumerator() => queryProvider.Execute<IEnumerable<T>>(Expression).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => queryProvider.Execute<IEnumerable>(Expression).GetEnumerator();

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default) =>
            queryProvider.StreamAsync<T>(Expression, cancellationToken).GetAsyncEnumerator(cancellationToken);

        public Type ElementType => typeof(T);
        public Expression Expression { get; }

        public IQueryProvider Provider => queryProvider;
    }
}
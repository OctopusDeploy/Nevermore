using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace Nevermore.Advanced.Queryable
{
    internal class Query<T> : INevermoreQueryable<T>
    {
        readonly QueryProvider<T> queryProvider;

        public Query(QueryProvider<T> queryProvider)
        {
            this.queryProvider = queryProvider;
            Expression = Expression.Constant(this);
        }

        public Query(QueryProvider<T> queryProvider, Expression expression)
        {
            this.queryProvider = queryProvider;
            Expression = expression;
        }

        public IEnumerator<T> GetEnumerator() => queryProvider.Execute<IEnumerable<object>>(Expression).Cast<T>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)queryProvider.Execute(Expression)).GetEnumerator();

        public Type ElementType => typeof(T);
        public Expression Expression { get; }

        public IQueryProvider Provider => queryProvider;
    }
}
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using Nevermore.Joins;

namespace Nevermore
{
    public interface IQueryBuilder<TRecord> where TRecord : class
    {
        IQueryGenerator QueryGenerator { get; }

        IQueryBuilder<TRecord> Where(string whereClause);
        IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, object value);
        IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, object startValue, object endValue);
        IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, IEnumerable<object> values);
        
        IOrderedQueryBuilder<TRecord> OrderBy(string orderByClause);
        IOrderedQueryBuilder<TRecord> OrderByDescending(string orderByClause);

        IQueryBuilder<TRecord> Join(IJoin join);
        IQueryBuilder<TRecord> Parameter(string name, object value);
        IQueryBuilder<TRecord> LikeParameter(string name, object value);
        IQueryBuilder<TRecord> LikePipedParameter(string name, object value);
        IQueryBuilder<TRecord> View(string viewName);
        IQueryBuilder<TRecord> Table(string tableName);
        IQueryBuilder<TRecord> Hint(string tableHint);
        IQueryBuilder<TRecord> NoLock();

        [Pure]
        int Count();

        [Pure]
        bool Any();

        [Pure]
        TRecord First();

        [Pure]
        IEnumerable<TRecord> Take(int take);

        [Pure]
        List<TRecord> ToList(int skip, int take);

        [Pure]
        List<TRecord> ToList(int skip, int take, out int totalResults);

        [Pure]
        List<TRecord> ToList();

        [Pure]
        void Delete();

        [Pure]
        IEnumerable<TRecord> Stream();

        [Pure]
        IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector);

        [Pure]
        string DebugViewRawQuery();
    }
}
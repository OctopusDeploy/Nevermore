using System;
using System.Collections.Generic;
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

        int Count();
        bool Any();
        TRecord First();
        IEnumerable<TRecord> Take(int take);
        List<TRecord> ToList(int skip, int take);
        List<TRecord> ToList(int skip, int take, out int totalResults);
        List<TRecord> ToList();
        void Delete();
        IEnumerable<TRecord> Stream();
        IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector);
        string DebugViewRawQuery();
    }
}
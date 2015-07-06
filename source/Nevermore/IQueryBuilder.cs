using System;
using System.Collections.Generic;

namespace Nevermore
{
    public interface IQueryBuilder<TRecord> where TRecord : class
    {
        IQueryBuilder<TRecord> Where(string whereClause);
        IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, object value);
        IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, object startValue, object endValue);
        IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, IEnumerable<object> values);
        
        IOrderedQueryBuilder<TRecord> OrderBy(string orderByClause);
        IOrderedQueryBuilder<TRecord> OrderByDescending(string orderByClause);
        
        IQueryBuilder<TRecord> Parameter(string name, object value);
        IQueryBuilder<TRecord> LikeParameter(string name, object value);

        IQueryBuilder<TRecord> View(string viewName);
        IQueryBuilder<TRecord> Table(string tableName);
        IQueryBuilder<TRecord> Hint(string tableHint);

        int Count();
        TRecord First();
        IEnumerable<TRecord> Take(int take);
        List<TRecord> ToList(int skip, int take);
        List<TRecord> ToList(int skip, int take, out int totalResults);
        List<TRecord> ToList();
        IEnumerable<TRecord> Stream();
        IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector);
    }
}
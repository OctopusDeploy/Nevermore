using System;
using System.Collections.Generic;

namespace Nevermore
{
    public interface IQueryBuilder<TRecord> where TRecord : class
    {
        IQueryBuilder<TRecord> Where(string whereClause);
        IQueryBuilder<TRecord> OrderBy(string orderByClause);
        IQueryBuilder<TRecord> Parameter(string name, object value);
        IQueryBuilder<TRecord> LikeParameter(string name, object value);
        IQueryBuilder<TRecord> View(string viewName); 
        IQueryBuilder<TRecord> Table(string tableName);
        IQueryBuilder<TRecord> Hint(string tableHint); 

        int Count();
        TRecord First();
        List<TRecord> ToList(int skip, int take);
        List<TRecord> ToList(int skip, int take, out int totalResults);
        List<TRecord> ToList();
        IEnumerable<TRecord> Stream();
        IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector);
    }
}
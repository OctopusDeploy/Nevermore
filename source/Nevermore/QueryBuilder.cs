using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Nevermore
{
    public class QueryBuilder<TRecord> : IQueryBuilder<TRecord> where TRecord : class
    {
        readonly IRelationalTransaction transaction;
        readonly CommandParameters parameters = new CommandParameters();
        readonly StringBuilder whereClauses = new StringBuilder();
        string viewOrTableName;
        string orderBy = "Id";
        string tableHint;
        string storedProcedureName;

        public QueryBuilder(IRelationalTransaction transaction, string viewOrTableName)
        {
            this.transaction = transaction;
            this.viewOrTableName = viewOrTableName;
        }

        public IQueryBuilder<TRecord> Where(string whereClause)
        {
            if (!string.IsNullOrWhiteSpace(whereClause))
            {
                if (whereClauses.Length > 0)
                {
                    whereClauses.Append(" AND ");
                }
                else
                {
                    whereClauses.Append("WHERE ");
                }

                whereClauses.Append('(');
                whereClauses.Append(whereClause);
                whereClauses.Append(')');
            }

            return this;
        }

        public IQueryBuilder<TRecord> Parameter(string name, object value)
        {
            parameters.Add(name, value);
            return this;
        }

        public IQueryBuilder<TRecord> LikeParameter(string name, object value)
        {
            parameters.Add(name, "%|" + (value ?? string.Empty).ToString().Replace("%", "[%]") + "|%");
            return this;
        }

        public IQueryBuilder<TRecord> Procedure(string storedProcName)
        {
            storedProcedureName = storedProcName;
            return this;
        }

        public IQueryBuilder<TRecord> View(string viewName)
        {
            viewOrTableName = viewName;
            return this;
        }

        public IQueryBuilder<TRecord> Table(string tableName)
        {
            viewOrTableName = tableName;
            return this;
        }

        public IQueryBuilder<TRecord> Hint(string tableHintClause)
        {
            tableHint = tableHintClause;
            return this;
        }

        public IQueryBuilder<TRecord> OrderBy(string orderByClause)
        {
            orderBy = orderByClause;
            return this;
        }

        public int Count()
        {
            return transaction.ExecuteScalar<int>("SELECT COUNT(*) FROM dbo.[" + viewOrTableName + "] " + tableHint + " " + whereClauses, parameters);
        }

        public TRecord First()
        {
            return transaction.ExecuteReader<TRecord>("SELECT TOP 1 * FROM dbo.[" + viewOrTableName + "] " + ToString(), parameters).FirstOrDefault();
        }

        public List<TRecord> ToList(int skip, int take)
        {
            Parameter("_minrow", skip + 1);
            Parameter("_maxrow", take + skip);
            return transaction.ExecuteReader<TRecord>("SELECT * FROM (SELECT *, Row_Number() over (ORDER BY " + orderBy + ") as RowNum FROM dbo.[" + viewOrTableName + "] " + whereClauses + ") RS WHERE RowNum >= @_minrow And RowNum <= @_maxrow", parameters)
                .ToList();
        }

        public List<TRecord> ToList(int skip, int take, out int totalResults)
        {
            totalResults = Count();
            return ToList(skip, take);
        }

        public List<TRecord> ToList()
        {
            return Stream().ToList();
        }

        public IEnumerable<TRecord> Stream()
        {
            return transaction.ExecuteReader<TRecord>("SELECT * FROM dbo.[" + viewOrTableName + "] " + ToString(), parameters);
        }

        public IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector)
        {
            return Stream().ToDictionary(keySelector, StringComparer.OrdinalIgnoreCase);
        }

        public override string ToString()
        {
            return (tableHint == null ? "" : tableHint + " ") + whereClauses + " ORDER BY " + orderBy;
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;

namespace Nevermore
{
    public class QueryBuilder<TRecord> : IOrderedQueryBuilder<TRecord> where TRecord : class
    {
        readonly IRelationalTransaction transaction;
        readonly IQueryGenerator queryGenerator;

        public QueryBuilder(IRelationalTransaction transaction, string viewOrTableName)
        {
            this.transaction = transaction;
            queryGenerator = new SqlQueryGenerator(viewOrTableName);
        }

        public IQueryBuilder<TRecord> Where(string whereClause)
        {
            if (!String.IsNullOrWhiteSpace(whereClause))
            {
                queryGenerator.AddWhere(whereClause);
            }
            return this;
        }

        public IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, object value)
        {
            switch (operand)
            {
                case SqlOperand.Contains:
                    queryGenerator.WhereContains(fieldName, value);
                    break;
                case SqlOperand.EndsWith:
                    queryGenerator.WhereEndsWith(fieldName, value);
                    break;
                case SqlOperand.Equal:
                    queryGenerator.WhereEquals(fieldName, value);
                    break;
                case SqlOperand.GreaterThan:
                    queryGenerator.WhereGreaterThan(fieldName, value);
                    break;
                case SqlOperand.GreaterThanOrEqual:
                    queryGenerator.WhereGreaterThanOrEqual(fieldName, value);
                    break;
                case SqlOperand.LessThan:
                    queryGenerator.WhereLessThan(fieldName, value);
                    break;
                case SqlOperand.LessThanOrEqual:
                    queryGenerator.WhereLessThanOrEqual(fieldName, value);
                    break;
                case SqlOperand.StartsWith:
                    queryGenerator.WhereStartsWith(fieldName, value);
                    break;
            }

            return this;
        }

        public IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, object startValue, object endValue)
        {
            switch (operand)
            {
                case SqlOperand.Between:
                    queryGenerator.WhereBetween(fieldName, startValue, endValue);
                    break;
                case SqlOperand.BetweenOrEqual:
                    queryGenerator.WhereBetweenOrEqual(fieldName, startValue, endValue);
                    break;
            }

            return this;
        }

        public IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, IEnumerable<object> values)
        {
            switch (operand)
            {
                case SqlOperand.In:
                    queryGenerator.WhereIn(fieldName, values);
                    break;
                case SqlOperand.ContainsAll:
                    break;
                case SqlOperand.ContainsAny:
                    break;
            }

            return this;
        }

        public IQueryBuilder<TRecord> Parameter(string name, object value)
        {
            queryGenerator.AddParameter(name, value);
            return this;
        }

        public IQueryBuilder<TRecord> LikeParameter(string name, object value)
        {
            queryGenerator.AddParameter(name, "%|" + (value ?? String.Empty).ToString().Replace("%", "[%]") + "|%");
            return this;
        }

        public IQueryBuilder<TRecord> Procedure(string storedProcName)
        {
            queryGenerator.UseStoredProcedure(storedProcName);
            return this;
        }

        public IQueryBuilder<TRecord> View(string viewName)
        {
            queryGenerator.UseView(viewName);
            return this;
        }

        public IQueryBuilder<TRecord> Table(string tableName)
        {
            queryGenerator.UseTable(tableName);
            return this;
        }

        public IQueryBuilder<TRecord> Hint(string tableHintClause)
        {
            queryGenerator.UseHint(tableHintClause);
            return this;
        }

        public IOrderedQueryBuilder<TRecord> OrderBy(string fieldName)
        {
            queryGenerator.AddOrder(fieldName, false);
            return this;
        }

        public IOrderedQueryBuilder<TRecord> ThenBy(string fieldName)
        {
            return OrderBy(fieldName);
        }

        public IOrderedQueryBuilder<TRecord> OrderByDescending(string fieldName)
        {
            queryGenerator.AddOrder(fieldName, true);
            return this;
        }

        public IOrderedQueryBuilder<TRecord> ThenByDescending(string fieldName)
        {
            return OrderByDescending(fieldName);
        }

        public int Count()
        {
            return transaction.ExecuteScalar<int>(queryGenerator.CountQuery(), queryGenerator.QueryParameters);
        }

        public TRecord First()
        {
            return transaction.ExecuteReader<TRecord>(queryGenerator.TopQuery(), queryGenerator.QueryParameters).FirstOrDefault();
        }

        public List<TRecord> ToList(int skip, int take)
        {
            return transaction.ExecuteReader<TRecord>(queryGenerator.PaginateQuery(skip, take), queryGenerator.QueryParameters)
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
            return transaction.ExecuteReader<TRecord>(queryGenerator.SelectQuery(), queryGenerator.QueryParameters);
        }

        public IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector)
        {
            return Stream().ToDictionary(keySelector, StringComparer.OrdinalIgnoreCase);
        }
    }
}
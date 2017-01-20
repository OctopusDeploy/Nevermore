using System;
using System.Collections.Generic;
using System.Linq;
using Nevermore.Joins;

namespace Nevermore
{
    public class QueryBuilder<TRecord> : IOrderedQueryBuilder<TRecord> where TRecord : class
    {
        readonly IRelationalTransaction transaction;

        public QueryBuilder(IRelationalTransaction transaction, string viewOrTableName, ITableAliasGenerator tableAliasGenerator = null)
        {
            this.transaction = transaction;
            QueryGenerator = new SqlQueryGenerator(viewOrTableName, tableAliasGenerator);
        }

        public IQueryGenerator QueryGenerator { get; }

        public IQueryBuilder<TRecord> Where(string whereClause)
        {
            if (!String.IsNullOrWhiteSpace(whereClause))
            {
                QueryGenerator.AddWhere(whereClause);
            }
            return this;
        }

        public IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, object value)
        {
            switch (operand)
            {
                case SqlOperand.Contains:
                    QueryGenerator.WhereContains(fieldName, value);
                    break;
                case SqlOperand.EndsWith:
                    QueryGenerator.WhereEndsWith(fieldName, value);
                    break;
                case SqlOperand.Equal:
                    QueryGenerator.WhereEquals(fieldName, value);
                    break;
                case SqlOperand.NotEqual:
                    QueryGenerator.WhereNotEquals(fieldName, value);
                    break;
                case SqlOperand.GreaterThan:
                    QueryGenerator.WhereGreaterThan(fieldName, value);
                    break;
                case SqlOperand.GreaterThanOrEqual:
                    QueryGenerator.WhereGreaterThanOrEqual(fieldName, value);
                    break;
                case SqlOperand.LessThan:
                    QueryGenerator.WhereLessThan(fieldName, value);
                    break;
                case SqlOperand.LessThanOrEqual:
                    QueryGenerator.WhereLessThanOrEqual(fieldName, value);
                    break;
                case SqlOperand.StartsWith:
                    QueryGenerator.WhereStartsWith(fieldName, value);
                    break;
                default:
                    throw new ArgumentException($"The operand {operand} is not valid with only one value", nameof(operand));
            }

            return this;
        }

        public IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, object startValue, object endValue)
        {
            switch (operand)
            {
                case SqlOperand.Between:
                    QueryGenerator.WhereBetween(fieldName, startValue, endValue);
                    break;
                case SqlOperand.BetweenOrEqual:
                    QueryGenerator.WhereBetweenOrEqual(fieldName, startValue, endValue);
                    break;
                default:
                    throw new ArgumentException($"The operand {operand} is not valid with two values", nameof(operand));
            }

            return this;
        }

        public IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, IEnumerable<object> values)
        {
            switch (operand)
            {
                case SqlOperand.In:
                    QueryGenerator.WhereIn(fieldName, values);
                    break;
                case SqlOperand.ContainsAll:
                    break;
                case SqlOperand.ContainsAny:
                    break;
                default:
                    throw new ArgumentException($"The operand {operand} is not valid with a list of values", nameof(operand));
            }

            return this;
        }

        public IQueryBuilder<TRecord> Join(IJoin join)
        {
            QueryGenerator.AddJoin(join);
            return this;
        }

        public IQueryBuilder<TRecord> Parameter(string name, object value)
        {
            QueryGenerator.AddParameter(name, value);
            return this;
        }


        public IQueryBuilder<TRecord> LikeParameter(string name, object value)
        {
            QueryGenerator.AddParameter(name, "%" + (value ?? string.Empty).ToString().Replace("[", "[[]").Replace("%", "[%]") + "%");
            return this;
        }

        public IQueryBuilder<TRecord> LikePipedParameter(string name, object value)
        {
            QueryGenerator.AddParameter(name, "%|" + (value ?? string.Empty).ToString().Replace("[", "[[]").Replace("%", "[%]") + "|%");
            return this;
        }


        public IQueryBuilder<TRecord> View(string viewName)
        {
            QueryGenerator.UseView(viewName);
            return this;
        }

        public IQueryBuilder<TRecord> Table(string tableName)
        {
            QueryGenerator.UseTable(tableName);
            return this;
        }

        public IQueryBuilder<TRecord> Hint(string tableHintClause)
        {
            QueryGenerator.UseHint(tableHintClause);
            return this;
        }

        public IOrderedQueryBuilder<TRecord> OrderBy(string fieldName)
        {
            QueryGenerator.AddOrder(fieldName, false);
            return this;
        }


        public IOrderedQueryBuilder<TRecord> ThenBy(string fieldName)
        {
            return OrderBy(fieldName);
        }


        public IOrderedQueryBuilder<TRecord> OrderByDescending(string fieldName)
        {
            QueryGenerator.AddOrder(fieldName, true);
            return this;
        }

        public IOrderedQueryBuilder<TRecord> ThenByDescending(string fieldName)
        {
            return OrderByDescending(fieldName);
        }

        public int Count()
        {
            return transaction.ExecuteScalar<int>(QueryGenerator.CountQuery(), QueryGenerator.QueryParameters);
        }

        public bool Any()
        {
            return Count() != 0;
        }

        public TRecord First()
        {
            return transaction.ExecuteReader<TRecord>(QueryGenerator.TopQuery(), QueryGenerator.QueryParameters).FirstOrDefault();
        }

        public IEnumerable<TRecord> Take(int take)
        {
            return transaction.ExecuteReader<TRecord>(QueryGenerator.TopQuery(take), QueryGenerator.QueryParameters);
        }

        public List<TRecord> ToList(int skip, int take)
        {
            return transaction.ExecuteReader<TRecord>(QueryGenerator.PaginateQuery(skip, take), QueryGenerator.QueryParameters)
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

        public void Delete()
        {
           transaction.ExecuteRawDeleteQuery(QueryGenerator.DeleteQuery(), QueryGenerator.QueryParameters);
        }

        public IEnumerable<TRecord> Stream()
        {
            return transaction.ExecuteReader<TRecord>(QueryGenerator.SelectQuery(), QueryGenerator.QueryParameters);
        }

        public IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector)
        {
            return Stream().ToDictionary(keySelector, StringComparer.OrdinalIgnoreCase);
        }

        public string DebugViewRawQuery()
        {
            return QueryGenerator.SelectQuery();
        }
        
        public IQueryBuilder<TRecord> NoLock()
        {
            Hint("NOLOCK");
            return this;
        }
    }
}
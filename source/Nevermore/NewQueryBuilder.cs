using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Nevermore.Joins;
using Nevermore.QueryGraph;

namespace Nevermore
{
    
    public interface IOrderedQueryBuilder<TRecord> : IQueryBuilder<TRecord>
    {
        IOrderedQueryBuilder<TRecord> ThenBy(string orderByClause);
        IOrderedQueryBuilder<TRecord> ThenByDescending(string orderByClause);
    }

    public interface ITableSourceQueryBuilder<TRecord> : IQueryBuilder<TRecord>
    {
        ITableSourceQueryBuilder<TRecord> View(string viewName);
        ITableSourceQueryBuilder<TRecord> Table(string tableName);
        ITableSourceQueryBuilder<TRecord> Alias(string tableAlias); 
        IQueryBuilder<TRecord> Hint(string tableHint);
        IQueryBuilder<TRecord> NoLock();
        IAliasedSelectSource AsAliasedSource();
    }

    public interface ISubquerySourceBuilder<TRecord> : IQueryBuilder<TRecord>
    {
        ISubquerySource AsSource();
        ISubquerySourceBuilder<TRecord> Alias(string subqueryAlias);
    }

    public interface IJoinSourceQueryBuilder<TRecord> : IQueryBuilder<TRecord>
    {
        IJoinSourceQueryBuilder<TRecord> On(string leftField, JoinOperand operand, string rightField);
        IJoinSourceQueryBuilder<TRecord> ThenInnerJoin(IAliasedSelectSource source); // todo: delete, don't need it!!
    }

    public interface IQueryBuilder<TRecord>
    {
        IQueryBuilder<TRecord> Where(string whereClause);
        IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, object value);
        IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, object startValue, object endValue);
        IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, IEnumerable<object> values);

        IOrderedQueryBuilder<TRecord> OrderBy(string orderByClause);
        IOrderedQueryBuilder<TRecord> OrderByDescending(string orderByClause);

        IQueryBuilder<TRecord> Column(string name);
        IQueryBuilder<TRecord> Column(string name, string columnAlias);
        IQueryBuilder<TRecord> Column(string name, string columnAlias, string tableAlias);
        IQueryBuilder<TRecord> CalculatedColumn(string expression, string columnAlias);
        IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias);
        IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias, params string[] partitionByColumns);
        IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias, params ColumnFromTable[] partitionByColumns);
        IQueryBuilder<TRecord> Parameter(string name, object value);
        IQueryBuilder<TRecord> LikeParameter(string name, object value);
        IQueryBuilder<TRecord> LikePipedParameter(string name, object value);

        IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType);
        ISubquerySourceBuilder<TRecord> Union(IQueryBuilder<TRecord> queryBuilder);
        ISubquerySourceBuilder<TRecord> Subquery(); 
        ISelectBuilder GetSelectBuilder();
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
        CommandParameters QueryParameters { get; }
    }

    public class ColumnFromTable // todo: rename
    {
        public ColumnFromTable(string columnName, string table)
        {
            ColumnName = columnName;
            Table = table;
        }

        public string ColumnName { get; }
        public string Table { get; }
    }

    public class QueryBuilder<TRecord, TSelectBuilder> : IOrderedQueryBuilder<TRecord> where TSelectBuilder: ISelectBuilder // todo: add class constraint on TRecord
    {
        readonly TSelectBuilder selectBuilder;
        readonly IRelationalTransaction transaction;
        readonly ITableAliasGenerator tableAliasGenerator;
        public CommandParameters QueryParameters { get; }

        public QueryBuilder(TSelectBuilder selectBuilder, IRelationalTransaction transaction,
            ITableAliasGenerator tableAliasGenerator, CommandParameters queryParameters)
        {
            this.selectBuilder = selectBuilder;
            this.transaction = transaction;
            this.tableAliasGenerator = tableAliasGenerator;
            QueryParameters = queryParameters;
        }

        public IQueryBuilder<TRecord> Where(string whereClause)
        {
            if (!String.IsNullOrWhiteSpace(whereClause))
            {
                selectBuilder.AddWhere(whereClause);
            }
            return this;
        }

        IQueryBuilder<TRecord> AddUnaryWhereClauseAndParameter(string fieldName, UnarySqlOperand operand, object value)
        {
            var unaryParameter = new UnaryWhereParameter(fieldName, operand);
            selectBuilder.AddWhere(unaryParameter);
            return Parameter(unaryParameter.ParameterName, value);
        }

        public IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, object value)
        {
            switch(operand)
                {
                    case SqlOperand.Equal:
                        return AddUnaryWhereClauseAndParameter(fieldName, UnarySqlOperand.Equal, value);
                    case SqlOperand.GreaterThan:
                        return AddUnaryWhereClauseAndParameter(fieldName, UnarySqlOperand.GreaterThan, value);
                    case SqlOperand.GreaterThanOrEqual:
                        return AddUnaryWhereClauseAndParameter(fieldName, UnarySqlOperand.GreaterThanOrEqual, value);
                    case SqlOperand.LessThan:
                        return AddUnaryWhereClauseAndParameter(fieldName, UnarySqlOperand.LessThan, value);
                    case SqlOperand.LessThanOrEqual:
                        return AddUnaryWhereClauseAndParameter(fieldName, UnarySqlOperand.LessThanOrEqual, value);
                    case SqlOperand.NotEqual:
                        return AddUnaryWhereClauseAndParameter(fieldName, UnarySqlOperand.NotEqual, value);
                    case SqlOperand.Contains:
                        return AddUnaryWhereClauseAndParameter(fieldName, UnarySqlOperand.Contains, $"%{value}%");
                    case SqlOperand.StartsWith:
                        return AddUnaryWhereClauseAndParameter(fieldName, UnarySqlOperand.StartsWith, $"{value}%");
                    case SqlOperand.EndsWith:
                        return AddUnaryWhereClauseAndParameter(fieldName, UnarySqlOperand.EndsWith, $"%{value}");
                    case SqlOperand.In:
                        if (value is IEnumerable enumerable)
                            return AddWhereIn(fieldName, enumerable);
                        else
                            throw new ArgumentException($"The operand {operand} is not valid with only one value",
                                nameof(operand));
                    default:
                        throw new ArgumentException($"The operand {operand} is not valid with only one value", nameof(operand));
                }
        }

        public IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, object startValue, object endValue)
        {
            switch (operand)
            {
                case SqlOperand.Between:
                    var parameter = new BinaryWhereParameter(fieldName, BinarySqlOperand.Between, "StartValue", "EndValue");
                    selectBuilder.AddWhere(parameter);
                    return Parameter(parameter.FirstParameterName, startValue)
                        .Parameter(parameter.SecondParameterName, endValue);
                case SqlOperand.BetweenOrEqual:
                    var greaterThanWhereParameter = new UnaryWhereParameter(fieldName, UnarySqlOperand.GreaterThanOrEqual, "StartValue");
                    var lessThanWhereParameter = new UnaryWhereParameter(fieldName, UnarySqlOperand.LessThanOrEqual, "EndValue");
                    selectBuilder.AddWhere(greaterThanWhereParameter);
                    selectBuilder.AddWhere(lessThanWhereParameter);
                    return Parameter(greaterThanWhereParameter.ParameterName, startValue)
                        .Parameter(lessThanWhereParameter.ParameterName, endValue);
                default:
                    throw new ArgumentException($"The operand {operand} is not valid with two values", nameof(operand));
            }
        }

        public IQueryBuilder<TRecord> Where(string fieldName, SqlOperand operand, IEnumerable<object> values)
        {
            switch (operand)
            {
                case SqlOperand.In:
                    AddWhereIn(fieldName, values);
                    break;
                default:
                    throw new ArgumentException($"The operand {operand} is not valid with a list of values", nameof(operand));
            }

            return this;
        }

        IQueryBuilder<TRecord> AddWhereIn(string fieldName, IEnumerable values)
        {
            var stringValues = values.OfType<object>().Select(v => v.ToString()).ToArray();
            if (!stringValues.Any()) return AddAlwaysFalseWhere();
            {
                var arrayWhereParameter = new ArrayWhereParameter(fieldName, ArraySqlOperand.In, stringValues.Length);
                selectBuilder.AddWhere(arrayWhereParameter);
                IQueryBuilder<TRecord> queryBuilderAsInterface = this;
                return stringValues.Zip(arrayWhereParameter.ParameterNames, (value, parameterName) => new {value, parameterName})
                    .Aggregate(queryBuilderAsInterface, (p, nv) => p.Parameter(nv.parameterName, nv.value));
            }

        }

        IQueryBuilder<TRecord> AddAlwaysFalseWhere()
        {
            return Where("0 = 1");
        }

        public IQueryBuilder<TRecord> CalculatedColumn(string expression, string columnAlias)
        {
            selectBuilder.AddColumnSelection(new CalculatedColumn(expression, columnAlias));
            return this;
        }

        public IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias)
        {
            return AddRowNumberColumn(columnAlias, new string[0]);
        }

        public IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias, params string[] partitionByColumns)
        {
            selectBuilder.AddRowNumberColumn(columnAlias, partitionByColumns.Select(c => new Column(c)).ToList());
            return this;
        }

        public IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias, params ColumnFromTable[] partitionByColumns)
        {
            selectBuilder.AddRowNumberColumn(columnAlias, partitionByColumns.Select(c => new TableColumn(new Column(c.ColumnName), c.Table)).ToList());
            return this;
        }

        public IQueryBuilder<TRecord> Parameter(string name, object value)
        {
            QueryParameters.Add(name, value);
            return this;
        }


        public IQueryBuilder<TRecord> LikeParameter(string name, object value)
        {
            return Parameter(name, "%" + (value ?? string.Empty).ToString().Replace("[", "[[]").Replace("%", "[%]") + "%");
        }

        public IQueryBuilder<TRecord> LikePipedParameter(string name, object value)
        {
            return Parameter(name, "%|" + (value ?? string.Empty).ToString().Replace("[", "[[]").Replace("%", "[%]") + "|%");
        }

        public IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType)
        {
            var subquery = new SubquerySource(selectBuilder.GenerateSelect(true), tableAliasGenerator.GenerateTableAlias());
            return new JoinSourceQueryBuilder<TRecord>(subquery, joinType, source, transaction, tableAliasGenerator, new CommandParameters(QueryParameters));
        }

        public ISubquerySourceBuilder<TRecord> Union(IQueryBuilder<TRecord> queryBuilder)
        {
            return new SubquerySourceBuilder<TRecord>(new Union(new [] { selectBuilder.GenerateSelect(true), queryBuilder.GetSelectBuilder().GenerateSelect(true) }), tableAliasGenerator.GenerateTableAlias(),
                transaction, tableAliasGenerator, new CommandParameters(QueryParameters));
        }

        public ISubquerySourceBuilder<TRecord> Subquery()
        {
            return new SubquerySourceBuilder<TRecord>(selectBuilder.GenerateSelect(true), tableAliasGenerator.GenerateTableAlias(), transaction, tableAliasGenerator, new CommandParameters(QueryParameters));
        }

        SubquerySelectBuilder CreateSubqueryBuilder()
        {
            return new SubquerySelectBuilder(new SubquerySource(selectBuilder.GenerateSelect(true), tableAliasGenerator.GenerateTableAlias()));
        }

        public ISelectBuilder GetSelectBuilder()
        {
            return selectBuilder;
        }

        public IOrderedQueryBuilder<TRecord> OrderBy(string fieldName)
        {
            selectBuilder.AddOrder(fieldName, false);
            return this;
        }


        public IOrderedQueryBuilder<TRecord> ThenBy(string fieldName)
        {
            return OrderBy(fieldName);
        }


        public IOrderedQueryBuilder<TRecord> OrderByDescending(string fieldName)
        {
            selectBuilder.AddOrder(fieldName, true);
            return this;
        }

        public IQueryBuilder<TRecord> Column(string name)
        {
            selectBuilder.AddColumn(name);
            return this;
        }

        public IQueryBuilder<TRecord> Column(string name, string columnAlias)
        {
            selectBuilder.AddColumn(name, columnAlias);
            return this;
        }

        public IQueryBuilder<TRecord> Column(string name, string columnAlias, string tableAlias)
        {
            selectBuilder.AddColumnSelection(new AliasedColumn(new TableColumn(new Column(name), tableAlias), columnAlias));
            return this;
        }

        public IOrderedQueryBuilder<TRecord> ThenByDescending(string fieldName)
        {
            return OrderByDescending(fieldName);
        }

        [Pure]
        public int Count()
        {
            selectBuilder.AddColumnSelection(new SelectCountSource());
            return transaction.ExecuteScalar<int>(selectBuilder.GenerateSelect().GenerateSql(), QueryParameters);
        }

        [Pure]
        public bool Any()
        {
            return Count() != 0;
        }

        [Pure]
        public TRecord First()
        {
            return Take(1).FirstOrDefault();
        }

        [Pure]
        public IEnumerable<TRecord> Take(int take)
        {
            selectBuilder.AddTop(take);
            return transaction.ExecuteReader<TRecord>(selectBuilder.GenerateSelect().GenerateSql(), QueryParameters);
        }

        [Pure]
        public List<TRecord> ToList(int skip, int take)
        {
            const string rowNumberColumnName = "RowNum";
            const string minRowParameterName = "_minrow";
            const string maxRowParameterName = "_maxrow";

            selectBuilder.AddDefaultColumnSelection();
            selectBuilder.AddRowNumberColumn(rowNumberColumnName, new List<Column>());

            var subqueryBuilder = CreateSubqueryBuilder();
            subqueryBuilder.AddWhere(new UnaryWhereParameter(rowNumberColumnName, UnarySqlOperand.GreaterThanOrEqual, minRowParameterName));
            subqueryBuilder.AddWhere(new UnaryWhereParameter(rowNumberColumnName, UnarySqlOperand.LessThanOrEqual, maxRowParameterName));
            subqueryBuilder.AddOrder("RowNum", false);

            Parameter(minRowParameterName, skip + 1);
            Parameter(maxRowParameterName, take + skip);

            return transaction.ExecuteReader<TRecord>(subqueryBuilder.GenerateSelect().GenerateSql(), QueryParameters).ToList();
        }

        [Pure]
        public List<TRecord> ToList(int skip, int take, out int totalResults)
        {
            totalResults = Count();
            return ToList(skip, take);
        }

        [Pure]
        public List<TRecord> ToList()
        {
            return Stream().ToList();
        }

        [Pure]
        public IEnumerable<TRecord> Stream()
        {
            return transaction.ExecuteReader<TRecord>(selectBuilder.GenerateSelect().GenerateSql(), QueryParameters);
        }

        [Pure]
        public IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector)
        {
            return Stream().ToDictionary(keySelector, StringComparer.OrdinalIgnoreCase);
        }

        [Pure]
        public string DebugViewRawQuery()
        {
            return selectBuilder.GenerateSelect().GenerateSql();
        }

        public void Delete()
        {
            transaction.ExecuteRawDeleteQuery(selectBuilder.DeleteQuery(), QueryParameters);
        }
    }
}
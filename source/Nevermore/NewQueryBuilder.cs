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
    }

    public interface IQueryBuilder<TRecord>
    {
        IQueryBuilder<TRecord> Where(string whereClause);
        IQueryBuilder<TRecord> WhereParameterised(string fieldName, UnarySqlOperand operand, Parameter parameter);
        IQueryBuilder<TRecord> WhereParameterised(string fieldName, BinarySqlOperand operand, Parameter startValueParameter, Parameter endValueParameter);
        IQueryBuilder<TRecord> WhereParameterised(string fieldName, ArraySqlOperand operand, IEnumerable<Parameter> parameterNames);

        IOrderedQueryBuilder<TRecord> OrderBy(string orderByClause);
        IOrderedQueryBuilder<TRecord> OrderByDescending(string orderByClause);

        IQueryBuilder<TRecord> Column(string name);
        IQueryBuilder<TRecord> Column(string name, string columnAlias);
        IQueryBuilder<TRecord> Column(string name, string columnAlias, string tableAlias);
        IQueryBuilder<TRecord> AllColumns();
        IQueryBuilder<TRecord> CalculatedColumn(string expression, string columnAlias);
        IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias);
        IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias, params string[] partitionByColumns);
        IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias, params ColumnFromTable[] partitionByColumns);
        IQueryBuilder<TRecord> Parameter(Parameter parameter);
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
    }

    public class Parameter
    {
        public Parameter(string parameterName, IDataType dataType)
        {
            ParameterName = parameterName;
            DataType = dataType;
        }

        public Parameter(string parameterName)
        {
            ParameterName = parameterName;
        }

        public string ParameterName { get; }

        // Data type must be specified if you are creating a stored proc or function, otherwise it is not required
        public IDataType DataType { get; }
    }

    public interface IDataType
    {
        string GenerateSql();
    }

    public class NVarChar : IDataType
    {
        readonly int? length;

        public NVarChar(int length)
        {
            this.length = length;
        }

        public NVarChar()
        {
            length = null;
        }

        public string GenerateSql()
        {
            var size = length == null ? "MAX" : length.ToString();
            return $"NVARCHAR({size})";
        }
    }

    public class DataType
    {
        Type ColumnDataType { get; set; }
        uint Length { get; set; }
    }

    public static class QueryBuilderExtensions
    {
        public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string fieldName, SqlOperand operand, object value)
        {
            switch(operand)
            {
                case SqlOperand.Equal:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.Equal, value);
                case SqlOperand.GreaterThan:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.GreaterThan, value);
                case SqlOperand.GreaterThanOrEqual:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.GreaterThanOrEqual, value);
                case SqlOperand.LessThan:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.LessThan, value);
                case SqlOperand.LessThanOrEqual:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.LessThanOrEqual, value);
                case SqlOperand.NotEqual:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.NotEqual, value);
                case SqlOperand.Contains:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.Like, $"%{value}%");
                case SqlOperand.StartsWith:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.Like, $"{value}%");
                case SqlOperand.EndsWith:
                    return AddUnaryWhereClauseAndParameter(queryBuilder, fieldName, UnarySqlOperand.Like, $"%{value}");
                case SqlOperand.In:
                    if (value is IEnumerable enumerable)
                        return AddWhereIn(queryBuilder, fieldName, enumerable);
                    else
                        throw new ArgumentException($"The operand {operand} is not valid with only one value",
                            nameof(operand));
                default:
                    throw new ArgumentException($"The operand {operand} is not valid with only one value", nameof(operand));
            }
        }

        public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string fieldName,
            SqlOperand operand, object startValue, object endValue)
        {
            Parameter startValueParameter = new Parameter("StartValue");
            Parameter endValueParameter = new Parameter("EndValue");
            switch (operand)
            {
                case SqlOperand.Between:
                    return queryBuilder.WhereParameterised(fieldName, BinarySqlOperand.Between, startValueParameter, endValueParameter)
                        .Parameter(startValueParameter.ParameterName, startValue)
                        .Parameter(endValueParameter.ParameterName, endValue);
                case SqlOperand.BetweenOrEqual:
                    return queryBuilder.WhereParameterised(fieldName, UnarySqlOperand.GreaterThanOrEqual, startValueParameter)
                        .Parameter(startValueParameter.ParameterName, startValue)
                        .WhereParameterised(fieldName, UnarySqlOperand.LessThanOrEqual, endValueParameter)
                        .Parameter(endValueParameter.ParameterName, endValue);
                default:
                    throw new ArgumentException($"The operand {operand} is not valid with two values", nameof(operand));
            }
        }

        static IQueryBuilder<TRecord> AddUnaryWhereClauseAndParameter<TRecord>(IQueryBuilder<TRecord> queryBuilder, string fieldName, UnarySqlOperand operand, object value)
        {
            var parameterName = fieldName.ToLower();
            return queryBuilder.WhereParameterised(fieldName, operand, new Parameter(parameterName))
                .Parameter(parameterName, value);
        }

        static IQueryBuilder<TRecord> AddWhereIn<TRecord>(IQueryBuilder<TRecord> queryBuilder, string fieldName, IEnumerable values)
        {
            var stringValues = values.OfType<object>().Select(v => v.ToString()).ToArray();
            var parameters = stringValues.Select((v, i) => new Parameter($"{fieldName.ToLower()}{i}")).ToArray();
            return stringValues.Zip(parameters, (value, parameter) => new {value, parameter})
                .Aggregate(queryBuilder.WhereParameterised(fieldName, ArraySqlOperand.In, parameters),
                    (p, pv) => p.Parameter(pv.parameter.ParameterName, pv.value));
        }

        public static IQueryBuilder<TRecord> Where<TRecord>(this IQueryBuilder<TRecord> queryBuilder, string fieldName,
            SqlOperand operand, IEnumerable<object> values)
        {
            switch (operand)
            {
                case SqlOperand.In:
                    return AddWhereIn(queryBuilder, fieldName, values);
                default:
                    throw new ArgumentException($"The operand {operand} is not valid with a list of values", nameof(operand));
            }
        }
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
        readonly CommandParameterValues parameterValues;
        readonly Parameters parameters;

        public QueryBuilder(TSelectBuilder selectBuilder, IRelationalTransaction transaction,
            ITableAliasGenerator tableAliasGenerator, CommandParameterValues parameterValues, Parameters parameters)
        {
            this.selectBuilder = selectBuilder;
            this.transaction = transaction;
            this.tableAliasGenerator = tableAliasGenerator;
            this.parameterValues = parameterValues;
            this.parameters = parameters;
        }

        public IQueryBuilder<TRecord> Where(string whereClause)
        {
            if (!String.IsNullOrWhiteSpace(whereClause))
            {
                selectBuilder.AddWhere(whereClause);
            }
            return this;
        }

        public IQueryBuilder<TRecord> WhereParameterised(string fieldName, UnarySqlOperand operand, Parameter parameter)
        {
            selectBuilder.AddWhere(new UnaryWhereParameter(fieldName, operand, parameter.ParameterName));
            return Parameter(parameter);
        }

        public IQueryBuilder<TRecord> WhereParameterised(string fieldName, BinarySqlOperand operand, Parameter startValueParameter, Parameter endValueParameter)
        {
            selectBuilder.AddWhere(new BinaryWhereParameter(fieldName, operand, startValueParameter.ParameterName, endValueParameter.ParameterName));
            return Parameter(startValueParameter).Parameter(endValueParameter);
        }

        public IQueryBuilder<TRecord> WhereParameterised(string fieldName, ArraySqlOperand operand, IEnumerable<Parameter> parameterNames)
        {
            var parameterNamesList = parameterNames.ToList();
            if (!parameterNamesList.Any())
            {
                return AddAlwaysFalseWhere();
            }
            selectBuilder.AddWhere(new ArrayWhereParameter(fieldName, operand, parameterNamesList.Select(p => p.ParameterName).ToList()));
            IQueryBuilder<TRecord> builder = this;
            return parameterNamesList.Aggregate(builder, (b, p) => b.Parameter(p));
        }
        
        IQueryBuilder<TRecord> AddAlwaysFalseWhere()
        {
            return Where("0 = 1");
        }

        public IQueryBuilder<TRecord> AllColumns()
        {
            selectBuilder.AddDefaultColumnSelection();
            return this;
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

        public IQueryBuilder<TRecord> Parameter(Parameter parameter)
        {
            parameters.Add(parameter);
            return this;
        }

        public IQueryBuilder<TRecord> Parameter(string name, object value)
        {
            parameterValues.Add(name, value);
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
            return new JoinSourceQueryBuilder<TRecord>(subquery, joinType, source, transaction, tableAliasGenerator, new CommandParameterValues(parameterValues), new Parameters(parameters));
        }

        public ISubquerySourceBuilder<TRecord> Union(IQueryBuilder<TRecord> queryBuilder)
        {
            return new SubquerySourceBuilder<TRecord>(new Union(new [] { selectBuilder.GenerateSelect(true), queryBuilder.GetSelectBuilder().GenerateSelect(true) }), tableAliasGenerator.GenerateTableAlias(),
                transaction, tableAliasGenerator, new CommandParameterValues(parameterValues), new Parameters(parameters));
        }

        public ISubquerySourceBuilder<TRecord> Subquery()
        {
            return new SubquerySourceBuilder<TRecord>(selectBuilder.GenerateSelect(true), tableAliasGenerator.GenerateTableAlias(), transaction, tableAliasGenerator, new CommandParameterValues(parameterValues), new Parameters(parameters));
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
            return transaction.ExecuteScalar<int>(selectBuilder.GenerateSelect().GenerateSql(), parameterValues);
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
            return transaction.ExecuteReader<TRecord>(selectBuilder.GenerateSelect().GenerateSql(), parameterValues);
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

            return transaction.ExecuteReader<TRecord>(subqueryBuilder.GenerateSelect().GenerateSql(), parameterValues).ToList();
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
            return transaction.ExecuteReader<TRecord>(selectBuilder.GenerateSelect().GenerateSql(), parameterValues);
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
            transaction.ExecuteRawDeleteQuery(selectBuilder.DeleteQuery(), parameterValues);
        }
    }
}
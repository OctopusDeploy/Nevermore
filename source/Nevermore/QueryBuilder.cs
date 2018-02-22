using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text.RegularExpressions;
using Nevermore.AST;

namespace Nevermore
{
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
                var whereClauseNormalised = Regex.Replace(whereClause, @"@\w+", m => new Parameter(m.Value).ParameterName);
                selectBuilder.AddWhere(whereClauseNormalised);
            }
            return this;
        }

        public IQueryBuilder<TRecord> WhereParameterised(string fieldName, UnarySqlOperand operand, Parameter parameter)
        {
            selectBuilder.AddWhere(new UnaryWhereParameter(fieldName, operand, parameter));
            return Parameter(parameter);
        }

        public IQueryBuilder<TRecord> WhereParameterised(string fieldName, BinarySqlOperand operand, Parameter startValueParameter, Parameter endValueParameter)
        {
            selectBuilder.AddWhere(new BinaryWhereParameter(fieldName, operand, startValueParameter, endValueParameter));
            return Parameter(startValueParameter).Parameter(endValueParameter);
        }

        public IQueryBuilder<TRecord> WhereParameterised(string fieldName, ArraySqlOperand operand, IEnumerable<Parameter> parameterNames)
        {
            var parameterNamesList = parameterNames.ToList();
            if (!parameterNamesList.Any())
            {
                return AddAlwaysFalseWhere();
            }
            selectBuilder.AddWhere(new ArrayWhereParameter(fieldName, operand, parameterNamesList));
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
            selectBuilder.AddColumnSelection(new AliasedColumn(new CalculatedColumn(expression), columnAlias));
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

        public IQueryBuilder<TRecord> Parameter(Parameter parameter, object value)
        {
            parameterValues.Add(parameter.ParameterName, value);
            return this;
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
            var minRowParameter = new Parameter("_minrow");
            var maxRowParameter = new Parameter("_maxrow");

            selectBuilder.AddDefaultColumnSelection();
            selectBuilder.AddRowNumberColumn(rowNumberColumnName, new List<Column>());

            var subqueryBuilder = CreateSubqueryBuilder();
            subqueryBuilder.AddWhere(new UnaryWhereParameter(rowNumberColumnName, UnarySqlOperand.GreaterThanOrEqual, minRowParameter));
            subqueryBuilder.AddWhere(new UnaryWhereParameter(rowNumberColumnName, UnarySqlOperand.LessThanOrEqual, maxRowParameter));
            subqueryBuilder.AddOrder("RowNum", false);

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
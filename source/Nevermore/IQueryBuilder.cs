using System;
using System.Collections.Generic;
using Nevermore.AST;

namespace Nevermore
{
    public interface IOrderedQueryBuilder<TRecord> : IQueryBuilder<TRecord> where TRecord : class
    {
    }

    public interface ITableSourceQueryBuilder<TRecord> : IQueryBuilder<TRecord> where TRecord : class
    {
        ITableSourceQueryBuilder<TRecord> View(string viewName);
        ITableSourceQueryBuilder<TRecord> Table(string tableName);
        ITableSourceQueryBuilder<TRecord> Alias(string tableAlias); 
        IQueryBuilder<TRecord> Hint(string tableHint);
        IAliasedSelectSource AsAliasedSource();
    }

    public interface ISubquerySourceBuilder<TRecord> : IQueryBuilder<TRecord> where TRecord : class
    {
        ISubquerySource AsSource();
        ISubquerySourceBuilder<TRecord> Alias(string subqueryAlias);
    }

    public interface IJoinSourceQueryBuilder<TRecord> : IQueryBuilder<TRecord> where TRecord : class
    {
        IJoinSourceQueryBuilder<TRecord> On(string leftField, JoinOperand operand, string rightField);
    }

    public interface IQueryBuilder<TRecord> where TRecord : class
    {
        IQueryBuilder<TRecord> Where(string whereClause);
        IQueryBuilder<TRecord> WhereParameterised(string fieldName, UnarySqlOperand operand, Parameter parameter);
        IQueryBuilder<TRecord> WhereParameterised(string fieldName, BinarySqlOperand operand, Parameter startValueParameter, Parameter endValueParameter);
        IQueryBuilder<TRecord> WhereParameterised(string fieldName, ArraySqlOperand operand, IEnumerable<Parameter> parameterNames);

        IOrderedQueryBuilder<TRecord> OrderBy(string orderByClause);
        IOrderedQueryBuilder<TRecord> OrderByDescending(string orderByClause);
        IQueryBuilder<TRecord> IgnoreDefaultOrderBy();

        IQueryBuilder<TRecord> Column(string name);
        IQueryBuilder<TRecord> Column(string name, string columnAlias);
        IQueryBuilder<TRecord> Column(string name, string columnAlias, string tableAlias);
        IQueryBuilder<TRecord> AllColumns();
        IQueryBuilder<TRecord> CalculatedColumn(string expression, string columnAlias);
        IQueryBuilder<TNewRecord> AsType<TNewRecord>() where TNewRecord : class;

        IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias);
        IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias, params string[] partitionByColumns);
        IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias, params ColumnFromTable[] partitionByColumns);
        IQueryBuilder<TRecord> Parameter(Parameter parameter);
        IQueryBuilder<TRecord> ParameterDefault(Parameter parameter, object defaultValue);
        IQueryBuilder<TRecord> Parameter(Parameter parameter, object value);

        IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType, CommandParameterValues parameterValues, Parameters parameters, ParameterDefaults parameterDefaults);
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
        IEnumerable<TRecord> Stream();
        IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector);
        Parameters Parameters { get; }
        ParameterDefaults ParameterDefaults { get; }
        CommandParameterValues ParameterValues { get; }
        string DebugViewRawQuery();
    }

    public class ColumnFromTable
    {
        public ColumnFromTable(string columnName, string table)
        {
            ColumnName = columnName;
            Table = table;
        }

        public string ColumnName { get; }
        public string Table { get; }
    }
}
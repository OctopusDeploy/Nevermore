using System;
using System.Collections.Generic;
using Nevermore.Querying;
using Nevermore.Querying.AST;

namespace Nevermore
{
    public interface IOrderedQueryBuilder<TRecord> : IQueryBuilder<TRecord> where TRecord : class
    {
    }

    public interface ITableSourceQueryBuilder<TRecord> : IQueryBuilder<TRecord> where TRecord : class
    {
        /// <summary>
        /// Change the source of the query from the automatically populated table name to a specified view
        /// </summary>
        /// <param name="viewName">The view to use as the source of the query</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        ITableSourceQueryBuilder<TRecord> View(string viewName);

        /// <summary>
        /// Change the source of the query from the automatically populated table name to a specified table
        /// </summary>
        /// <param name="tableName">The table to use as the source of the query</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        ITableSourceQueryBuilder<TRecord> Table(string tableName);

        /// <summary>
        /// Adds a specified alias for the table. If not specified, it may be automatically generated for more complex queries (for example, involving joins)
        /// </summary>
        /// <param name="tableAlias">The alias to use for the table</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        ITableSourceQueryBuilder<TRecord> Alias(string tableAlias); 

        /// <summary>
        /// Adds a table hint to the query for the table.
        /// </summary>
        /// <param name="tableHint">The table hint to add</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> Hint(string tableHint);

        /// <summary>
        /// Converts the query to a source with an alias that can be used as part of a join
        /// </summary>
        /// <returns>The source that can be used as part of a join</returns>
        IAliasedSelectSource AsAliasedSource();
    }

    public interface ISubquerySourceBuilder<TRecord> : IQueryBuilder<TRecord> where TRecord : class
    {
        /// <summary>
        /// Converts the query to a source with an alias that can be used as part of a join
        /// </summary>
        /// <returns>The source that can be used as part of a join</returns>
        ISubquerySource AsSource();

        /// <summary>
        /// Adds a specified alias for the subquery. If not specified, it may be automatically generated for more complex queries (for example, involving joins)
        /// </summary>
        /// <param name="subqueryAlias">The alias to use for the subquery</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        ISubquerySourceBuilder<TRecord> Alias(string subqueryAlias);
    }

    public interface IJoinSourceQueryBuilder<TRecord> : IQueryBuilder<TRecord> where TRecord : class
    {
        /// <summary>
        /// Adds a join condition (ON expression) to a join. At least one join condition must be added for each join.
        /// </summary>
        /// <param name="leftField">The column from the left side of the join to use in the join condition</param>
        /// <param name="operand">The operator to use in the join condition</param>
        /// <param name="rightField">The column from the right side of the join to use in the join condition</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IJoinSourceQueryBuilder<TRecord> On(string leftField, JoinOperand operand, string rightField);
        
        /// <summary>
        /// Adds a join condition (ON expression) to a join. At least one join condition must be added for each join.
        /// </summary>
        /// <param name="leftTableAlias">The table alias for the column from the left side of the join to use in the join condition</param>
        /// <param name="leftField">The column from the left side of the join to use in the join condition</param>
        /// <param name="operand">The operator to use in the join condition</param>
        /// <param name="rightField">The column from the right side of the join to use in the join condition</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IJoinSourceQueryBuilder<TRecord> On(string leftTableAlias, string leftField, JoinOperand operand, string rightField);
    }

    public interface IQueryBuilder<TRecord> : ICompleteQuery<TRecord> where TRecord : class
    {
        /// <summary>
        /// Sets the command timeout for execution of the query
        /// </summary>
        /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        ICompleteQuery<TRecord> WithTimeout(TimeSpan commandTimeout);

        /// <summary>
        /// Adds a custom where expression to the query. Avoid using this as it is difficult to refactor. Prefer using Where methods from <see cref="QueryBuilderWhereExtensions" />
        /// </summary>
        /// <param name="whereClause">Where clause expression that will be inserted directly into the resulting SQL string</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> Where(string whereClause);

        /// <summary>
        /// Adds a parameterised unary where clause to the query. This does not add a value for that parameter, and is therefore useful for Functions and Stored Procedures.
        /// Prefer using Where methods from <see cref="QueryBuilderWhereExtensions" /> in normal queries, which allows you to additionally provide a value for the parameter.
        /// </summary>
        /// <param name="fieldName">The name of one of the columns in the query. The where condition will be evaluated against the value of this column.</param>
        /// <param name="operand">The SQL operator to be used in the where clause</param>
        /// <param name="parameter">The parameter that will be included in the where clause. Requires a data type if used as part of a Function or Stored Procedure</param>
        /// <returns>The query builder that can be used to add parameters to the query, and then further modify the query, or execute the query</returns>
        IUnaryParameterQueryBuilder<TRecord> WhereParameterized(string fieldName, UnarySqlOperand operand, Parameter parameter);

        /// <summary>
        /// Adds a parameterised binary where clause to the query. This does not add a values for the parameters, and is therefore useful for Functions and Stored Procedures.
        /// Prefer using Where methods from <see cref="QueryBuilderWhereExtensions" /> in normal queries, which allows you to additionally provide a values for the parameters.
        /// </summary>
        /// <param name="fieldName">The name of one of the columns in the query. The where condition will be evaluated against the value of this column.</param>
        /// <param name="operand">The SQL operator to be used in the where clause</param>
        /// <param name="startValueParameter">The first parameter that will be included in the where clause. Requires a data type if used as part of a Function or Stored Procedure</param>
        /// <param name="endValueParameter">The second parameter that will be included in the where clause. Requires a data type if used as part of a Function or Stored Procedure</param>
        /// <returns>The query builder that can be used to add parameters to the query, and then further modify the query, or execute the query</returns>
        IBinaryParametersQueryBuilder<TRecord> WhereParameterized(string fieldName, BinarySqlOperand operand, Parameter startValueParameter, Parameter endValueParameter);

        /// <summary>
        /// Adds a parameterised array where clause to the query. This does not add a values for the parameters, and is therefore useful for Functions and Stored Procedures.
        /// Prefer using Where methods from <see cref="QueryBuilderWhereExtensions" /> in normal queries, which allows you to additionally provide a values for the parameters.
        /// </summary>
        /// <param name="fieldName">The name of one of the columns in the query. The where condition will be evaluated against the value of this column.</param>
        /// <param name="operand">The SQL operator to be used in the where clause</param>
        /// <param name="parameterNames">The parameters that will be included in the where clause. Requires data types if used as part of a Function or Stored Procedure</param>
        /// <returns>The query builder that can be used to add parameters to the query, and then further modify the query, or execute the query</returns>
        IArrayParametersQueryBuilder<TRecord> WhereParameterized(string fieldName, ArraySqlOperand operand, IEnumerable<Parameter> parameterNames);
        
        IQueryBuilder<TRecord> WhereNull(string fieldName);
        IQueryBuilder<TRecord> WhereNotNull(string fieldName);

        /// <summary>
        /// Adds an order by clause to the query, where the order by clause will be in the default order (ascending).
        /// If no order by clauses are added to the query, the query will be ordered by the Id column in ascending order.
        /// </summary>
        /// <param name="fieldName">The column that the query should be ordered by</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IOrderedQueryBuilder<TRecord> OrderBy(string fieldName);
        
        /// <summary>
        /// Adds an order by clause to the query using a table alias, where the order by clause will be in the default order (ascending).
        /// If no order by clauses are added to the query, the query will be ordered by the Id column in ascending order.
        /// </summary>
        /// <param name="fieldName">The column that the query should be ordered by</param>
        /// <param name="tableAlias">The alias for where the column exists</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IOrderedQueryBuilder<TRecord> OrderBy(string fieldName, string tableAlias);

        /// <summary>
        /// Adds an order by clause to the query, where the order by clause will be in descending order.
        /// If no order by clauses are explicitly added to the query, the query will be ordered by the Id column in ascending order.
        /// </summary>
        /// <param name="fieldName">The column that the query should be ordered by</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IOrderedQueryBuilder<TRecord> OrderByDescending(string fieldName);
        
        /// <summary>
        /// Adds an order by clause to the query using a table alias, where the order by clause will be in descending order.
        /// If no order by clauses are explicitly added to the query, the query will be ordered by the Id column in ascending order.
        /// </summary>
        /// <param name="fieldName">The column that the query should be ordered by</param>
        /// <param name="tableAlias">The alias for where the column exists</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IOrderedQueryBuilder<TRecord> OrderByDescending(string fieldName, string tableAlias);

        /// <summary>
        /// Adds a column to the column selection for the query.
        /// If no columns are explicitly added to the column selection for the query, all columns will be selected.
        /// </summary>
        /// <param name="name">The column to select</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> Column(string name);

        /// <summary>
        /// Adds a column to the column selection for the query, and aliases the column.
        /// If no columns are explicitly added to the column selection for the query, all columns will be selected.
        /// </summary>
        /// <param name="name">The column to select</param>
        /// <param name="columnAlias">The alias to use for this column</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> Column(string name, string columnAlias);

        /// <summary>
        /// Adds a column to the column selection for the query from a specific table that has been aliased in the query, and then aliases the column.
        /// If no columns are explicitly added to the column selection for the query, all columns will be selected.
        /// </summary>
        /// <param name="name">The column to select</param>
        /// <param name="columnAlias">The alias to use for this column</param>
        /// <param name="tableAlias">The alias of the table from which the column originates</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> Column(string name, string columnAlias, string tableAlias);

        /// <summary>
        /// Explicitly add all columns to the column selection for the query.
        /// By default, all columns are included in the query if no other columns are selected. 
        /// This allows you to explicitly select all columns in addition to other columns (for example, a calculated column)
        /// </summary>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> AllColumns();

        /// <summary>
        /// Adds a calculated column to the column selection for the query.
        /// By default, all columns are included in the query if no other columns are selected. 
        /// </summary>
        /// <param name="expression">The expression that will be used in the SQL string for the calculated column</param>
        /// <param name="columnAlias">The alias for the calculated column</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> CalculatedColumn(string expression, string columnAlias);

        /// <summary>
        /// Change the type of the record returned by the QueryBuilder.
        /// This is useful if the initial type no longer matches the columns returned by the query.
        /// </summary>
        /// <typeparam name="TNewRecord">The new type to use for the query builder</typeparam>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TNewRecord> AsType<TNewRecord>() where TNewRecord : class;

        /// <summary>
        /// Adds a ROW_NUMBER() column to the column selection for the query.
        /// Any order by clauses that have been added to the query so far will be used in the OVER part of the ROW_NUMBER() expression, 
        /// and will not be used in the main Order By section of the SQL string.
        /// By default, all columns are included in the query if no other columns are selected. 
        /// </summary>
        /// <param name="columnAlias">The alias to use for the row number column</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias);

        /// <summary>
        /// Adds a ROW_NUMBER() column to the column selection for the query.
        /// Any order by clauses that have been added to the query so far will be used in the OVER part of the ROW_NUMBER() expression, 
        /// and will not be used in the main Order By section of the SQL string.
        /// By default, all columns are included in the query if no other columns are selected. 
        /// </summary>
        /// <param name="columnAlias">The alias to use for the row number column</param>
        /// <param name="partitionByColumns">The columns to include in the Partition By section of the OVER clause</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias, params string[] partitionByColumns);

        /// <summary>
        /// Adds a ROW_NUMBER() column to the column selection for the query.
        /// Any order by clauses that have been added to the query so far will be used in the OVER part of the ROW_NUMBER() expression, 
        /// and will not be used in the main Order By section of the SQL string.
        /// By default, all columns are included in the query if no other columns are selected. 
        /// </summary>
        /// <param name="columnAlias">The alias to use for the row number column</param>
        /// <param name="partitionByColumns">The columns to include in the Partition By section of the OVER clause</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> AddRowNumberColumn(string columnAlias, params ColumnFromTable[] partitionByColumns);

        /// <summary>
        /// Adds a parameter without a value to the query. 
        /// If this parameter is used as part of a Function or Stored Procedure, a data type must also be provided.
        /// Otherwise, if this forms part of a normal query, use the overload that allows you to additionally provide a value for this parameter.
        /// If you have used the WhereParameterized method to add a where clause, then the Parameter is already included in the query.
        /// </summary>
        /// <param name="parameter">The parameter to add to the query</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> Parameter(Parameter parameter);

        /// <summary>
        /// Provides a default value for a parameter that has already been added to the query. 
        /// Default values only apply when creating a Functions or Stored Procedures.
        /// </summary>
        /// <param name="parameter">The parameter for which the default value applies</param>
        /// <param name="defaultValue">The default value of the parameter</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> ParameterDefault(Parameter parameter, object defaultValue);

        /// <summary>
        /// Provides a value for a parameter that has already been added to the query.
        /// </summary>
        /// <param name="parameter">The parameter for which the value applies</param>
        /// <param name="value">The value of the parameter</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IQueryBuilder<TRecord> Parameter(Parameter parameter, object value);

        /// <summary>
        /// Adds a join to the query.
        /// The query that has been built up so far in this query builder may be converted to a subquery to capture more complex modifications, such as where clauses.
        /// Prefer using methods from <see cref="QueryBuilderJoinExtensions" /> for convenience
        /// </summary>
        /// <param name="source">The source that is being joined to the current query</param>
        /// <param name="joinType">The type of join</param>
        /// <param name="parameterValues">The parameter values from the source that is being joined to the current query</param>
        /// <param name="parameters">The parameters from the source that is being joined to the current query</param>
        /// <param name="parameterDefaults">The default parameter values from the source that is being joined to the current query</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        IJoinSourceQueryBuilder<TRecord> Join(IAliasedSelectSource source, JoinType joinType, CommandParameterValues parameterValues, Parameters parameters, ParameterDefaults parameterDefaults);

        /// <summary>
        /// Unions two queries together
        /// </summary>
        /// <param name="queryBuilder">The query builder for the query that will be unioned with the current query</param>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        ISubquerySourceBuilder<TRecord> Union(IQueryBuilder<TRecord> queryBuilder);

        /// <summary>
        /// Converts the current query into a subquery.
        /// </summary>
        /// <returns>The query builder that can be used to further modify the query, or execute the query</returns>
        ISubquerySourceBuilder<TRecord> Subquery();

        /// <summary>
        /// Used internally. Can also be used for testing, debugging, and for extension methods. Avoid using directly
        /// </summary>
        ISelectBuilder GetSelectBuilder();

        /// <summary>
        /// Used internally. Can also be used for testing, debugging, and for extension methods. Avoid using directly
        /// </summary>
        Parameters Parameters { get; }

        /// <summary>
        /// Used internally. Can also be used for testing, debugging, and for extension methods. Avoid using directly
        /// </summary>
        ParameterDefaults ParameterDefaults { get; }

        /// <summary>
        /// Used internally. Can also be used for testing, debugging, and for extension methods. Avoid using directly
        /// </summary>
        CommandParameterValues ParameterValues { get; }

        /// <summary>
        /// Used for testing and debugging
        /// </summary>
        /// <returns>The row SQL string that well be executed</returns>
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
using System;
using System.Collections.Generic;
using System.Linq;
using Nevermore.Joins;

namespace Nevermore.QueryGraph
{
    // To add
    // Join conditions
    // paginate query
    // delete
    // wherein
    // or
    // parameter collection

    public interface IDelete
    {
        ITableSource From { get; }
        Where Where { get; }
        string GenerateSql();
    }

    public class Delete : IDelete
    {
        public Delete(ITableSource @from, Where @where)
        {
            From = @from;
            Where = @where;
        }

        public ITableSource From { get; }
        public Where Where { get; }

        public string GenerateSql()
        {
            return $"DELETE FROM {From.GenerateSql()}{Where.GenerateSql()}";
        }
    }

    public interface ISelect
    {
        string GenerateSql();
    }

    public interface IRowSelection
    {
        string GenerateSql();
    }

    public class Top : IRowSelection
    {
        readonly int numberOfRows;

        public Top(int numberOfRows)
        {
            this.numberOfRows = numberOfRows;
        }

        public string GenerateSql()
        {
            return $"TOP {numberOfRows} ";
        }
    }

    public class AllRows : IRowSelection
    {
        public string GenerateSql()
        {
            return "";
        }
    }

    public class AggregateSelectColumns : ISelectColumns
    {
        readonly IReadOnlyList<ISelectColumns> columns;

        public AggregateSelectColumns(IReadOnlyList<ISelectColumns> columns)
        {
            this.columns = columns;
        }

        public bool AggregatesRows => columns.Any(c => c.AggregatesRows);
        public string GenerateSql()
        {
            return string.Join(", ", columns.Select(c => c.GenerateSql()));
        }
    }

    public class Union : ISelect
    {
        readonly IReadOnlyList<ISelect> selects;

        public Union(IReadOnlyList<ISelect> selects)
        {
            this.selects = selects;
        }

        public string GenerateSql()
        {
            return string.Join("\r\nUNION\r\n", selects.Select(s => s.GenerateSql()));
        }
    }

    public class Select : ISelect
    {
        public Select(IRowSelection rowSelection, ISelectColumns columns, ISelectSource @from, Where @where, OrderBy orderBy)
        {
            RowSelection = rowSelection;
            Columns = columns;
            From = @from;
            Where = @where;
            OrderBy = orderBy;
        }

        public IRowSelection RowSelection { get; }
        public ISelectColumns Columns { get; }
        public ISelectSource From { get; }
        public Where Where { get; }

        // can be null
        public OrderBy OrderBy { get; }

        public string GenerateSql()
        {
            var orderBy = OrderBy != null ? $" {OrderBy.GenerateSql()}" : string.Empty;
            return $"SELECT {RowSelection.GenerateSql()}{Columns.GenerateSql()} FROM {From.GenerateSql()}{Where.GenerateSql()}{orderBy}";
        }
    }

    public class OrderBy
    {
        readonly IReadOnlyList<OrderByField> fields;

        public OrderBy(IReadOnlyList<OrderByField> fields)
        {
            if (fields.Count < 1) throw new ArgumentException("Fields must have at least one value");
            this.fields = fields;
        }
        public string GenerateSql()
        {
            return $"ORDER BY {string.Join(", ", fields.Select(f => f.GenerateSql()))}";
        }
    }

    public enum OrderByDirection
    {
        Ascending,
        Descending
    }

    // Handle the case where there are no order by fields and we want to use the default order by (i.e. ID column)
    public class OrderByField
    {
        readonly string fieldName;
        public string TableAlias { get; set; }
        public OrderByDirection Direction { get; set; } = OrderByDirection.Ascending;

        public OrderByField(string fieldName)
        {
            this.fieldName = fieldName;
        }

        public string GenerateSql()
        {
            var direction = Direction == OrderByDirection.Descending ? " DESC" : string.Empty;
            var field = TableAlias == null ? $"[{fieldName}]" : $"{TableAlias}.[{fieldName}]";
            return $"{field}{direction}";
        }
    }

    public class Where
    {
        readonly IWhereClause whereClause;

        public Where()
        {
        }

        public Where(IWhereClause whereClause)
        {
            this.whereClause = whereClause;
        }

        public string GenerateSql()
        {
            return whereClause != null ? $" WHERE {whereClause.GenerateSql()}" : string.Empty;
        }
    }

    public interface IWhereClause
    {
        string GenerateSql();
    }

    public interface IWhereFieldReference
    {
        string GenerateSql();
    }

    public class WhereFieldReference : IWhereFieldReference
    {
        readonly string fieldName;

        public WhereFieldReference(string fieldName)
        {
            this.fieldName = fieldName;
        }

        public string GenerateSql()
        {
            return $"[{fieldName}]";
        }
    }

    public class AliasedWhereFieldReference : IWhereFieldReference
    {
        readonly string tableAlias;
        readonly WhereFieldReference fieldReference;

        public AliasedWhereFieldReference(string tableAlias, WhereFieldReference fieldReference)
        {
            this.tableAlias = tableAlias;
            this.fieldReference = fieldReference;
        }

        public string GenerateSql()
        {
            return $"{tableAlias}.{fieldReference.GenerateSql()}";
        }
    }

    public class UnaryWhereClause : IWhereClause
    {
        readonly IWhereFieldReference whereFieldReference;
        readonly UnarySqlOperand operand;
        readonly string parameterName;

        public UnaryWhereClause(IWhereFieldReference whereFieldReference, UnarySqlOperand operand, string parameterName)
        {
            this.whereFieldReference = whereFieldReference;
            this.operand = operand;
            this.parameterName = parameterName;
        }
        public string GenerateSql()
        {
            return $"{whereFieldReference.GenerateSql()} {GetQueryOperandSql()} @{parameterName}";
        }

        string GetQueryOperandSql()
        {
            switch (operand)
            {
                case UnarySqlOperand.Contains:
                case UnarySqlOperand.EndsWith:
                case UnarySqlOperand.StartsWith:
                    return "LIKE";
                case UnarySqlOperand.Equal:
                    return "=";
                case UnarySqlOperand.NotEqual:
                    return "<>";
                case UnarySqlOperand.GreaterThan:
                    return ">";
                case UnarySqlOperand.GreaterThanOrEqual:
                    return ">=";
                case UnarySqlOperand.LessThan:
                    return "<";
                case UnarySqlOperand.LessThanOrEqual:
                    return "<=";
                default:
                    throw new NotSupportedException("Operand " + operand + " is not supported!");
            }
        }
    }

    public class BinaryWhereClause : IWhereClause
    {
        readonly IWhereFieldReference whereFieldReference;
        readonly BinarySqlOperand operand;
        readonly string firstParameterName;
        readonly string secondParameterName;

        public BinaryWhereClause(IWhereFieldReference whereFieldReference, BinarySqlOperand operand, string firstParameterName, string secondParameterName)
        {
            this.whereFieldReference = whereFieldReference;
            this.operand = operand;
            this.firstParameterName = firstParameterName;
            this.secondParameterName = secondParameterName;
        }

        public string GenerateSql()
        {
            return $"{whereFieldReference.GenerateSql()} {GetQueryOperandSql()} @{firstParameterName} AND @{secondParameterName}";
        }

        string GetQueryOperandSql()
        {
            switch (operand)
            {
                case BinarySqlOperand.Between:
                    return "BETWEEN";
                default:
                    throw new NotSupportedException("Operand " + operand + " is not supported!");
            }
        }
    }

    public class ArrayWhereClause : IWhereClause
    {
        readonly IWhereFieldReference whereFieldReference;
        readonly ArraySqlOperand operand;
        readonly IEnumerable<string> parameterNames;

        public ArrayWhereClause(IWhereFieldReference whereFieldReference, ArraySqlOperand operand, IEnumerable<string> parameterNames)
        {
            this.whereFieldReference = whereFieldReference;
            this.operand = operand;
            this.parameterNames = parameterNames;
        }

        public string GenerateSql()
        {
            return $"{whereFieldReference.GenerateSql()} {GetQueryOperandSql()} ({string.Join(", ", parameterNames.Select(p => $"@{p}"))})";
        }

        string GetQueryOperandSql()
        {
            switch (operand)
            {
                case ArraySqlOperand.In:
                    return "IN";
                default:
                    throw new ArgumentOutOfRangeException("Operand " + operand + " is not supported!");
            }
        }
    }

    public class AndClause : IWhereClause
    {
        readonly IReadOnlyList<IWhereClause> subClauses;

        public AndClause(IReadOnlyList<IWhereClause> subClauses)
        {
            this.subClauses = subClauses;
        }

        public string GenerateSql()
        {
            return string.Join(" AND ", subClauses.Select(c => $"({c.GenerateSql()})"));
        }
    }

    public class CustomWhereClause : IWhereClause
    {
        readonly string whereClause;

        public CustomWhereClause(string whereClause)
        {
            this.whereClause = whereClause;
        }

        public string GenerateSql() => whereClause;
    }

    public interface ISelectSource
    {
        string GenerateSql();
    }

    public interface IAliasedSelectSource : ISelectSource
    {
        string Alias { get; }
    }

    public class SelectAllFrom : ISelectColumns
    {
        readonly string tableAlias;

        public SelectAllFrom(string tableAlias)
        {
            this.tableAlias = tableAlias;
        }

        public bool AggregatesRows => false;

        public string GenerateSql()
        {
            return $"{tableAlias}.*";
        }
    }

    public class SelectAllSource : ISelectColumns
    {
        public bool AggregatesRows => false;
        public string GenerateSql() => "*";
    }

    public class SelectCountSource : ISelectColumns
    {
        public bool AggregatesRows => true;
        public string GenerateSql() => "COUNT(*)";
    }

    public class SelectRowNumber : ISelectColumns
    {
        readonly Over over;
        readonly string alias;

        public SelectRowNumber(Over over, string alias)
        {
            this.over = over;
            this.alias = alias;
        }

        public bool AggregatesRows => false;

        public string GenerateSql()
        {
            return $"ROW_NUMBER() {over.GenerateSql()} AS {alias}";
        }
    }

    public class Over
    {
        readonly OrderBy orderBy;
        readonly PartitionBy partitionBy;

        public Over(OrderBy orderBy, PartitionBy partitionBy)
        {
            this.orderBy = orderBy;
            this.partitionBy = partitionBy;
        }

        public string GenerateSql()
        {
            return $"OVER ({(partitionBy == null ? string.Empty : $"{partitionBy.GenerateSql()} ")}{orderBy.GenerateSql()})";
        }
    }

    public class PartitionBy
    {
        readonly IReadOnlyList<IColumn> columns;

        public PartitionBy(IReadOnlyList<IColumn> columns)
        {
            this.columns = columns;
        }

        public string GenerateSql()
        {
            return $"PARTITION BY {string.Join(", ", columns.Select(c => c.GenerateSql()))}";
        }
    }

    public interface ITableSource : ISelectSource
    {
    }

    public interface ISimpleTableSource : ITableSource
    {
    }

    public class NewTableSource : ISimpleTableSource
    {
        readonly string tableOrViewName;

        public NewTableSource(string tableOrViewName)
        {
            this.tableOrViewName = tableOrViewName;
        }

        public string GenerateSql() => $"dbo.[{tableOrViewName}]";
    }

    public class AliasedTableSource : ISimpleTableSource, IAliasedSelectSource
    {
        readonly NewTableSource source;

        public AliasedTableSource(NewTableSource source, string alias)
        {
            this.source = source;
            Alias = alias;
        }

        public string Alias { get; }

        public string GenerateSql() => $"{source.GenerateSql()} {Alias}";
    }

    public class TableSourceWithHint : ITableSource
    {
        readonly ISimpleTableSource tableSource;
        readonly string tableHint;

        public TableSourceWithHint(ISimpleTableSource tableSource, string tableHint)
        {
            this.tableSource = tableSource;
            this.tableHint = tableHint;
        }

        public string GenerateSql()
        {
            return $"{tableSource.GenerateSql()} {tableHint}";
        }
    }

    public class TableSource : IAliasedSelectSource
    {
        public TableSource(string tableOrViewName, string alias = null, string tableHint = null)
        {
            TableOrViewName = tableOrViewName;
            TableHint = tableHint;
            Alias = alias;
        }

        public string TableOrViewName { get; }
        public string Alias { get; }
        public string TableHint { get; }

        public string GenerateSql()
        {
            var aliasExpression = string.IsNullOrEmpty(Alias) ? "" : $" {Alias}";
            var hintExpression = string.IsNullOrEmpty(TableHint) ? "" : $" {TableHint}";
            return $"dbo.[{TableOrViewName}]{aliasExpression}{hintExpression}";
        }
    }

    public class JoinClause
    {
        readonly string leftFieldName;
        readonly JoinOperand operand;
        readonly string rightFieldName;

        public JoinClause(string leftFieldName, JoinOperand operand, string rightFieldName)
        {
            this.leftFieldName = leftFieldName;
            this.operand = operand;
            this.rightFieldName = rightFieldName;
        }

        public string GenerateSql(string leftTableAlias, string rightTableAlias)
        {
            return $"{leftTableAlias}.[{leftFieldName}] {GetQueryOperand()} {rightTableAlias}.[{rightFieldName}]";
        }

        string GetQueryOperand()
        {
            switch (operand)
            {
                case JoinOperand.Equal:
                    return "=";
                default:
                    throw new NotSupportedException("Operand " + operand + " is not supported!");
            }
        }
    }
    
    public class Join
    {
        public Join(IReadOnlyList<JoinClause> clauses, IAliasedSelectSource source, JoinType type)
        {
            Clauses = clauses;
            Source = source;
            Type = type;
        }

        public IAliasedSelectSource Source { get; }
        public JoinType Type { get; }
        public IReadOnlyList<JoinClause> Clauses { get; }

        public string GenerateSql(string leftSourceAlias)
        {
            return $"{GenerateJoinTypeSql(Type)} {Source.GenerateSql()} ON {string.Join(" AND ", Clauses.Select(c => c.GenerateSql(leftSourceAlias, Source.Alias)))}";
        }

        string GenerateJoinTypeSql(JoinType joinType)
        {
            switch (joinType)
            {
                case JoinType.InnerJoin:
                    return "INNER JOIN";
                case JoinType.LeftHashJoin:
                    return "LEFT HASH JOIN";
                default:
                    throw new NotSupportedException($"Join {joinType} is not supported");
            }
        }
    }

    public class JoinedSource : ISelectSource
    {
        public Join LastJoin { get; }
        public IAliasedSelectSource Source { get; }
        public IReadOnlyList<Join> IntermediateJoins { get; }

        public JoinedSource(IAliasedSelectSource source, IReadOnlyList<Join> intermediateJoins, Join lastJoin) // todo, don't need intermediate + last joins here
        {
            this.LastJoin = lastJoin;
            IntermediateJoins = intermediateJoins;
            Source = source;
        }

        public JoinedSource AddClause(JoinClause clause)
        {
            return new JoinedSource(Source, IntermediateJoins, new Join(LastJoin.Clauses.Concat(new [] {clause}).ToList(), LastJoin.Source, LastJoin.Type));
        }

        public string GenerateSql()
        {
            var sourceParts = new [] {Source.GenerateSql()}.Concat(IntermediateJoins.Concat(new [] {LastJoin}).Select(j => j.GenerateSql(Source.Alias)));
            return string.Join("\r\n", sourceParts);
        }
    }

    public interface ISubquerySource : IAliasedSelectSource
    {
    }

    public class SubquerySource : ISubquerySource
    {

        public SubquerySource(ISelect select, string alias) // todo: alias can't be null. Subqueries NEED an alias
        {
            Alias = alias;
            Source = select;
        }

        public ISelect Source { get; }
        public string Alias { get; }

        public string GenerateSql()
        {
            var alias = string.IsNullOrEmpty(Alias) ? string.Empty : $" {Alias}";
            return $"({Source.GenerateSql()}){alias}";
        }
    }

    public interface ISelectColumns
    {
        bool AggregatesRows { get; }
        string GenerateSql();
    }

    public interface IColumn : ISelectColumns
    {
    }

    public class Column : IColumn
    {
        readonly string columnName;

        public Column(string columnName)
        {
            this.columnName = columnName;
        }

        public bool AggregatesRows => false;
        public string GenerateSql() => $"[{columnName}]";
    }

    public class TableColumn : IColumn
    {
        readonly Column column;
        readonly string tableAlias;

        public TableColumn(Column column, string tableAlias)
        {
            this.column = column;
            this.tableAlias = tableAlias;
        }

        public bool AggregatesRows => false;
        public string GenerateSql() => $"{tableAlias}.{column.GenerateSql()}";
    }

    public class AliasedColumn : ISelectColumns
    {
        readonly IColumn column;
        readonly string columnAlias;

        public AliasedColumn(IColumn column, string columnAlias)
        {
            this.column = column;
            this.columnAlias = columnAlias;
        }

        public bool AggregatesRows => false;
        public string GenerateSql() => $"{column.GenerateSql()} AS [{columnAlias}]";
    }

    public class CalculatedColumn : IColumn
    {
        readonly string expression;
        readonly string alias;

        public CalculatedColumn(string expression, string alias)
        {
            this.expression = expression;
            this.alias = alias;
        }

        public bool AggregatesRows => false;
        public string GenerateSql()
        {
            return $"{expression} AS [{alias}]";
        }
    }
}
using System.Collections.Generic;
using System.Linq;

namespace Nevermore.AST
{
    public interface ISelectColumns
    {
        bool AggregatesRows { get; }
        string GenerateSql();
    }

    public interface IColumn : ISelectColumns
    {
    }

    public class AggregateSelectColumns : ISelectColumns
    {
        readonly IReadOnlyList<ISelectColumns> columns;

        public AggregateSelectColumns(IReadOnlyList<ISelectColumns> columns)
        {
            this.columns = columns;
        }

        public bool AggregatesRows => columns.Any(c => c.AggregatesRows);

        public string GenerateSql() => string.Join(", ", columns.Select(c => c.GenerateSql()));
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

    public class SelectAllFrom : ISelectColumns
    {
        readonly string tableAlias;

        public SelectAllFrom(string tableAlias)
        {
            this.tableAlias = tableAlias;
        }

        public bool AggregatesRows => false;

        public string GenerateSql() => $"{tableAlias}.*";
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

        public string GenerateSql() => $"ROW_NUMBER() {over.GenerateSql()} AS {alias}";
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
        public string GenerateSql() => $"{expression} AS [{alias}]";
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
}
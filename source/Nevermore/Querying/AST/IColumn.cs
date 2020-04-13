namespace Nevermore.AST
{
    public interface IColumn : ISelectColumns
    {
    }

    public class CalculatedColumn : IColumn
    {
        readonly IExpression expression;

        public CalculatedColumn(IExpression expression)
        {
            this.expression = expression;
        }

        public bool AggregatesRows => false;
        public string GenerateSql() => $"{expression.GenerateSql()}";
        public override string ToString() => GenerateSql();
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
        public override string ToString() => GenerateSql();
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
        public override string ToString() => GenerateSql();
    }
}
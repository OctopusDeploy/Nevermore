namespace Nevermore.Querying.AST
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
        public readonly Column Column;
        public readonly string TableAlias;

        public TableColumn(Column column, string tableAlias)
        {
            this.Column = column;
            this.TableAlias = tableAlias;
        }

        public bool AggregatesRows => false;
        public string GenerateSql() => $"{TableAlias}.{Column.GenerateSql()}";
        public override string ToString() => GenerateSql();
    }

    public class SelectCountSource : IColumn
    {
        public bool AggregatesRows => true;
        public string GenerateSql() =>  "COUNT(*)";
        public override string ToString() => GenerateSql();
    }

    public class JsonQueryColumn : IColumn
    {
        readonly string jsonPath;
        public bool AggregatesRows => false;

        public JsonQueryColumn(string jsonPath)
        {
            this.jsonPath = jsonPath;
        }

        public string GenerateSql() => $"JSON_QUERY([JSON], '{jsonPath}')";
        public override string ToString() => GenerateSql();
    }

    public class JsonValueColumn : IColumn
    {
        readonly string jsonPath;
        public bool AggregatesRows => false;

        public JsonValueColumn(string jsonPath)
        {
            this.jsonPath = jsonPath;
        }

        public string GenerateSql() => $"JSON_VALUE([JSON], '{jsonPath}')";
        public override string ToString() => GenerateSql();
    }
}
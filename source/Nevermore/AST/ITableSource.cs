namespace Nevermore.AST
{
    public interface ITableSource : ISelectSource
    {
    }

    public interface ISimpleTableSource : ITableSource
    {
    }

    public class SimpleTableSource : ISimpleTableSource
    {
        readonly string tableOrViewName;

        public SimpleTableSource(string tableOrViewName)
        {
            this.tableOrViewName = tableOrViewName;
        }

        public string GenerateSql() => $"dbo.[{tableOrViewName}]";
    }

    public class AliasedTableSource : ISimpleTableSource, IAliasedSelectSource
    {
        readonly SimpleTableSource source;

        public AliasedTableSource(SimpleTableSource source, string alias)
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
}
using Nevermore.Advanced;

namespace Nevermore.Querying.AST
{
    public interface ITableSource : ISelectSource
    {
        string TableName { get; }
    }

    public interface ISimpleTableSource : ITableSource
    {
    }

    public class SimpleTableSource : ISimpleTableSource
    {
        readonly string tableOrViewName;
        readonly string schemaName;

        public SimpleTableSource(string tableOrViewName, string schemaName)
        {
            this.tableOrViewName = tableOrViewName;
            this.schemaName = schemaName;
        }

        public string Schema => schemaName;
        public string TableName => tableOrViewName;

        public string GenerateSql() => $"[{schemaName}].[{tableOrViewName}]";
        public override string ToString() => GenerateSql();
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

        public string Schema => source.Schema;
        public string TableName => source.TableName;

        public string GenerateSql() => $"{source.GenerateSql()} {Alias}";
        public override string ToString() => GenerateSql();
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

        public string Schema => tableSource.Schema;
        public string TableName => tableSource.TableName;

        public string GenerateSql()
        {
            return $"{tableSource.GenerateSql()} {tableHint}";
        }

        public override string ToString() => GenerateSql();
    }
}
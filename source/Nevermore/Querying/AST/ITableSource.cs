using System;
using Nevermore.Advanced;

namespace Nevermore.Querying.AST
{
    public interface ITableSource : ISelectSource
    {
        string TableName { get; }
        string[] ColumnNames { get; }
    }

    public interface ISimpleTableSource : ITableSource
    {
    }

    public class SchemalessTableSource : ISimpleTableSource
    {
        public string Schema => throw new InvalidOperationException("Schemaless Tables do not have a schema");
        public string GenerateSql() => $"[{TableName}]";

        public SchemalessTableSource(string tableOrViewName)
        {
            TableName = tableOrViewName;
        }
        
        public string TableName { get; }
        public string[] ColumnNames => Array.Empty<string>();
    }

    public class SimpleTableSource : ISimpleTableSource
    {
        public SimpleTableSource(string tableOrViewName, string schemaName, string[] columnNames)
        {
            TableName = tableOrViewName;
            Schema = schemaName;
            ColumnNames = columnNames;
        }

        public string Schema { get; }

        public string TableName { get; }
        
        public string[] ColumnNames { get; }

        public string GenerateSql() => $"[{Schema}].[{TableName}]";
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
        public string[] ColumnNames => source.ColumnNames;

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
        public string[] ColumnNames => tableSource.ColumnNames;

        public string GenerateSql()
        {
            return $"{tableSource.GenerateSql()} {tableHint}";
        }

        public override string ToString() => GenerateSql();
    }
}
using System;

namespace Nevermore.Querying.AST
{
    public class CteSelectSource : IAliasedSelectSource
    {
        
        readonly ISelect source;
        readonly ISelectBuilder inner;

        public CteSelectSource(ISelect source, string alias, ISelectBuilder inner)
        {
            Alias = alias;
            this.source = source;
            this.inner = inner;
        }

        public string Alias { get; }

        public string Schema => source.Schema;

        public string GenerateSql()
        {
            var alias = string.IsNullOrEmpty(Alias) ? string.Empty : $" {Alias}";
            return $@"With {alias} as (
{Format.IndentLines(source.GenerateSql())}
){Environment.NewLine}{inner.GenerateSelect()}";
        }

        public override string ToString() => GenerateSql();
    }
}
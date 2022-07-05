using System;

namespace Nevermore.Querying.AST
{
    public class CteSelectSource : IAliasedSelectSource
    {
        
        readonly ISelect cteSelect;
        readonly ISelect querySelect;

        public CteSelectSource(ISelect cteSelect, string alias, ISelect querySelect)
        {
            Alias = alias;
            this.cteSelect = cteSelect;
            this.querySelect = querySelect;
        }

        public string Alias { get; }

        public string Schema => cteSelect.Schema;

        public string GenerateSql()
        {
            return $@"With {Alias} as (
{Format.IndentLines(cteSelect.GenerateSql())}
){Environment.NewLine}{querySelect.GenerateSql()}";
        }

        public override string ToString() => GenerateSql();
    }
}
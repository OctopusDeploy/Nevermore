namespace Nevermore.AST
{
    public interface ISubquerySource : IAliasedSelectSource
    {
        ISelect InnerSelect { get; }
    }

    public class SubquerySource : ISubquerySource
    {
        public SubquerySource(ISelect select, string alias)
        {
            Alias = alias;
            InnerSelect = select;
        }

        public string Alias { get; }
        public ISelect InnerSelect { get; }

        public string GenerateSql()
        {
            var alias = string.IsNullOrEmpty(Alias) ? string.Empty : $" {Alias}";
            return $@"(
{Format.IndentLines(InnerSelect.GenerateSql())}
){alias}";
        }

        public override string ToString() => GenerateSql();
    }
}
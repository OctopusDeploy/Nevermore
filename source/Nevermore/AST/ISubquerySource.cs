namespace Nevermore.AST
{
    public interface ISubquerySource : IAliasedSelectSource
    {
    }

    public class SubquerySource : ISubquerySource
    {
        readonly ISelect source;

        public SubquerySource(ISelect select, string alias)
        {
            Alias = alias;
            source = select;
        }

        public string Alias { get; }

        public string GenerateSql()
        {
            var alias = string.IsNullOrEmpty(Alias) ? string.Empty : $" {Alias}";
            return $@"(
{Format.IndentLines(source.GenerateSql())}
){alias}";
        }

        public override string ToString() => GenerateSql();
    }
}
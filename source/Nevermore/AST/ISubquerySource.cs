namespace Nevermore.AST
{
    public interface ISubquerySource : IAliasedSelectSource
    {
    }

    public class SubquerySource : ISubquerySource
    {
        public SubquerySource(ISelect select, string alias) // todo: alias can't be null. Subqueries NEED an alias
        {
            Alias = alias;
            Source = select;
        }

        public ISelect Source { get; }
        public string Alias { get; }

        public string GenerateSql()
        {
            var alias = string.IsNullOrEmpty(Alias) ? string.Empty : $" {Alias}";
            return $"({Source.GenerateSql()}){alias}";
        }
    }
}
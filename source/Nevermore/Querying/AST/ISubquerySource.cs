namespace Nevermore.Querying.AST
{
    public interface ISubquerySource : IAliasedSelectSource
    {
        ISelect Select { get; }
    }

    public class SubquerySource : ISubquerySource
    {
        public SubquerySource(ISelect select, string alias)
        {
            Select = select;
            Alias = alias;
        }

        public string Alias { get; }

        public string Schema
        {
            get => Select.Schema;
        }

        public string GenerateSql()
        {
            var alias = string.IsNullOrEmpty(Alias) ? string.Empty : $" {Alias}";
            return $@"(
{Format.IndentLines(Select.GenerateSql())}
){alias}";
        }

        public override string ToString() => GenerateSql();
        public ISelect Select { get; }
    }
}
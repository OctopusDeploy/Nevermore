namespace Nevermore.AST
{
    public class TableSource : IAliasedSelectSource
    {
        public TableSource(string tableOrViewName, string alias = null, string tableHint = null)
        {
            TableOrViewName = tableOrViewName;
            TableHint = tableHint;
            Alias = alias;
        }

        public string TableOrViewName { get; }
        public string Alias { get; }
        public string TableHint { get; }

        public string GenerateSql()
        {
            var aliasExpression = string.IsNullOrEmpty(Alias) ? "" : $" {Alias}";
            var hintExpression = string.IsNullOrEmpty(TableHint) ? "" : $" {TableHint}";
            return $"dbo.[{TableOrViewName}]{aliasExpression}{hintExpression}";
        }
    }
}
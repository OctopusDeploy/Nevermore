using Nevermore.Advanced;

namespace Nevermore.Querying.AST
{
    public class View
    {
        readonly ISelect select;
        readonly string viewName;
        readonly string schemaName;

        public View(ISelect select, string viewName, string schemaName)
        {
            this.select = @select;
            this.viewName = viewName;
            this.schemaName = schemaName;
        }

        public string GenerateSql()
        {
            return $@"CREATE VIEW [{schemaName}].[{viewName}] AS
{Format.IndentLines(select.GenerateSql())}";
        }
    }
}
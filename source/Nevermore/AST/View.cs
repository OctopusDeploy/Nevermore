namespace Nevermore.AST
{
    public class View
    {
        readonly ISelect select;
        readonly string viewName;

        public View(ISelect select, string viewName)
        {
            this.select = @select;
            this.viewName = viewName;
        }

        public string GenerateSql()
        {
            return $@"CREATE VIEW dbo.[{viewName}] AS
{Format.IndentLines(select.GenerateSql())}";
        }
    }
}
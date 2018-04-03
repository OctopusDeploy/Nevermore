namespace Nevermore.AST
{
    public class Delete
    {
        readonly ITableSource from;
        readonly Where where;

        public Delete(ITableSource from, Where @where)
        {
            this.from = from;
            this.where = where;
        }

        public string GenerateSql() => $"DELETE FROM {from.GenerateSql()}{where.GenerateSql()}";
    }
}
namespace Nevermore.Querying.AST
{
    public class Select : ISelect
    {
        readonly IRowSelection rowSelection;
        readonly ISelectColumns columns;
        readonly ISelectSource from;
        readonly Where where;
        readonly OrderBy orderBy; // Can be null

        public Select(IRowSelection rowSelection, ISelectColumns columns, ISelectSource from, Where where, OrderBy orderBy)
        {
            this.rowSelection = rowSelection;
            this.columns = columns;
            this.from = from;
            this.where = where;
            this.orderBy = orderBy;
        }

        public string Schema => @from.Schema;

        public string GenerateSql()
        {
            var orderByString = orderBy != null ? $@"
{orderBy.GenerateSql()}" : string.Empty;
            return $@"SELECT {rowSelection.GenerateSql()}{columns.GenerateSql()}
FROM {from.GenerateSql()}{where.GenerateSql()}{orderByString}";
        }

        public override string ToString()
        {
            return GenerateSql();
        }
    }
}
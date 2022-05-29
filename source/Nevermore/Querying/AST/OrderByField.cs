namespace Nevermore.Querying.AST
{
    public class OrderByField
    {
        public readonly IColumn Column;
        public readonly OrderByDirection Direction;

        public OrderByField(IColumn column, OrderByDirection direction = OrderByDirection.Ascending)
        {
            this.Column = column;
            this.Direction = direction;
        }

        public string GenerateSql()
        {
            return $"{Column.GenerateSql()}{(Direction == OrderByDirection.Descending ? " DESC" : string.Empty)}";
        }

        public override string ToString() => GenerateSql();
    }
}
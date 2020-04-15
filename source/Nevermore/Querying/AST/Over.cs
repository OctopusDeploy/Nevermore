namespace Nevermore.Querying.AST
{
    public class Over
    {
        readonly OrderBy orderBy;
        readonly PartitionBy partitionBy; // Can be null

        public Over(OrderBy orderBy, PartitionBy partitionBy)
        {
            this.orderBy = orderBy;
            this.partitionBy = partitionBy;
        }

        public string GenerateSql() => $"OVER ({(partitionBy == null ? string.Empty : $"{partitionBy.GenerateSql()} ")}{orderBy.GenerateSql()})";
    }
}
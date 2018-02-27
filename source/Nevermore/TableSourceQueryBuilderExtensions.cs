namespace Nevermore
{
    public static class TableSourceQueryBuilderExtensions
    {
        public static IQueryBuilder<TRecord> NoLock<TRecord>(this ITableSourceQueryBuilder<TRecord> queryBuilder)
        {
            return queryBuilder.Hint("NOLOCK");
        }
    }
}
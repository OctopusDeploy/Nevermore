namespace Nevermore
{
    public static class TableSourceQueryBuilderExtensions
    {
        public static IQueryBuilder<TRecord> NoLock<TRecord>(this ITableSourceQueryBuilder<TRecord> queryBuilder) where TRecord : class
        {
            return queryBuilder.Hint("NOLOCK");
        }
    }
}
namespace Nevermore
{
    public static class OrderedQueryBuilderExtensions
    {
        public static IOrderedQueryBuilder<TRecord> ThenBy<TRecord>(this IOrderedQueryBuilder<TRecord> queryBuilder,
            string orderByClause) where TRecord : class
        {
            return queryBuilder.OrderBy(orderByClause);
        }

        public static IOrderedQueryBuilder<TRecord> ThenByDescending<TRecord>(
            this IOrderedQueryBuilder<TRecord> queryBuilder, string orderByClause) where TRecord : class
        {
            return queryBuilder.OrderByDescending(orderByClause);
        }
    }
}
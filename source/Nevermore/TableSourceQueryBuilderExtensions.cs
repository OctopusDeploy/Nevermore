namespace Nevermore
{
    public static class TableSourceQueryBuilderExtensions
    {
        /// <summary>
        /// Adds a "NOLOCK" table hint to the table source of the query
        /// </summary>
        /// <typeparam name="TRecord">The record type of the query builder</typeparam>
        /// <param name="queryBuilder">The query builder</param>
        /// <returns>A plain SQL string representing a create stored procedure query</returns>
        public static IQueryBuilder<TRecord> NoLock<TRecord>(this ITableSourceQueryBuilder<TRecord> queryBuilder) where TRecord : class
        {
            return queryBuilder.Hint("NOLOCK");
        }
    }
}
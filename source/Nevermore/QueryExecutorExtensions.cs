using Nevermore.Contracts;

namespace Nevermore
{
    public static class QueryExecutorExtensions
    {
        /// <summary>
        /// Creates a query that returns strongly typed documents.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <returns>A stream of resulting documents.</returns>
        public static IQueryBuilder<TDocument> Query<TDocument>(this IQueryExecutor queryExecutor) where TDocument : class, IId
        {
            return queryExecutor.TableQuery<TDocument>()
                // AsType creates an instance `QueryBuilder` without actually modifying the query itself.
                // This allows any changes to the query (eg by calling `queryBuild.Where(...)`) to modify the state of the query builder itself
                .AsType<TDocument>();
        }
    }
}
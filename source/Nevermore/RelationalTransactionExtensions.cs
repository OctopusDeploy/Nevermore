using Nevermore.Contracts;

namespace Nevermore
{
    public static class RelationalTransactionExtensions
    {
        public static IQueryBuilder<TDocument> Query<TDocument>(this IRelationalTransaction transaction) where TDocument : class, IId
        {
            return transaction.TableQuery<TDocument>()
                // AsType creates an instance `QueryBuilder` without actually modifying the query itself.
                // This allows any changes to the query (eg by calling `queryBuild.Where(...)`) to modify the state of the query builder itself
                .AsType<TDocument>();
        }
    }
}
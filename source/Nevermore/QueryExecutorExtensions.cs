using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
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

        /// <summary>
        /// Loads a single document given its ID. If the item is not found, returns <c>null</c>.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <param name="id">The <c>Id</c> of the document to find.</param>
        /// <param name="queryExecutor">The query executor to use</param>
        /// <returns>The document, or <c>null</c> if the document is not found.</returns>
        [Pure]
        public static T Load<T>(this IQueryExecutor queryExecutor, string id) where T : class, IId
        {
            return queryExecutor.TableQuery<T>()
                .Where("[Id] = @id")
                .Parameter("id", id)
                .First();
        }
        
        /// <summary>
        /// Loads a set of documents by their ID's. Documents that are not found are excluded from the result list (that is,
        /// the results may contain less items than the number of ID's queried for).
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <param name="ids">A collection of ID's to query by.</param>
        /// <param name="queryExecutor">The query executor to use</param>
        /// <returns>The documents.</returns>
        [Pure]
        public static T[] Load<T>(this IQueryExecutor queryExecutor, IEnumerable<string> ids) where T : class, IId
            => queryExecutor.LoadStream<T>(ids).ToArray();
        
        /// <summary>
        /// Loads a set of documents by their ID's. Documents that are not found are excluded from the result list (that is,
        /// the results may contain less items than the number of ID's queried for).
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <param name="ids">A collection of ID's to query by.</param>
        /// <param name="queryExecutor">The query executor to use</param>
        /// <returns>The documents as a lazy loaded stream.</returns>
        [Pure]
        public static IEnumerable<T> LoadStream<T>(this IQueryExecutor queryExecutor, IEnumerable<string> ids) where T : class, IId
        {
            var blocks = ids
                .Distinct()
                .Select((id, index) => (id: id, index: index))
                .GroupBy(x => x.index / 500, y => y.id)
                .ToArray();

            foreach (var block in blocks)
            {
                var results = queryExecutor.TableQuery<T>()
                    .Where("[Id] IN @ids")
                    .Parameter("ids", block.ToArray())
                    .Stream();

                foreach (var result in results)
                    yield return result;
            }
        }

        /// <summary>
        /// Loads a single document given its ID. If the item is not found, throws a <see cref="ResourceNotFoundException" />.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <param name="id">The <c>Id</c> of the document to find.</param>
        /// <param name="queryExecutor">The query executor to use</param>
        /// <returns>The document, or <c>null</c> if the document is not found.</returns>
        [Pure]
        public static T LoadRequired<T>(this IQueryExecutor queryExecutor, string id) where T : class, IId
        {
            var result = queryExecutor.Load<T>(id);
            if (result == null)
                throw new ResourceNotFoundException(id);
            return result;
        }

        /// <summary>
        /// Loads a set of documents by their ID's. If any of the documents are not found, a
        /// <see cref="ResourceNotFoundException" /> will be thrown.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <param name="ids">A collection of ID's to query by.</param>
        /// <param name="queryExecutor">The query executor to use</param>
        /// <returns>The documents.</returns>
        [Pure]
        public static T[] LoadRequired<T>(this IQueryExecutor queryExecutor, IEnumerable<string> ids) where T : class, IId
        {
            var allIds = ids.ToArray();
            var results = queryExecutor.TableQuery<T>()
                .Where("[Id] IN @ids")
                .Parameter("ids", allIds)
                .Stream().ToArray();

            var items = allIds.Zip(results, Tuple.Create);
            foreach (var pair in items)
                if (pair.Item2 == null)
                    throw new ResourceNotFoundException(pair.Item1);
            return results;
        }
    }
}
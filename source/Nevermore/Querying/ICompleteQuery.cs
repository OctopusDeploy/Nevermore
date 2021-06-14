using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Querying
{
    public interface ICompleteQuery<TRecord> where TRecord : class
    {
        /// <summary>
        /// Executes the query, and counts the number of rows.
        /// Any order by clauses will be ignored
        /// </summary>
        /// <returns>The number of rows in the result set</returns>
        [Pure] int Count();
        
        /// <summary>
        /// Executes the query, and counts the number of rows.
        /// Any order by clauses will be ignored
        /// </summary>
        /// <returns>The number of rows in the result set</returns>
        [Pure] Task<int> CountAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the query and determines if there are any rows
        /// </summary>
        /// <returns>Returns true if there are any rows in the result set, otherwise false</returns>
        [Pure] bool Any();

        /// <summary>
        /// Executes the query and determines if there are any rows
        /// </summary>
        /// <returns>Returns true if there are any rows in the result set, otherwise false</returns>
        [Pure] Task<bool> AnyAsync(CancellationToken cancellationToken = default);

        [Obsolete("First returns the first row, or null if there are no rows. To make your code easier to read, use FirstOrDefault instead.")]
        [Pure] TRecord First();

        /// <summary>
        /// Executes the query and returns the first row, or null if there are no rows
        /// </summary>
        [Pure] TRecord FirstOrDefault();
        
        /// <summary>
        /// Executes the query and returns the first row, or null if there are no rows
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the command.</param>
        [Pure] Task<TRecord> FirstOrDefaultAsync(CancellationToken cancellationToken = default);
        
        /// <summary>
        ///  Executes the query and returns a distinct set of results based on the columns included included
        /// </summary>
        /// <returns>The distinct set of results from the column projection</returns>
        [Pure] IEnumerable<TRecord> Distinct();

        /// <summary>
        ///  Executes the query and returns a distinct set of results based on the columns included
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the command.</param>
        /// <returns>The distinct set of results from the column projection</returns>
        [Pure] IAsyncEnumerable<TRecord> DistinctAsync(CancellationToken cancellationToken = default);

        /// <summary>
        ///  Executes the query and returns the specified number of rows
        /// </summary>
        /// <param name="take">The number of rows to return</param>
        /// <returns>The specified number of rows from the start of the result set</returns>
        [Pure] IEnumerable<TRecord> Take(int take);

        /// <summary>
        ///  Executes the query and returns the specified number of rows
        /// </summary>
        /// <param name="take">The number of rows to return</param>
        /// <param name="cancellationToken">Token used to cancel the command.</param>
        /// <returns>The specified number of rows from the start of the result set</returns>
        [Pure] IAsyncEnumerable<TRecord> TakeAsync(int take, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the query and returns the specified number of rows, after first skipping a specified number of rows.
        /// The rows are completely enumerated and stored in a List in memory.
        /// </summary>
        /// <param name="skip">The number of rows to skip before starting to return rows</param>
        /// <param name="take">The number of rows to return</param>
        /// <returns>The specified number of rows taken from the result set, after first skipping the specified number of rows</returns>
        [Pure] List<TRecord> ToList(int skip, int take);

        /// <summary>
        /// Executes the query and returns the specified number of rows, after first skipping a specified number of rows.
        /// The rows are completely enumerated and stored in a List in memory.
        /// </summary>
        /// <param name="skip">The number of rows to skip before starting to return rows</param>
        /// <param name="take">The number of rows to return</param>
        /// <param name="cancellationToken">Token used to cancel the command.</param>
        /// <returns>The specified number of rows taken from the result set, after first skipping the specified number of rows</returns>
        [Pure] Task<List<TRecord>> ToListAsync(int skip, int take, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the query and returns the specified number of rows, after first skipping a specified number of rows.
        /// Additionally executes the query a second time to determine the total number of available rows.
        /// The rows are completely enumerated and stored in a List in memory.
        /// </summary>
        /// <param name="skip">The number of rows to skip before starting to return rows</param>
        /// <param name="take">The number of rows to return</param>
        /// <param name="totalResults">The total number of available rows</param>
        /// <returns>The specified number of rows taken from the result set, after first skipping the specified number of rows</returns>
        [Pure] List<TRecord> ToList(int skip, int take, out int totalResults);

        /// <summary>
        /// Executes the query and returns the specified number of rows, after first skipping a specified number of rows.
        /// Additionally executes the query a second time to determine the total number of available rows.
        /// The rows are completely enumerated and stored in a List in memory.
        /// </summary>
        /// <param name="skip">The number of rows to skip before starting to return rows</param>
        /// <param name="take">The number of rows to return</param>
        /// <param name="cancellationToken">Token to use to cancel the command.</param>
        /// <returns>The specified number of rows taken from the result set, after first skipping the specified number of rows</returns>
        [Pure] Task<(List<TRecord>, int)> ToListWithCountAsync(int skip, int take, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the query and returns all of the rows.
        /// The rows are completely enumerated and stored in a List in memory.
        /// </summary>
        /// <returns>All of the rows from the result set</returns>
        [Pure] List<TRecord> ToList();
        
        /// <summary>
        /// Executes the query and returns all of the rows.
        /// The rows are completely enumerated and stored in a List in memory.
        /// </summary>
        /// <param name="cancellationToken">Token to use to cancel the command.</param>
        /// <returns>All of the rows from the result set</returns>
        [Pure] Task<List<TRecord>> ToListAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the query and returns all of the rows.
        /// The rows are completely enumerated and stored in a Array in memory.
        /// </summary>
        /// <returns>All of the rows from the result set</returns>
        [Pure] TRecord[] ToArray();

        /// <summary>
        /// Executes the query and streams the rows.
        /// The rows are not enumerated up front and are not all stored in memory at the same time.
        /// This is useful when executing an unbounded query that will produce a large result set.
        /// </summary>
        /// <returns>An IEnumerable that can be used to enumerate through all of the rows in the result set</returns>
        [Pure] IEnumerable<TRecord> Stream();

        /// <summary>
        /// Executes the query and streams the rows.
        /// The rows are not enumerated up front and are not all stored in memory at the same time.
        /// This is useful when executing an unbounded query that will produce a large result set.
        /// </summary>
        /// <param name="cancellationToken">Token used to cancel the query.</param>
        /// <returns>An IEnumerable that can be used to enumerate through all of the rows in the result set</returns>
        [Pure] IAsyncEnumerable<TRecord> StreamAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes the query and converts the result to dictionary.
        /// </summary>
        /// <param name="keySelector">Defines how to select a key for the dictionary from a row</param>
        /// <returns>A dictionary that maps the specified keys to rows from the result set</returns>
        [Pure] IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector);

        /// <summary>
        /// Executes the query and converts the result to dictionary.
        /// </summary>
        /// <param name="keySelector">Defines how to select a key for the dictionary from a row</param>
        /// <param name="cancellationToken">Token used to cancel the query.</param>
        /// <returns>A dictionary that maps the specified keys to rows from the result set</returns>
        [Pure] Task<IDictionary<string, TRecord>> ToDictionaryAsync(Func<TRecord, string> keySelector, CancellationToken cancellationToken = default);
    }
}
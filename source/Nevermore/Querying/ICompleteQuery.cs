using System;
using System.Collections.Generic;

namespace Nevermore.Querying
{
    public interface ICompleteQuery<TRecord> where TRecord : class
    {
        /// <summary>
        /// Executes the query, and counts the number of rows.
        /// Any order by clauses will be ignored
        /// </summary>
        /// <returns>The number of rows in the result set</returns>
        int Count();

        /// <summary>
        /// Executes the query and determines if there are any rows
        /// </summary>
        /// <returns>Returns true if there are any rows in the result set, otherwise false</returns>
        bool Any();

        [Obsolete("First returns the first row, or null if there are no rows. To make your code easier to read, use FirstOrDefault instead.")]
        TRecord First();

        /// <summary>
        /// Executes the query and returns the first row, or null if there are no rows
        /// </summary>
        TRecord FirstOrDefault();

        /// <summary>
        ///  Executes the query and returns the specified number of rows
        /// </summary>
        /// <param name="take">The number of rows to return</param>
        /// <returns>The specified number of rows from the start of the result set</returns>
        IEnumerable<TRecord> Take(int take);

        /// <summary>
        /// Executes the query and returns the specified number of rows, after first skipping a specified number of rows.
        /// The rows are completely enumerated and stored in a List in memory.
        /// </summary>
        /// <param name="skip">The number of rows to skip before starting to return rows</param>
        /// <param name="take">The number of rows to return</param>
        /// <returns>The specified number of rows taken from the result set, after first skipping the specified number of rows</returns>
        List<TRecord> ToList(int skip, int take);

        /// <summary>
        /// Executes the query and returns the specified number of rows, after first skipping a specified number of rows.
        /// Additionally executes the query a second time to determine the total number of available rows.
        /// The rows are completely enumerated and stored in a List in memory.
        /// </summary>
        /// <param name="skip">The number of rows to skip before starting to return rows</param>
        /// <param name="take">The number of rows to return</param>
        /// <param name="totalResults">The total number of available rows</param>
        /// <returns>The specified number of rows taken from the result set, after first skipping the specified number of rows</returns>
        List<TRecord> ToList(int skip, int take, out int totalResults);

        /// <summary>
        /// Executes the query and returns all of the rows.
        /// The rows are completely enumerated and stored in a List in memory.
        /// </summary>
        /// <returns>All of the rows from the result set</returns>
        List<TRecord> ToList();

        /// <summary>
        /// Executes the query and returns all of the rows.
        /// The rows are completely enumerated and stored in a Array in memory.
        /// </summary>
        /// <returns>All of the rows from the result set</returns>
        TRecord[] ToArray();

        /// <summary>
        /// Executes the query and streams the rows.
        /// The rows are not enumerated up front and are not all stored in memory at the same time.
        /// This is useful when executing an unbounded query that will produce a large result set.
        /// </summary>
        /// <returns>An IEnumerable that can be used to enumerate through all of the rows in the result set</returns>
        IEnumerable<TRecord> Stream();

        /// <summary>
        /// Executes the query and converts the result to dictionary.
        /// </summary>
        /// <param name="keySelector">Defines how to select a key for the dictionary from a row</param>
        /// <returns>A dictionary that maps the specified keys to rows from the result set</returns>
        IDictionary<string, TRecord> ToDictionary(Func<TRecord, string> keySelector);
    }
}
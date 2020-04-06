using System;
using System.Collections.Generic;
using System.Data;
using Nevermore.Contracts;

namespace Nevermore
 {
     public interface IReadQueryExecutor
     {
         /// <summary>
         /// Executes a query that returns a scalar value (e.g., an INSERT or UPDATE query that returns the number of rows, or a
         /// SELECT query that returns a count).
         /// </summary>
         /// <typeparam name="TResult">The scalar value type to return.</typeparam>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT COUNT(*) FROM...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <returns>A scalar value.</returns>
         TResult ExecuteScalar<TResult>(string query, CommandParameterValues args = null, TimeSpan? commandTimeout = null);
 
         /// <summary>
         /// Executes a query that returns a data reader, and allows you to manually read the fields.
         /// </summary>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT DISTINCT ProjectId FROM Release...</c></param>
         /// <param name="readerCallback">
         ///     A callback that will be invoked with the SQL data reader. The reader will be disposed
         ///     after the callback returns.
         /// </param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         void ExecuteReader(string query, Action<IDataReader> readerCallback, TimeSpan? commandTimeout = null);
 
         /// <summary>
         /// Executes a query that returns a data reader, and allows you to manually read the fields.
         /// </summary>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT DISTINCT ProjectId FROM Release...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="readerCallback">
         ///     A callback that will be invoked with the SQL data reader. The reader will be disposed
         ///     after the callback returns.
         /// </param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         void ExecuteReader(string query, CommandParameterValues args, Action<IDataReader> readerCallback, TimeSpan? commandTimeout = null);
 
         /// <summary>
         /// Executes a query that returns strongly typed documents.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT * FROM Release...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <returns>A stream of resulting documents.</returns>
         IEnumerable<TDocument> ExecuteReader<TDocument>(string query, CommandParameterValues args, TimeSpan? commandTimeout = null);
 
         /// <summary>
         /// Executes a query that returns strongly typed documents using a custom mapper function.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT * FROM Release...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="projectionMapper">The mapper function to use to convert each record into the strongly typed document.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <returns>A stream of resulting documents.</returns>
         IEnumerable<TDocument> ExecuteReaderWithProjection<TDocument>(string query, CommandParameterValues args, Func<IProjectionMapper, TDocument> projectionMapper, TimeSpan? commandTimeout = null);
 
         /// <summary>
         /// Executes a query that returns no results.
         /// </summary>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT COUNT(*) FROM...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <returns>The number of rows affected.</returns>
         int ExecuteNonQuery(string query, CommandParameterValues args = null, TimeSpan? commandTimeout = null);
 
         /// <summary>
         /// Creates a query that returns strongly typed documents.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <returns>A stream of resulting documents.</returns>
         ITableSourceQueryBuilder<TDocument> TableQuery<TDocument>() where TDocument : class, IId;
 
         /// <summary>
         /// Returns strongly typed documents from the specified raw SQL query.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT COUNT(*) FROM...</c></param>
         /// <returns>A builder to further customize the query.</returns>
         ISubquerySourceBuilder<TDocument> RawSqlQuery<TDocument>(string query) where TDocument : class, IId;
 
         /// <summary>
         /// Loads a single document given its ID. If the item is not found, returns <c>null</c>.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="id">The <c>Id</c> of the document to find.</param>
         /// <returns>The document, or <c>null</c> if the document is not found.</returns>
         TDocument Load<TDocument>(string id) where TDocument : class, IId;
 
         /// <summary>
         /// Loads a set of documents by their ID's. Documents that are not found are excluded from the result list (that is,
         /// the results may contain less items than the number of ID's queried for).
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="ids">A collection of ID's to query by.</param>
         /// <returns>The documents.</returns>
         TDocument[] Load<TDocument>(IEnumerable<string> ids) where TDocument : class, IId;
 
         /// <summary>
         /// Loads a set of documents by their ID's. Documents that are not found are excluded from the result list (that is,
         /// the results may contain less items than the number of ID's queried for).
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="ids">A collection of ID's to query by.</param>
         /// <returns>The documents as a lazy loaded stream.</returns>
         IEnumerable<TDocument> LoadStream<TDocument>(IEnumerable<string> ids) where TDocument : class, IId;
 
         /// <summary>
         /// Loads a single document given its ID. If the item is not found, throws a <see cref="ResourceNotFoundException" />.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="id">The <c>Id</c> of the document to find.</param>
         /// <returns>The document, or <c>null</c> if the document is not found.</returns>
         TDocument LoadRequired<TDocument>(string id) where TDocument : class, IId;
 
         /// <summary>
         /// Loads a set of documents by their ID's. If any of the documents are not found, a
         /// <see cref="ResourceNotFoundException" /> will be thrown.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="ids">A collection of ID's to query by.</param>
         /// <returns>The documents.</returns>
         TDocument[] LoadRequired<TDocument>(IEnumerable<string> ids) where TDocument : class, IId;
 
     }
 }
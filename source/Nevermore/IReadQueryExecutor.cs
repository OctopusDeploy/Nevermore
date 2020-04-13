using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using Nevermore.Contracts;
using Nevermore.Util;

namespace Nevermore
 {
     public interface IReadQueryExecutor
     {
         /// <summary>
         /// Loads a single document given its ID. If the item is not found, returns <c>null</c>.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="id">The <c>Id</c> of the document to find.</param>
         /// <returns>The document, or <c>null</c> if the document is not found.</returns>
         [Pure] TDocument Load<TDocument>(string id) where TDocument : class, IId;
         
         /// <summary>
         /// Loads a single document given its ID. If the item is not found, returns <c>null</c>.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="id">The <c>Id</c> of the document to find.</param>
         /// <returns>The document, or <c>null</c> if the document is not found.</returns>
         [Pure] Task<TDocument> LoadAsync<TDocument>(string id) where TDocument : class, IId;
 
         /// <summary>
         /// Loads a set of documents by their ID's. Documents that are not found are excluded from the result list (that is,
         /// the results may contain less items than the number of ID's queried for).
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="ids">A collection of ID's to query by.</param>
         /// <returns>The documents.</returns>
         [Pure] List<TDocument> Load<TDocument>(IEnumerable<string> ids) where TDocument : class, IId;
         
         /// <summary>
         /// Loads a set of documents by their ID's. Documents that are not found are excluded from the result list (that is,
         /// the results may contain less items than the number of ID's queried for).
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="ids">A collection of ID's to query by.</param>
         /// <returns>The documents.</returns>
         [Pure] Task<List<TDocument>> LoadAsync<TDocument>(IEnumerable<string> ids) where TDocument : class, IId;
 
         /// <summary>
         /// Loads a set of documents by their ID's. Documents that are not found are excluded from the result list (that is,
         /// the results may contain less items than the number of ID's queried for).
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="ids">A collection of ID's to query by.</param>
         /// <returns>The documents as a lazy loaded stream.</returns>
         [Pure] IEnumerable<TDocument> LoadStream<TDocument>(IEnumerable<string> ids) where TDocument : class, IId;
 
         /// <summary>
         /// Loads a set of documents by their ID's. Documents that are not found are excluded from the result list (that is,
         /// the results may contain less items than the number of ID's queried for).
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="ids">A collection of ID's to query by.</param>
         /// <returns>The documents as a lazy loaded stream.</returns>
         [Pure] IAsyncEnumerable<TDocument> LoadStreamAsync<TDocument>(IEnumerable<string> ids) where TDocument : class, IId;
 
         /// <summary>
         /// Loads a single document given its ID. If the item is not found, throws a <see cref="ResourceNotFoundException" />.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="id">The <c>Id</c> of the document to find.</param>
         /// <returns>The document, or <c>null</c> if the document is not found.</returns>
         [Pure] TDocument LoadRequired<TDocument>(string id) where TDocument : class, IId;
         
         /// <summary>
         /// Loads a single document given its ID. If the item is not found, throws a <see cref="ResourceNotFoundException" />.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="id">The <c>Id</c> of the document to find.</param>
         /// <returns>The document, or <c>null</c> if the document is not found.</returns>
         [Pure] Task<TDocument> LoadRequiredAsync<TDocument>(string id) where TDocument : class, IId;
 
         /// <summary>
         /// Loads a set of documents by their ID's. If any of the documents are not found, a
         /// <see cref="ResourceNotFoundException" /> will be thrown.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="ids">A collection of ID's to query by.</param>
         /// <returns>The documents.</returns>
         [Pure] List<TDocument> LoadRequired<TDocument>(IEnumerable<string> ids) where TDocument : class, IId;
 
         /// <summary>
         /// Begins building a query that returns strongly typed documents.
         /// </summary>
         /// <typeparam name="TRecord">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <returns>A stream of resulting documents.</returns>
         [Pure] ITableSourceQueryBuilder<TRecord> TableQuery<TRecord>() where TRecord : class;
 
         /// <summary>
         /// Returns strongly typed documents from the specified raw SQL query.
         /// </summary>
         /// <typeparam name="TRecord">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT COUNT(*) FROM...</c></param>
         /// <returns>A builder to further customize the query.</returns>
         [Pure] ISubquerySourceBuilder<TRecord> RawSqlQuery<TRecord>(string query) where TRecord : class;

         /// <summary>
         /// Executes a query that returns a scalar value (e.g., an INSERT or UPDATE query that returns the number of rows, or a
         /// SELECT query that returns a count).
         /// </summary>
         /// <typeparam name="TResult">The scalar value type to return.</typeparam>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT COUNT(*) FROM...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="retriableOperation">The type of operation being performed. The retry policy on the transaction will then decide whether it's safe to retry this command if it fails.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <returns>A scalar value.</returns>
         TResult ExecuteScalar<TResult>(string query, CommandParameterValues args = null, RetriableOperation retriableOperation = RetriableOperation.Select, TimeSpan? commandTimeout = null);

         /// <summary>
         /// Executes a query that returns strongly typed documents.
         /// </summary>
         /// <typeparam name="TRecord">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT * FROM Release...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <returns>A stream of resulting documents.</returns>
         [Pure] IEnumerable<TRecord> Stream<TRecord>(string query, CommandParameterValues args = null, TimeSpan? commandTimeout = null);

         /// <summary>
         /// Executes a query that returns strongly typed documents. 
         /// </summary>
         /// <param name="preparedCommand">Everything needed to run the query.</param>
         /// <typeparam name="TRecord">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <returns>A stream of resulting documents.</returns>
         [Pure] IEnumerable<TRecord> Stream<TRecord>(PreparedCommand preparedCommand);
         
         /// <summary>
         /// Executes a query that returns strongly typed documents. 
         /// </summary>
         /// <param name="preparedCommand">Everything needed to run the query.</param>
         /// <typeparam name="TRecord">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <returns>A stream of resulting documents.</returns>
         [Pure] IAsyncEnumerable<TRecord> StreamAsync<TRecord>(PreparedCommand preparedCommand);
         
         /// <summary>
         /// Executes a query that returns strongly typed documents using a custom mapper function.
         /// </summary>
         /// <typeparam name="TRecord">The type of object being returned from the query. Results from the database will be mapped to this type using the projection mapper.</typeparam>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT * FROM Release...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="projectionMapper">The mapper function to use to convert each record into the strongly typed document.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <returns>A stream of resulting documents.</returns>
         [Pure] IEnumerable<TRecord> Stream<TRecord>(string query, CommandParameterValues args, Func<IProjectionMapper, TRecord> projectionMapper, TimeSpan? commandTimeout = null);

         /// <summary>
         /// Executes a query that returns strongly typed documents using a custom mapper function.
         /// </summary>
         /// <typeparam name="TRecord">The type of object being returned from the query. Results from the database will be mapped to this type using the projection mapper.</typeparam>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT * FROM Release...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="projectionMapper">The mapper function to use to convert each record into the strongly typed document.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <returns>A stream of resulting documents.</returns>
         [Pure] IAsyncEnumerable<TRecord> StreamAsync<TRecord>(string query, CommandParameterValues args, Func<IProjectionMapper, TRecord> projectionMapper, TimeSpan? commandTimeout = null);

         /// <summary>
         /// Executes a query that returns no results.
         /// </summary>
         /// <param name="query">The SQL query to execute. Example: <c>DROP TABLE...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <returns>The number of rows affected.</returns>
         int ExecuteNonQuery(string query, CommandParameterValues args = null, TimeSpan? commandTimeout = null);

         int ExecuteNonQuery(PreparedCommand preparedCommand);
         Task<int> ExecuteNonQueryAsync(PreparedCommand preparedCommand);
         
         T ExecuteScalar<T>(PreparedCommand preparedCommand);
         Task<T> ExecuteScalarAsync<T>(PreparedCommand preparedCommand);
         
         [Pure] DbDataReader ExecuteReader(PreparedCommand preparedCommand);
         [Pure] Task<DbDataReader> ExecuteReaderAsync(PreparedCommand preparedCommand);
     }
 }
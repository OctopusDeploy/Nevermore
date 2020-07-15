using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Advanced;

namespace Nevermore
 {
     /// <summary>
     /// A transaction that provides the ability to read data from the database.
     /// </summary>
     public interface IReadQueryExecutor
     {
         /// <summary>
         /// Loads a single document given its ID. If the item is not found, returns <c>null</c>.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="id">The <c>Id</c> of the document to find.</param>
         /// <returns>The document, or <c>null</c> if the document is not found.</returns>
         [Pure] TDocument Load<TDocument>(object id) where TDocument : class;

         /// <summary>
         /// Loads a single document given its ID. If the item is not found, returns <c>null</c>.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="id">The <c>Id</c> of the document to find.</param>
         /// <param name="cancellationToken">Token to use to cancel the command.</param>
         /// <returns>The document, or <c>null</c> if the document is not found.</returns>
         [Pure] Task<TDocument> LoadAsync<TDocument>(object id, CancellationToken cancellationToken = default) where TDocument : class;
 
         /// <summary>
         /// Loads a set of documents by their ID's. Documents that are not found are excluded from the result list (that is,
         /// the results may contain less items than the number of ID's queried for).
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="ids">A collection of ID's to query by.</param>
         /// <returns>The documents.</returns>
         [Pure] List<TDocument> LoadMany<TDocument>(params object[] ids) where TDocument : class;

         /// <summary>
         /// Loads a set of documents by their ID's. Documents that are not found are excluded from the result list (that is,
         /// the results may contain less items than the number of ID's queried for).
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="ids">A collection of ID's to query by.</param>
         /// <param name="cancellationToken">Token to use to cancel the command.</param>
         /// <returns>The documents.</returns>
         [Pure] Task<List<TDocument>> LoadManyAsync<TDocument>(object[] ids, CancellationToken cancellationToken = default) where TDocument : class;
 
         /// <summary>
         /// Loads a set of documents by their ID's. Documents that are not found are excluded from the result list (that is,
         /// the results may contain less items than the number of ID's queried for).
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="ids">A collection of ID's to query by.</param>
         /// <returns>The documents as a lazy loaded stream.</returns>
         [Pure] IEnumerable<TDocument> LoadStream<TDocument>(params object[] ids) where TDocument : class;

         /// <summary>
         /// Loads a set of documents by their ID's. Documents that are not found are excluded from the result list (that is,
         /// the results may contain less items than the number of ID's queried for).
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="ids">A collection of ID's to query by.</param>
         /// <param name="cancellationToken">Token to use to cancel the command.</param>
         /// <returns>The documents as a lazy loaded stream.</returns>
         [Pure] IAsyncEnumerable<TDocument> LoadStreamAsync<TDocument>(object[] ids, CancellationToken cancellationToken = default) where TDocument : class;
 
         /// <summary>
         /// Loads a single document given its ID. If the item is not found, throws a <see cref="ResourceNotFoundException" />.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="id">The <c>Id</c> of the document to find.</param>
         /// <returns>The document, or <c>null</c> if the document is not found.</returns>
         [Pure] TDocument LoadRequired<TDocument>(object id) where TDocument : class;

         /// <summary>
         /// Loads a single document given its ID. If the item is not found, throws a <see cref="ResourceNotFoundException" />.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="id">The <c>Id</c> of the document to find.</param>
         /// <param name="cancellationToken">Token to use to cancel the command.</param>
         /// <returns>The document, or <c>null</c> if the document is not found.</returns>
         [Pure] Task<TDocument> LoadRequiredAsync<TDocument>(object id, CancellationToken cancellationToken = default) where TDocument : class;
 
         /// <summary>
         /// Loads a set of documents by their ID's. If any of the documents are not found, a
         /// <see cref="ResourceNotFoundException" /> will be thrown.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="ids">A collection of ID's to query by.</param>
         /// <returns>The documents.</returns>
         [Pure] List<TDocument> LoadManyRequired<TDocument>(params object[] ids) where TDocument : class;

         /// <summary>
         /// Loads a set of documents by their ID's. If any of the documents are not found, a
         /// <see cref="ResourceNotFoundException" /> will be thrown.
         /// </summary>
         /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="ids">A collection of ID's to query by.</param>
         /// <param name="cancellationToken">Token to use to cancel the command.</param>
         /// <returns>The documents.</returns>
         [Pure] Task<List<TDocument>> LoadManyRequiredAsync<TDocument>(object[] ids, CancellationToken cancellationToken = default) where TDocument : class;
 
         /// <summary>
         /// Begins building a query that returns strongly typed documents.
         /// </summary>
         /// <typeparam name="TRecord">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <returns>A stream of resulting documents.</returns>
         [Pure] ITableSourceQueryBuilder<TRecord> Query<TRecord>() where TRecord : class;
 
         /// <summary>
         /// Returns strongly typed documents from the specified raw SQL query.
         /// </summary>
         /// <typeparam name="TRecord">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT COUNT(*) FROM...</c></param>
         /// <returns>A builder to further customize the query.</returns>
         [Obsolete("We are thinking of removing this method. Do you use it? What do you use it for? Let us know.")]
         [Pure] ISubquerySourceBuilder<TRecord> RawSqlQuery<TRecord>(string query) where TRecord : class;

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
         /// <typeparam name="TRecord">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT * FROM Release...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <param name="cancellationToken">Token to use to cancel the command.</param>
         /// <returns>A stream of resulting documents.</returns>
         [Pure] IAsyncEnumerable<TRecord> StreamAsync<TRecord>(string query, CommandParameterValues args = null, TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default);

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
         /// <param name="cancellationToken">Token to use to cancel the command.</param>
         /// <typeparam name="TRecord">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
         /// <returns>A stream of resulting documents.</returns>
         [Pure] IAsyncEnumerable<TRecord> StreamAsync<TRecord>(PreparedCommand preparedCommand, CancellationToken cancellationToken = default);
         
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
         /// <param name="cancellationToken">Token to use to cancel the command.</param>
         /// <returns>A stream of resulting documents.</returns>
         [Pure] IAsyncEnumerable<TRecord> StreamAsync<TRecord>(string query, CommandParameterValues args, Func<IProjectionMapper, TRecord> projectionMapper, TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default);

         /// <summary>
         /// Executes a query that returns no results.
         /// </summary>
         /// <param name="query">The SQL query to execute. Example: <c>DROP TABLE...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <returns>The number of rows affected.</returns>
         int ExecuteNonQuery(string query, CommandParameterValues args = null, TimeSpan? commandTimeout = null);

         /// <summary>
         /// Executes a query that returns no results, typically one that will write to the database. It can also be
         /// used when reading data though, so it's included on <see cref="IReadQueryExecutor"/>.
         /// </summary>
         /// <param name="query">The SQL query to execute. Example: <c>DROP TABLE...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <param name="cancellationToken">Token to use to cancel the command.</param>
         /// <returns>Depends on the query, but typically the number of rows affected.</returns>
         Task<int> ExecuteNonQueryAsync(string query, CommandParameterValues args = null, TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default);

         /// <summary>
         /// Executes a query that returns no results, typically one that will write to the database. It can also be
         /// used when reading data though, so it's included on <see cref="IReadQueryExecutor"/>.
         /// </summary>
         /// <param name="preparedCommand">The command to execute.</param>
         /// <returns>Depends on the query, but typically the number of rows affected.</returns>
         int ExecuteNonQuery(PreparedCommand preparedCommand);

         /// <summary>
         /// Executes a query that returns no results, typically one that will write to the database. It can also be
         /// used when reading data though, so it's included on <see cref="IReadQueryExecutor"/>.
         /// </summary>
         /// <param name="preparedCommand">The command to execute.</param>
         /// <param name="cancellationToken">Token to use to cancel the command.</param>
         /// <returns>Depends on the query, but typically the number of rows affected.</returns>
         Task<int> ExecuteNonQueryAsync(PreparedCommand preparedCommand, CancellationToken cancellationToken = default);
 
         /// <summary>
         /// Executes a query that returns a scalar value (e.g., SELECT query that returns a count).
         /// </summary>
         /// <typeparam name="TResult">The scalar value type to return.</typeparam>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT COUNT(*) FROM...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="retriableOperation">The type of operation being performed. The retry policy on the transaction will then decide whether it's safe to retry this command if it fails.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <returns>A scalar value.</returns>
         TResult ExecuteScalar<TResult>(string query, CommandParameterValues args = null, RetriableOperation retriableOperation = RetriableOperation.Select, TimeSpan? commandTimeout = null);

         /// <summary>
         /// Executes a query that returns a scalar value (e.g., SELECT query that returns a count).
         /// </summary>
         /// <typeparam name="TResult">The scalar value type to return.</typeparam>
         /// <param name="query">The SQL query to execute. Example: <c>SELECT COUNT(*) FROM...</c></param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="retriableOperation">The type of operation being performed. The retry policy on the transaction will then decide whether it's safe to retry this command if it fails.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <param name="cancellationToken">Token to use to cancel the command.</param>
         /// <returns>A scalar value.</returns>
         Task<TResult> ExecuteScalarAsync<TResult>(string query, CommandParameterValues args = null, RetriableOperation retriableOperation = RetriableOperation.Select, TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default);

         /// <summary>
         /// Executes a query that returns a scalar value (e.g., SELECT query that returns a count).
         /// </summary>
         /// <typeparam name="TResult">The scalar value type to return. The DB result will be cast to this type. If the result is null, it will return the default value for the type..</typeparam>
         /// <param name="preparedCommand">The command to execute.</param>
         /// <returns>A scalar value.</returns>
         TResult ExecuteScalar<TResult>(PreparedCommand preparedCommand);

         /// <summary>
         /// Executes a query that returns a scalar value (e.g., SELECT query that returns a count).
         /// </summary>
         /// <typeparam name="TResult">The scalar value type to return.</typeparam>
         /// <param name="preparedCommand">The command to execute.</param>
         /// <param name="cancellationToken">Token to use to cancel the command.</param>
         /// <returns>A scalar value.</returns>
         Task<TResult> ExecuteScalarAsync<TResult>(PreparedCommand preparedCommand, CancellationToken cancellationToken = default);

         /// <summary>
         /// Executes a query that returns a data reader that you can process manually.
         /// </summary>
         /// <param name="query">The query to execute.</param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <returns>A data reader.</returns>
         [Pure] DbDataReader ExecuteReader(string query, CommandParameterValues args = null, TimeSpan? commandTimeout = null);

         /// <summary>
         /// Executes a query that returns a data reader that you can process manually.
         /// </summary>
         /// <param name="query">The query to execute.</param>
         /// <param name="args">Any arguments to pass to the query as command parameters.</param>
         /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
         /// <param name="cancellationToken">Token to use to cancel the command.</param>
         /// <returns>A data reader.</returns>
         [Pure] Task<DbDataReader> ExecuteReaderAsync(string query, CommandParameterValues args = null, TimeSpan? commandTimeout = null, CancellationToken cancellationToken = default);

         /// <summary>
         /// Executes a query that returns a data reader that you can process manually.
         /// </summary>
         /// <param name="preparedCommand">The command to execute.</param>
         /// <returns>A data reader.</returns>
         [Pure] DbDataReader ExecuteReader(PreparedCommand preparedCommand);
         
         /// <summary>
         /// Executes a query that returns a data reader that you can process manually.
         /// </summary>
         /// <param name="preparedCommand">The command to execute.</param>
         /// <param name="cancellationToken">Token to use to cancel the command.</param>
         /// <returns>A data reader.</returns>
         [Pure] Task<DbDataReader> ExecuteReaderAsync(PreparedCommand preparedCommand, CancellationToken cancellationToken = default);
     }
 }
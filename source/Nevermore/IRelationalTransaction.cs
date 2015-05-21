using System;
using System.Collections.Generic;
using System.Data;

namespace Nevermore
{
    public interface IRelationalTransaction : IDisposable
    {
        /// <summary>
        /// Executes a query that returns a scalar value (e.g., an INSERT or UPDATE query that returns the number of rows, or a SELECT query that returns a count).
        /// </summary>
        /// <typeparam name="TResult">The scalar value type to return.</typeparam>
        /// <param name="query">The SQL query to execute. Example: <c>SELECT COUNT(*) FROM...</c></param>
        /// <param name="args">Any arguments to pass to the query as command parameters.</param>
        /// <returns>A scalar value.</returns>
        TResult ExecuteScalar<TResult>(string query, CommandParameters args = null);

        /// <summary>
        /// Executes a query that returns a data reader, and allows you to manually read the fields.
        /// </summary>
        /// <param name="query">The SQL query to execute. Example: <c>SELECT DISTINCT ProjectId FROM Release...</c></param>
        /// <param name="readerCallback">A callback that will be invoked with the SQL data reader. The reader will be disposed after the callback returns.</param>
        void ExecuteReader(string query, Action<IDataReader> readerCallback);

        /// <summary>
        /// Executes a query that returns a data reader, and allows you to manually read the fields.
        /// </summary>
        /// <param name="query">The SQL query to execute. Example: <c>SELECT DISTINCT ProjectId FROM Release...</c></param>
        /// <param name="args">Any arguments to pass to the query as command parameters.</param>
        /// <param name="readerCallback">A callback that will be invoked with the SQL data reader. The reader will be disposed after the callback returns.</param>
        void ExecuteReader(string query, CommandParameters args, Action<IDataReader> readerCallback);

        /// <summary>
        /// Executes a query that returns strongly typed documents.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <param name="query">The SQL query to execute. Example: <c>SELECT * FROM Release...</c></param>
        /// <param name="args">Any arguments to pass to the query as command parameters.</param>
        /// <returns>A stream of resulting documents.</returns>
        IEnumerable<TDocument> ExecuteReader<TDocument>(string query, CommandParameters args);

        IEnumerable<TDocument> ExecuteReaderWithProjection<TDocument>(string query, CommandParameters args, Func<IProjectionMapper, TDocument> projectionMapper);

        /// <summary>
        /// Creates a query that returns strongly typed documents.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <returns>A stream of resulting documents.</returns>
        IQueryBuilder<TDocument> Query<TDocument>() where TDocument : class;

        /// <summary>
        /// Loads a single document given its ID. If the item is not found, returns <c>null</c>.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <param name="id">The <c>Id</c> of the document to find.</param>
        /// <returns>The document, or <c>null</c> if the document is not found.</returns>
        TDocument Load<TDocument>(string id) where TDocument : class;

        /// <summary>
        /// Loads a set of documents by their ID's. Documents that are not found are excluded from the result list (that is, the results may contain less items than the number of ID's queried for).
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <param name="ids">A collection of ID's to query by.</param>
        /// <returns>The documents.</returns>
        TDocument[] Load<TDocument>(IEnumerable<string> ids) where TDocument : class;

        /// <summary>
        /// Loads a single document given its ID. If the item is not found, throws a <see cref="ResourceNotFoundException"/>.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <param name="id">The <c>Id</c> of the document to find.</param>
        /// <returns>The document, or <c>null</c> if the document is not found.</returns>
        TDocument LoadRequired<TDocument>(string id) where TDocument : class;

        /// <summary>
        /// Loads a set of documents by their ID's. If any of the documents are not found, a <see cref="ResourceNotFoundException"/> will be thrown.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <param name="ids">A collection of ID's to query by.</param>
        /// <returns>The documents.</returns>
        TDocument[] LoadRequired<TDocument>(IEnumerable<string> ids) where TDocument : class;

        /// <summary>
        /// Immediately inserts a new item into the default table for the document type. The item will have an automatically assigned ID, and that ID value will be visible in the <code>Id</code> property of the object as soon as <see cref="M:Insert"/> returns.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="instance">The document instance to insert.</param>
        void Insert<TDocument>(TDocument instance) where TDocument : class;

        /// <summary>
        /// Immediately inserts a new item into a specific table. The item will have an automatically assigned ID, and that ID value will be visible in the <code>Id</code> property of the object as soon as <see cref="M:Insert"/> returns.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="tableName">The name of the table to insert the document into.</param>
        /// <param name="instance">The document instance to insert.</param>
        void Insert<TDocument>(string tableName, TDocument instance) where TDocument : class;

        /// <summary>
        /// Immediately inserts a new item into the default table for the document type. Uses a specific ID rather than automatically generating one.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="instance">The document instance to insert.</param>
        /// <param name="customAssignedId">The ID to assign to the document.</param>
        void Insert<TDocument>(TDocument instance, string customAssignedId) where TDocument : class;

        /// <summary>
        /// Immediately inserts a new item into a specific table. Uses a specific ID rather than automatically generating one.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="tableName">The name of the table to insert the document into.</param>
        /// <param name="instance">The document instance to insert.</param>
        /// <param name="customAssignedId">The ID to assign to the document.</param>
        void Insert<TDocument>(string tableName, TDocument instance, string customAssignedId) where TDocument : class;

        /// <summary>
        /// Updates an existing document in the database.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being updated.</typeparam>
        /// <param name="instance">The document to update.</param>
        void Update<TDocument>(TDocument instance) where TDocument : class;

        /// <summary>
        /// Deletes an existing document from the database.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being deleted.</typeparam>
        /// <param name="instance">The document to delete.</param>
        void Delete<TDocument>(TDocument instance) where TDocument : class;

        /// <summary>
        /// Commits the current pending transaction.
        /// </summary>
        void Commit();
    }
}
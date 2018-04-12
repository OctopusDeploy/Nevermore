using System;
using System.Collections.Generic;
using System.Data;
using Nevermore.Contracts;

namespace Nevermore
{
    public interface IRelationalTransaction : IDisposable
    {
        /// <summary>
        /// Executes a query that returns a scalar value (e.g., an INSERT or UPDATE query that returns the number of rows, or a
        /// SELECT query that returns a count).
        /// </summary>
        /// <typeparam name="TResult">The scalar value type to return.</typeparam>
        /// <param name="query">The SQL query to execute. Example: <c>SELECT COUNT(*) FROM...</c></param>
        /// <param name="args">Any arguments to pass to the query as command parameters.</param>
        /// <param name="commandTimeoutSeconds">A custom timeout in seconds to use for the command instead of the default.</param>
        /// <returns>A scalar value.</returns>
        TResult ExecuteScalar<TResult>(string query, CommandParameterValues args = null, int? commandTimeoutSeconds = null);

        /// <summary>
        /// Executes a query that returns a data reader, and allows you to manually read the fields.
        /// </summary>
        /// <param name="query">The SQL query to execute. Example: <c>SELECT DISTINCT ProjectId FROM Release...</c></param>
        /// <param name="readerCallback">
        /// A callback that will be invoked with the SQL data reader. The reader will be disposed
        /// after the callback returns.
        /// </param>
        void ExecuteReader(string query, Action<IDataReader> readerCallback);

        /// <summary>
        /// Executes a query that returns a data reader, and allows you to manually read the fields.
        /// </summary>
        /// <param name="query">The SQL query to execute. Example: <c>SELECT DISTINCT ProjectId FROM Release...</c></param>
        /// <param name="args">Any arguments to pass to the query as command parameters.</param>
        /// <param name="readerCallback">
        /// A callback that will be invoked with the SQL data reader. The reader will be disposed
        /// after the callback returns.
        /// </param>
        void ExecuteReader(string query, CommandParameterValues args, Action<IDataReader> readerCallback);

        /// <summary>
        /// Executes a query that returns strongly typed documents.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <param name="query">The SQL query to execute. Example: <c>SELECT * FROM Release...</c></param>
        /// <param name="args">Any arguments to pass to the query as command parameters.</param>
        /// <param name="commandTimeoutSeconds">A custom timeout in seconds to use for the command instead of the default.</param>
        /// <returns>A stream of resulting documents.</returns>
        IEnumerable<TDocument> ExecuteReader<TDocument>(string query, CommandParameterValues args, int? commandTimeoutSeconds = null);

        /// <summary>
        /// Executes a query that returns strongly typed documents using a custom mapper function.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <param name="query">The SQL query to execute. Example: <c>SELECT * FROM Release...</c></param>
        /// <param name="args">Any arguments to pass to the query as command parameters.</param>
        /// <param name="projectionMapper">The mapper function to use to convert each record into the strongly typed document.</param>
        /// <param name="commandTimeoutSeconds">A custom timeout in seconds to use for the command instead of the default.</param>
        /// <returns>A stream of resulting documents.</returns>
        IEnumerable<TDocument> ExecuteReaderWithProjection<TDocument>(string query, CommandParameterValues args, Func<IProjectionMapper, TDocument> projectionMapper, int? commandTimeoutSeconds = null);

        /// <summary>
        /// Executes a delete query (bypasses the usual OctopusModelDeletionRules checks). Only use this if you are 100% certain you can 
        /// delete from the given table without worrying about deletion rules.
        /// </summary>
        /// <param name="query">The SQL query to execute. Example: <c>DELETE FROM [Event]...</c></param>
        /// <param name="args">Any arguments to pass to the query as command parameters.</param>
        /// <param name="commandTimeoutSeconds">A custom timeout in seconds to use for the command instead of the default.</param>
        void ExecuteRawDeleteQuery(string query, CommandParameterValues args, int? commandTimeoutSeconds = null);

        /// <summary>
        /// Executes a query that returns no results.
        /// </summary>
        /// <param name="query">The SQL query to execute. Example: <c>SELECT COUNT(*) FROM...</c></param>
        /// <param name="args">Any arguments to pass to the query as command parameters.</param>
        /// <param name="commandTimeoutSeconds">A custom timeout in seconds to use for the command instead of the default.</param>
        /// <returns>The number of rows affected.</returns>
        int ExecuteNonQuery(string query, CommandParameterValues args = null, int? commandTimeoutSeconds = null);

        /// <summary>
        /// Creates a query that returns strongly typed documents.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Results from the database will be mapped to this type.</typeparam>
        /// <returns>A stream of resulting documents.</returns>
        ITableSourceQueryBuilder<TDocument> TableQuery<TDocument>() where TDocument : class, IId;

        /// <summary>
        /// Creates a deletion query for a strongly typed document.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Used to find the table against which the query will be executed.</typeparam>
        /// <returns>A builder to further customize the query</returns>
        IDeleteQueryBuilder<TDocument> DeleteQuery<TDocument>() where TDocument : class, IId;

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

        /// <summary>
        /// Immediately inserts a new item into the default table for the document type. The item will have an automatically
        /// assigned ID, and that ID value will be visible in the <code>Id</code> property of the object as soon as
        /// <see cref="M:Insert" /> returns.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="instance">The document instance to insert.</param>
        void Insert<TDocument>(TDocument instance) where TDocument : class, IId;

        /// <summary>
        /// Immediately inserts a new item into a specific table. The item will have an automatically assigned ID, and that ID
        /// value will be visible in the <code>Id</code> property of the object as soon as <see cref="M:Insert" /> returns.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="tableName">The name of the table to insert the document into.</param>
        /// <param name="instance">The document instance to insert.</param>
        void Insert<TDocument>(string tableName, TDocument instance) where TDocument : class, IId;

        /// <summary>
        /// Immediately inserts a new item into the default table for the document type. Uses a specific ID rather than
        /// automatically generating one.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="instance">The document instance to insert.</param>
        /// <param name="customAssignedId">The ID to assign to the document.</param>
        void Insert<TDocument>(TDocument instance, string customAssignedId) where TDocument : class, IId;

        /// <summary>
        /// Immediately inserts a new item into a specific table. Uses a specific ID rather than automatically generating one.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="tableName">The name of the table to insert the document into.</param>
        /// <param name="instance">The document instance to insert.</param>
        /// <param name="customAssignedId">The ID to assign to the document.</param>
        /// <param name="tableHint">The table hint to use for the insert (useful when we need a table lock on insert).</param>
        /// <param name="commandTimeoutSeconds">A custom timeout in seconds to use for the command instead of the default.</param>
        void Insert<TDocument>(string tableName, TDocument instance, string customAssignedId, string tableHint = null, int? commandTimeoutSeconds = null) where TDocument : class, IId;

        /// <summary>
        /// Immediately inserts multiple items into a specific table.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="tableName">The name of the table to insert the document into.</param>
        /// <param name="instances">The document instances to insert (will be formed into a multiple VALUES for a single SQL INSERT.</param>
        /// <param name="includeDefaultModelColumns">Whether to include the Id and Json columns in the mapping (can disable for certain tables that do not use Json - like the EventRelatedDocument table etc).</param>
        /// <param name="tableHint">The table hint to use for the insert (useful when we need a table lock on insert).</param>
        void InsertMany<TDocument>(string tableName, IReadOnlyCollection<TDocument> instances, bool includeDefaultModelColumns, string tableHint = null) where TDocument : class, IId;

        /// <summary>
        /// Updates an existing document in the database.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being updated.</typeparam>
        /// <param name="instance">The document to update.</param>
        /// <param name="tableHint">The table hint to use for the insert (useful when we need a table lock on insert).</param>
        /// <param name="commandTimeoutSeconds">A custom timeout in seconds to use for the command instead of the default.</param>
        void Update<TDocument>(TDocument instance, string tableHint = null, int? commandTimeoutSeconds = null) where TDocument : class, IId;

        /// <summary>
        /// Deletes an existing document from the database.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being deleted.</typeparam>
        /// <param name="instance">The document to delete.</param>
        /// <param name="commandTimeoutSeconds">A custom timeout in seconds to use for the command instead of the default.</param>
        void Delete<TDocument>(TDocument instance, int? commandTimeoutSeconds = null) where TDocument : class, IId;

        /// <summary>
        /// Deletes an existing document from the database by it's ID.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being deleted.</typeparam>
        /// <param name="id">The id of the document to delete.</param>
        /// <param name="commandTimeoutSeconds">A custom timeout in seconds to use for the command instead of the default.</param>
        void DeleteById<TDocument>(string id, int? commandTimeoutSeconds = null) where TDocument : class, IId;


        /// <summary>
        /// Commits the current pending transaction.
        /// </summary>
        void Commit();

        /// <summary>
        /// Allocate an ID for the specified type. The type must be mapped.
        /// If the mapping specifies a SingletonId, that is returned
        /// </summary>
        /// <param name="documentType"></param>
        /// <returns></returns>
        string AllocateId(Type documentType);

        /// <summary>
        /// Allocates an ID using the specified table name. Any mapping for that table is not used.
        /// </summary>
        /// <param name="tableName"></param>
        /// <param name="idPrefix"></param>
        /// <returns></returns>
        string AllocateId(string tableName, string idPrefix);
    }
}
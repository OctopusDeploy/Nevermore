using System;
using System.Collections.Generic;
using Nevermore.Contracts;

namespace Nevermore
{
    public interface IWriteQueryExecutor : IReadQueryExecutor
    {
        /// <summary>
        /// Immediately inserts a new item into the default table for the document type. The item will have an automatically
        /// assigned ID, and that ID value will be visible in the <code>Id</code> property of the object as soon as
        /// <see cref="M:Insert" /> returns.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="instance">The document instance to insert.</param>
        /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
        void Insert<TDocument>(TDocument instance, TimeSpan? commandTimeout = null) where TDocument : class, IId;

        /// <summary>
        /// Immediately inserts a new item into a specific table. The item will have an automatically assigned ID, and that ID
        /// value will be visible in the <code>Id</code> property of the object as soon as <see cref="M:Insert" /> returns.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="tableName">The name of the table to insert the document into.</param>
        /// <param name="instance">The document instance to insert.</param>
        /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
        void Insert<TDocument>(string tableName, TDocument instance, TimeSpan? commandTimeout = null) where TDocument : class, IId;

        /// <summary>
        /// Immediately inserts a new item into the default table for the document type. Uses a specific ID rather than
        /// automatically generating one.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="instance">The document instance to insert.</param>
        /// <param name="customAssignedId">The ID to assign to the document.</param>
        /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
        void Insert<TDocument>(TDocument instance, string customAssignedId, TimeSpan? commandTimeout = null) where TDocument : class, IId;

        /// <summary>
        /// Immediately inserts a new item into a specific table. Uses a specific ID rather than automatically generating one.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="tableName">The name of the table to insert the document into.</param>
        /// <param name="instance">The document instance to insert.</param>
        /// <param name="customAssignedId">The ID to assign to the document.</param>
        /// <param name="tableHint">The table hint to use for the insert (useful when we need a table lock on insert).</param>
        /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
        void Insert<TDocument>(string tableName, TDocument instance, string customAssignedId, string tableHint = null, TimeSpan? commandTimeout = null) where TDocument : class, IId;

        /// <summary>
        /// Immediately inserts multiple items into a specific table.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="tableName">The name of the table to insert the document into.</param>
        /// <param name="instances">The document instances to insert (will be formed into a multiple VALUES for a single SQL INSERT.</param>
        /// <param name="includeDefaultModelColumns">Whether to include the Id and Json columns in the mapping (can disable for certain tables that do not use Json - like the EventRelatedDocument table etc).</param>
        /// <param name="tableHint">The table hint to use for the insert (useful when we need a table lock on insert).</param>
        /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
        void InsertMany<TDocument>(string tableName, IReadOnlyCollection<TDocument> instances, bool includeDefaultModelColumns, string tableHint = null, TimeSpan? commandTimeout = null) where TDocument : class, IId;

        /// <summary>
        /// Updates an existing document in the database.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being updated.</typeparam>
        /// <param name="instance">The document to update.</param>
        /// <param name="tableHint">The table hint to use for the insert (useful when we need a table lock on insert).</param>
        /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
        void Update<TDocument>(TDocument instance, string tableHint = null, TimeSpan? commandTimeout = null) where TDocument : class, IId;

        /// <summary>
        /// Deletes an existing document from the database.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being deleted.</typeparam>
        /// <param name="instance">The document to delete.</param>
        /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
        void Delete<TDocument>(TDocument instance, TimeSpan? commandTimeout = null) where TDocument : class, IId;

        /// <summary>
        /// Deletes an existing document from the database by its ID.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being deleted.</typeparam>
        /// <param name="id">The id of the document to delete.</param>
        /// <param name="commandTimeout">A custom timeout to use for the command instead of the default.</param>
        void DeleteById<TDocument>(string id, TimeSpan? commandTimeout = null) where TDocument : class, IId;
        
        /// <summary>
        /// Creates a deletion query for a strongly typed document.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being queried. Used to find the table against which the query will be executed.</typeparam>
        /// <returns>A builder to further customize the query</returns>
        IDeleteQueryBuilder<TDocument> DeleteQuery<TDocument>() where TDocument : class, IId;

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
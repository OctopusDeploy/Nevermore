using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Nevermore.Contracts;
using Nevermore.Querying;

namespace Nevermore
{
    public interface IWriteQueryExecutor : IReadQueryExecutor
    {
        /// <summary>
        /// Immediately inserts a new item into the default table for the document type. The item will have an automatically
        /// assigned ID, and that ID value will be visible in the <code>Id</code> property of the object as soon as
        /// <see cref="M:Insert" /> returns. To assign your own ID, use the <see cref="options"/> parameter.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="instance">The document instance to insert.</param>
        /// <param name="options">Advanced options for the insert operation.</param>
        void Insert<TDocument>(TDocument instance, InsertOptions options = null) where TDocument : class, IId;

        /// <summary>
        /// Immediately inserts a new item into the default table for the document type. The item will have an automatically
        /// assigned ID, and that ID value will be visible in the <code>Id</code> property of the object as soon as
        /// <see cref="M:Insert" /> returns. To assign your own ID, use the <see cref="options"/> parameter.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="instance">The document instance to insert.</param>
        /// <param name="options">Advanced options for the insert operation.</param>
        Task InsertAsync<TDocument>(TDocument instance, InsertOptions options = null) where TDocument : class, IId;

        /// <summary>
        /// Immediately inserts multiple items into a specific table. Useful for up to a few hundred items, but not more
        /// (depends on the number of properties in each item). 
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="documents">The document instances to insert (will be formed into a multiple VALUES for a single SQL INSERT.</param>
        /// <param name="options">Advanced options for the insert operation.</param>
        void InsertMany<TDocument>(IReadOnlyCollection<TDocument> documents, InsertOptions options = null) where TDocument : class, IId;

        /// <summary>
        /// Immediately inserts multiple items into a specific table. Useful for up to a few hundred items, but not more
        /// (depends on the number of properties in each item).
        /// </summary>
        /// <typeparam name="TDocument">The type of document being inserted.</typeparam>
        /// <param name="documents">The document instances to insert (will be formed into a multiple VALUES for a single SQL INSERT.</param>
        /// <param name="options">Advanced options for the insert operation.</param>
        Task InsertManyAsync<TDocument>(IReadOnlyCollection<TDocument> documents, InsertOptions options = null) where TDocument : class, IId;

        /// <summary>
        /// Updates an existing document in the database. 
        /// </summary>
        /// <typeparam name="TDocument">The type of document being updated.</typeparam>
        /// <param name="document">The document to update.</param>
        /// <param name="options">Advanced options for the update operation.</param>
        void Update<TDocument>(TDocument document, UpdateOptions options = null) where TDocument : class, IId;
        
        /// <summary>
        /// Updates an existing document in the database. 
        /// </summary>
        /// <typeparam name="TDocument">The type of document being updated.</typeparam>
        /// <param name="document">The document to update.</param>
        /// <param name="options">Advanced options for the update operation.</param>
        Task UpdateAsync<TDocument>(TDocument document, UpdateOptions options = null) where TDocument : class, IId;

        /// <summary>
        /// Deletes an existing document from the database.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being deleted.</typeparam>
        /// <param name="document">The document to delete.</param>
        /// <param name="options">Advanced options for the delete operation.</param>
        void Delete<TDocument>(TDocument document, DeleteOptions options = null) where TDocument : class, IId;

        /// <summary>
        /// Deletes an existing document from the database.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being deleted.</typeparam>
        /// <param name="document">The document to delete.</param>
        /// <param name="options">Advanced options for the delete operation.</param>
        Task DeleteAsync<TDocument>(TDocument document, DeleteOptions options = null) where TDocument : class, IId;

        /// <summary>
        /// Deletes an existing document from the database by its ID.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being deleted.</typeparam>
        /// <param name="id">The id of the document to delete.</param>
        /// <param name="options">Advanced options for the delete operation.</param>
        void Delete<TDocument>(string id, DeleteOptions options = null) where TDocument : class, IId;

        /// <summary>
        /// Deletes an existing document from the database by its ID.
        /// </summary>
        /// <typeparam name="TDocument">The type of document being deleted.</typeparam>
        /// <param name="id">The id of the document to delete.</param>
        /// <param name="options">Advanced options for the delete operation.</param>
        Task DeleteAsync<TDocument>(string id, DeleteOptions options = null) where TDocument : class, IId;
        
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
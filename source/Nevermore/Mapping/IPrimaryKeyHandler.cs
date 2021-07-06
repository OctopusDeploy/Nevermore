#nullable enable
using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Data.SqlClient.Server;

namespace Nevermore.Mapping
{
    public interface IPrimaryKeyHandler
    {
        /// <summary>
        /// The property type this handler is responsible for.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets the SqlMetaData to use for keys of this type. This is used for TableValueParameters.
        /// </summary>
        /// <param name="name">The name of the column.</param>
        SqlMetaData GetSqlMetaData(string name);

        /// <summary>
        /// Convert the given id value to the underlying primitive type required for Sql command parameters.
        /// </summary>
        /// <param name="id">The id to convert.</param>
        /// <returns>The converted id.</returns>
        [return: NotNullIfNotNull("id")]
        object? ConvertToPrimitiveValue(object? id);

        /// <summary>
        /// Get the next key for the given table.
        /// </summary>
        /// <remarks>The keyAllocator has to be passed here as it is tied to the RelationalStore instance, and thus can't be specified at configuration time in the constructor.</remarks>
        /// <param name="keyAllocator">The key allocator to use getting the next id from.</param>
        /// <param name="tableName">The table name the key is required for.</param>
        /// <returns>The next key, as the type that matches the model object's Id property type. ConvertToPrimitiveValue should be called with this value if it is to be used as a Sql parameter.</returns>
        object GetNextKey(IKeyAllocator keyAllocator, string tableName);
    }
}
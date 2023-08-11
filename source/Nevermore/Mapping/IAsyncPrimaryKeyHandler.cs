#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Mapping
{
    public interface IAsyncPrimaryKeyHandler : IPrimaryKeyHandler
    {
        /// <summary>
        /// Get the next key for the given table.
        /// </summary>
        /// <remarks>The keyAllocator has to be passed here as it is tied to the RelationalStore instance, and thus can't be specified at configuration time in the constructor.</remarks>
        /// <param name="keyAllocator">The key allocator to use getting the next id from.</param>
        /// <param name="tableName">The table name the key is required for.</param>
        /// <param name="cancellationToken">Token to use to cancel the command.</param>
        /// <returns>The next key, as the type that matches the model object's Id property type. ConvertToPrimitiveValue should be called with this value if it is to be used as a Sql parameter.</returns>
        Task<object> GetNextKeyAsync(IKeyAllocator keyAllocator, string tableName, CancellationToken cancellationToken);
    }
}
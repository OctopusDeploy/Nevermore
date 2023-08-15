#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Mapping
{
    public abstract class AsyncPrimaryKeyHandler<T> : PrimaryKeyHandler<T>, IAsyncPrimaryKeyHandler
    {
        public abstract ValueTask<object> GetNextKeyAsync(IKeyAllocator keyAllocator, string tableName, CancellationToken cancellationToken);
    }
}
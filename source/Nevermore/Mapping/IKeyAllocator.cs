using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Mapping
{
    public interface IKeyAllocator
    {
        void Reset();
        long NextId(string tableName);
        ValueTask<long> NextIdAsync(string tableName, CancellationToken cancellationToken);
    }
}
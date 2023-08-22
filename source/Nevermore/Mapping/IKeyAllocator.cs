using System.Threading;
using System.Threading.Tasks;

namespace Nevermore.Mapping
{
    public interface IKeyAllocator
    {
        void Reset();
        int NextId(string tableName);
        ValueTask<int> NextIdAsync(string tableName, CancellationToken cancellationToken);
    }
}
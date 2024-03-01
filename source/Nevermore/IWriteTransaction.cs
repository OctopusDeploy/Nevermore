using System.Threading;
using System.Threading.Tasks;

namespace Nevermore
{
    public interface IWriteTransaction : IReadTransaction, IWriteQueryExecutor
    {
        void CommitIfOwned();
        Task CommitIfOwnedAsync(CancellationToken cancellationToken = default);
        void Commit();
        Task CommitAsync(CancellationToken cancellationToken = default);
    }
}
using System.Threading;
using System.Threading.Tasks;

namespace Nevermore
{
    public interface IWriteTransaction : IReadTransaction, IWriteQueryExecutor
    {
        void Commit();
        Task CommitAsync(CancellationToken cancellationToken = default);
    }
}
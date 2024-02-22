using System.Threading;
using System.Threading.Tasks;

namespace Nevermore
{
    public interface IWriteTransaction : IReadTransaction, IWriteQueryExecutor
    {
        void TryCommit();
        Task TryCommitAsync(CancellationToken cancellationToken = default);
    }
}
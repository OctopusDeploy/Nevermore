using System.Threading.Tasks;

namespace Nevermore
{
    public interface IWriteTransaction : IReadTransaction, IWriteQueryExecutor
    {
        void Commit();
        Task CommitAsync();
    }
}
namespace Nevermore
{
    public interface IWriteTransaction : IReadTransaction, IWriteQueryExecutor
    {
        void Commit();
    }
}
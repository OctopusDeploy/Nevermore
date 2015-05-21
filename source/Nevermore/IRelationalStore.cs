using System.Data;

namespace Nevermore
{
    public interface IRelationalStore
    {
        string ConnectionString { get; }
        IRelationalTransaction BeginTransaction();
        IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel);
    }
}
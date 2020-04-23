using System.Data;
using System.Text;
using System.Threading.Tasks;
using Nevermore.Advanced;

namespace Nevermore
{
    public interface IRelationalStore
    {
        IRelationalStoreConfiguration Configuration { get; }
        void WriteCurrentTransactions(StringBuilder output);
        
        IReadTransaction BeginReadTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);
        Task<IReadTransaction> BeginReadTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select, string name = null);

        IWriteTransaction BeginWriteTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);
        Task<IWriteTransaction> BeginWriteTransactionAsync(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);

        IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);
    }
}
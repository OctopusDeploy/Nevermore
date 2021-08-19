using System.Data;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Advanced;

namespace Nevermore
{
    public interface IRelationalStore
    {
        IRelationalStoreConfiguration Configuration { get; }
        void WriteCurrentTransactions(StringBuilder output);
        
        IReadTransaction BeginReadTransaction(RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);
        Task<IReadTransaction> BeginReadTransactionAsync(RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);
        Task<IReadTransaction> BeginReadTransactionAsync(CancellationToken cancellationToken, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);

        IReadTransaction BeginReadTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);
        Task<IReadTransaction> BeginReadTransactionAsync(IsolationLevel isolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);
        Task<IReadTransaction> BeginReadTransactionAsync(CancellationToken cancellationToken, IsolationLevel isolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);

        IWriteTransaction BeginWriteTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);
        Task<IWriteTransaction> BeginWriteTransactionAsync(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);
        Task<IWriteTransaction> BeginWriteTransactionAsync(CancellationToken cancellationToken, IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);

        IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);
    }
}
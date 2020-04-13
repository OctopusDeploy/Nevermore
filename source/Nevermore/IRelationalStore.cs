using System;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Nevermore.Mapping;

namespace Nevermore
{
    public interface IRelationalStore
    {
        RelationalStoreConfiguration Configuration { get; }
        string ConnectionString { get; }
        int MaxPoolSize { get; }
        void WriteCurrentTransactions(StringBuilder sb);
        DocumentMap GetMappingFor<T>();
        DocumentMap GetMappingFor(Type type);
        
        IReadTransaction BeginReadTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);
        Task<IReadTransaction> BeginReadTransactionAsync(IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select, string name = null);

        IWriteTransaction BeginWriteTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);
        Task<IWriteTransaction> BeginWriteTransactionAsync(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);

        IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null);
    }
}
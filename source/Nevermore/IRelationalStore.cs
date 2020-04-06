using System;
using System.Data;
using System.Runtime.CompilerServices;
using System.Text;
using Nevermore.Mapping;

namespace Nevermore
{
    public interface IRelationalStore
    {
        string ConnectionString { get; }
        int MaxPoolSize { get; }
        void WriteCurrentTransactions(StringBuilder sb);
        DocumentMap GetMappingFor<T>();
        DocumentMap GetMappingFor(Type type);
        
        IReadTransaction BeginReadTransaction(RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select, [CallerMemberName]string name = null);
        IReadTransaction BeginReadTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select, [CallerMemberName]string name = null);
        
        IWriteTransaction BeginWriteTransaction(RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select, [CallerMemberName]string name = null);
        IWriteTransaction BeginWriteTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select, [CallerMemberName]string name = null);

        IRelationalTransaction BeginTransaction(RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select, [CallerMemberName]string name = null);
        IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select, [CallerMemberName]string name = null);
    }
}
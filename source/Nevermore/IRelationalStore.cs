using System;
using System.Data;
using System.Text;

namespace Nevermore
{
    public interface IRelationalStore
    {
        string ConnectionString { get; }
        int MaxPoolSize { get; }
        IDisposableRelationalTransaction BeginTransaction(RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select, string name = null);
        IDisposableRelationalTransaction BeginTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select, string name = null);
        void WriteCurrentTransactions(StringBuilder sb);
    }
}
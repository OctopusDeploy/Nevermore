using System;
using System.Data;

namespace Nevermore
{
    public interface IRelationalStore
    {
        string ConnectionString { get; }
        int MaxPoolSize { get; }
        IRelationalTransaction BeginTransaction(RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select, string name = null);
        IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select, string name = null);
    }
}
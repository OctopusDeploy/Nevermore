using System;
using System.Data;

namespace Nevermore
{
    public interface IRelationalStore
    {
        string ConnectionString { get; }
        IRelationalTransaction BeginTransaction(RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select);
        IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select);
    }
}
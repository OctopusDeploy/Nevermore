using System;
using System.Data;
using System.Text;
using Nevermore.Mapping;

namespace Nevermore
{
    public interface IRelationalStore
    {
        string ConnectionString { get; }
        int MaxPoolSize { get; }
        
        RelationalStoreConfiguration RelationalStoreConfiguration { get; }
        
        IRelationalTransaction BeginTransaction(RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select, string name = null);
        IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.Select, string name = null);
        void WriteCurrentTransactions(StringBuilder sb);
        DocumentMap GetMappingFor<T>();
        DocumentMap GetMappingFor(Type type);
    }
}
using System;
using System.Data;
#if NETFRAMEWORK
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Nevermore.Advanced;
using Nevermore.Mapping;

namespace Nevermore
{
    public class RelationalStore : IRelationalStore
    {
        readonly Lazy<RelationalTransactionRegistry> registry;
        readonly Lazy<KeyAllocator> keyAllocator;

        public RelationalStore(IRelationalStoreConfiguration configuration)
        {
            Configuration = configuration;
            registry = new Lazy<RelationalTransactionRegistry>(() => new RelationalTransactionRegistry(new SqlConnectionStringBuilder(configuration.ConnectionString)));
            keyAllocator = new Lazy<KeyAllocator>(() => new KeyAllocator(this, configuration.KeyBlockSize));
        }

        public void WriteCurrentTransactions(StringBuilder output) => registry.Value.WriteCurrentTransactions(output);

        public IRelationalStoreConfiguration Configuration { get; }

        public void Reset()
        {
            keyAllocator.Value.Reset();
        }

        public IReadTransaction BeginReadTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation, string name)
        {
            var txn = CreateReadTransaction(retriableOperation, name);
            txn.Open(isolationLevel);
            return txn;
        }

        public async Task<IReadTransaction> BeginReadTransactionAsync(IsolationLevel isolationLevel, RetriableOperation retriableOperation, string name)
        {
            var txn = CreateReadTransaction(retriableOperation, name);
            await txn.OpenAsync(isolationLevel);
            return txn;
        }

        public IWriteTransaction BeginWriteTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation, string name)
        {
            var txn = CreateWriteTransaction(retriableOperation, name);
            txn.Open(isolationLevel);
            return txn;
        }

        public async Task<IWriteTransaction> BeginWriteTransactionAsync(IsolationLevel isolationLevel, RetriableOperation retriableOperation, string name)
        {
            var txn = CreateWriteTransaction(retriableOperation, name);
            await txn.OpenAsync(isolationLevel);
            return txn;
        }

        public IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel, RetriableOperation retriableOperation, string name)
        {
            return (IRelationalTransaction)BeginWriteTransaction(isolationLevel, retriableOperation, name);
        }

        ReadTransaction CreateReadTransaction(RetriableOperation retriableOperation, string name)
        {
            return new ReadTransaction(registry.Value, retriableOperation, Configuration, name);
        }
        
        WriteTransaction CreateWriteTransaction(RetriableOperation retriableOperation, string name)
        {
            return new WriteTransaction(registry.Value, retriableOperation, Configuration, keyAllocator.Value, name);
        }
    }
}
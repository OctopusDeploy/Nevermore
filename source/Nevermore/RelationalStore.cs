using System;
using System.Data;
#if NETFRAMEWORK
using System.Data.SqlClient;
#else
using Microsoft.Data.SqlClient;
#endif
using System.Text;
using System.Threading;
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

        public IReadTransaction BeginReadTransaction(RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null)
        {
            var txn = CreateReadTransaction(retriableOperation, name);
            try
            {
                txn.Open();
                return txn;
            }
            catch
            {
                txn.Dispose();
                throw;
            }
        }

        public async Task<IReadTransaction> BeginReadTransactionAsync(CancellationToken cancellationToken, RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.None | RetriableOperation.Select,
            string name = null)
        {
            var txn = CreateReadTransaction(retriableOperation, name);

            try
            {
                await txn.OpenAsync(cancellationToken);
                return txn;
            }
            catch
            {
                await txn.DisposeAsync();
                throw;
            }
        }

        public IReadTransaction BeginReadTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null)
        {
            var txn = CreateReadTransaction(retriableOperation, name);

            try
            {
                txn.Open(isolationLevel);
                return txn;
            }
            catch
            {
                txn.Dispose();
                throw;
            }
        }

        public async Task<IReadTransaction> BeginReadTransactionAsync(CancellationToken cancellationToken, IsolationLevel isolationLevel, RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.None | RetriableOperation.Select, string name = null)
        {
            var txn = CreateReadTransaction(retriableOperation, name);
            try
            {
                await txn.OpenAsync(isolationLevel, cancellationToken);
                return txn;
            }
            catch
            {
                await txn.DisposeAsync();
                throw;
            }
        }

        public IWriteTransaction BeginWriteTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null)
        {
            var txn = CreateWriteTransaction(retriableOperation, name);
            try
            {
                txn.Open(isolationLevel);
                return txn;
            }
            catch
            {
                txn.Dispose();
                throw;
            }
        }

        public async Task<IWriteTransaction> BeginWriteTransactionAsync(CancellationToken cancellationToken, IsolationLevel isolationLevel = IsolationLevel.ReadCommitted, RetriableOperation retriableOperation = RetriableOperation.Delete | RetriableOperation.None | RetriableOperation.Select, string name = null)
        {
            var txn = CreateWriteTransaction(retriableOperation, name);
            try
            {
                await txn.OpenAsync(isolationLevel, cancellationToken);
                return txn;
            }
            catch
            {
                await txn.DisposeAsync();
                throw;
            }
        }

        public IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null)
        {
            return (IRelationalTransaction) BeginWriteTransaction(isolationLevel, retriableOperation, name);
        }

        public async Task<IRelationalTransaction> BeginTransactionAsync(CancellationToken cancellationToken, IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null)
        {
            return (IRelationalTransaction) await BeginWriteTransactionAsync(cancellationToken, isolationLevel, retriableOperation, name);
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
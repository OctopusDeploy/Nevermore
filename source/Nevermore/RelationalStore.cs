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
        readonly Lazy<IKeyAllocator> keyAllocator;

        public RelationalStore(IRelationalStoreConfiguration configuration)
        {
            Configuration = configuration;
            registry = new Lazy<RelationalTransactionRegistry>(() => new RelationalTransactionRegistry(new SqlConnectionStringBuilder(configuration.ConnectionString)));
            keyAllocator = new Lazy<IKeyAllocator>(configuration.KeyAllocatorFactory is not null ? () => configuration.KeyAllocatorFactory() : () => new KeyAllocator(this, configuration.KeyBlockSize));
        }

        public void WriteCurrentTransactions(StringBuilder output) => registry.Value.WriteCurrentTransactions(output);

        public IRelationalStoreConfiguration Configuration { get; }

        public void Reset()
        {
            keyAllocator.Value.Reset();
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

        public async Task<IReadTransaction> BeginReadTransactionAsync(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null, CancellationToken cancellationToken = default)
        {
            var txn = CreateReadTransaction(retriableOperation, name);
            try
            {
                await txn.OpenAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
                return txn;
            }
            catch
            {
                txn.Dispose();
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

        public async Task<IWriteTransaction> BeginWriteTransactionAsync(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null, CancellationToken cancellationToken = default)
        {
            var txn = CreateWriteTransaction(retriableOperation, name);
            try
            {
                await txn.OpenAsync(isolationLevel, cancellationToken).ConfigureAwait(false);
                return txn;
            }
            catch
            {
                txn.Dispose();
                throw;
            }
        }

        public IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string name = null)
        {
            return (IRelationalTransaction) BeginWriteTransaction(isolationLevel, retriableOperation, name);
        }

        ReadTransaction CreateReadTransaction(RetriableOperation retriableOperation, string name)
        {
            return new ReadTransaction(this, registry.Value, retriableOperation, Configuration, name);
        }

        WriteTransaction CreateWriteTransaction(RetriableOperation retriableOperation, string name)
        {
            return new WriteTransaction(this, registry.Value, retriableOperation, Configuration, keyAllocator.Value, name);
        }
    }
}
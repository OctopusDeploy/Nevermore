#nullable enable

using System;
using System.Data;
using System.Data.Common;
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

        IReadTransaction BeginReadTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string? name = null);
        Task<IReadTransaction> BeginReadTransactionAsync(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string? name = null, CancellationToken cancellationToken = default);

        IWriteTransaction BeginWriteTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string? name = null);
        Task<IWriteTransaction> BeginWriteTransactionAsync(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string? name = null, CancellationToken cancellationToken = default);

        IRelationalTransaction BeginTransaction(IsolationLevel isolationLevel = NevermoreDefaults.IsolationLevel, RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations, string? name = null);

        IReadTransaction CreateReadTransactionFromExistingConnectionAndTransaction(
            DbConnection existingConnection,
            DbTransaction existingTransaction,
            IRelationalTransactionRegistry? customRelationalTransactionRegistry = null,
            Action<string>? customCommandTrace = null,
            RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations,
            string? name = null);

        IWriteTransaction CreateWriteTransactionFromExistingConnectionAndTransaction(
            DbConnection existingConnection,
            DbTransaction existingTransaction,
            IRelationalTransactionRegistry? customRelationalTransactionRegistry = null,
            Action<string>? customCommandTrace = null,
            RetriableOperation retriableOperation = NevermoreDefaults.RetriableOperations,
            string? name = null);
    }
}
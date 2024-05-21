using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Transient;
using Nito.AsyncEx;

namespace Nevermore.Mapping
{
    public class KeyAllocator : IKeyAllocator
    {
        readonly IRelationalStore store;
        readonly int blockSize;
        readonly ConcurrentDictionary<string, Allocation> allocations = new(StringComparer.OrdinalIgnoreCase);

        public KeyAllocator(IRelationalStore store, int blockSize)
        {
            this.store = store;
            this.blockSize = blockSize;
        }

        public void Reset()
            => allocations.Clear();

        Allocation GetAllocation(string tableName)
            => allocations.GetOrAdd(tableName, t => new Allocation(store, t, blockSize));

        public long NextId(string tableName)
            => GetAllocation(tableName).Next();

        public ValueTask<long> NextIdAsync(string tableName, CancellationToken cancellationToken)
            => GetAllocation(tableName).NextAsync(cancellationToken);

        class Allocation
        {
            readonly IRelationalStore store;
            readonly string collectionName;
            readonly int blockSize;
            readonly SemaphoreSlim sync = new(1, 1);
            long blockStart;
            long blockNext;
            long blockFinish;

            public Allocation(IRelationalStore store, string collectionName, int blockSize)
            {
                this.store = store;
                this.collectionName = collectionName;
                this.blockSize = blockSize;
            }

            public async ValueTask<long> NextAsync(CancellationToken cancellationToken)
            {
                using var releaseLock = await sync.LockAsync(cancellationToken);

                async Task<long> GetNextMaxValue(CancellationToken ct)
                {
                    using var transaction = await store.BeginWriteTransactionAsync(IsolationLevel.Serializable, name: $"{nameof(KeyAllocator)}.{nameof(Allocation)}.{nameof(GetNextMaxValue)}", cancellationToken: ct).ConfigureAwait(false);
                    var parameters = new CommandParameterValues
                    {
                        { "collectionName", collectionName },
                        { "blockSize", blockSize }
                    };
                    parameters.CommandType = CommandType.StoredProcedure;

                    var result = await transaction.ExecuteScalarAsync<object>("GetNextKeyBlock", parameters, cancellationToken: ct).ConfigureAwait(false);
                    await transaction.CommitAsync(ct).ConfigureAwait(false);
                    // Older versions of the GetNextKeyBlock stored proc and KeyAllocation table might be using 32-bit ID's
                    // The type-check here lets us remain compatible with that while supporting 64-bit ID's as well
                    return result is int i ? i : (long)result;
                }

                if (blockNext == blockFinish)
                {
                    await GetRetryPolicy().ExecuteActionAsync(async ct =>
                    {
                        var max = await GetNextMaxValue(ct).ConfigureAwait(false);
                        SetRange(max);
                    }, cancellationToken).ConfigureAwait(false);
                }

                return blockNext++;
            }

            public long Next()
            {
                using var releaseLock = sync.Lock();

                long GetNextMaxValue()
                {
                    using var transaction = store.BeginWriteTransaction(IsolationLevel.Serializable, name: $"{nameof(KeyAllocator)}.{nameof(Allocation)}.{nameof(GetNextMaxValue)}");
                    var parameters = new CommandParameterValues
                    {
                        { "collectionName", collectionName },
                        { "blockSize", blockSize }
                    };
                    parameters.CommandType = CommandType.StoredProcedure;

                    var result = transaction.ExecuteScalar<object>("GetNextKeyBlock", parameters);
                    transaction.Commit();
                    return result is int i ? i : (long)result; // 32/64-bit compatibility, see NextAsync() for explanation
                }

                if (blockNext == blockFinish)
                {
                    GetRetryPolicy().ExecuteAction(() =>
                    {
                        var max = GetNextMaxValue();
                        SetRange(max);
                    });
                }

                return blockNext++;
            }

            RetryPolicy GetRetryPolicy()
            {
                return new RetryPolicy(new SqlDatabaseTransientErrorDetectionStrategy(),
                        TransientFaultHandling.FastIncremental)
                    .LoggingRetries("Extending key allocation");
            }

            /// <remarks>
            /// Must only ever be executed while protected by the <see cref="sync"/> mutex!
            /// </remarks>
            void SetRange(long max)
            {
                var first = (max - blockSize) + 1;
                blockStart = first;
                blockNext = first;
                blockFinish = max + 1;
            }

            public override string ToString() => $"{blockStart} to {blockNext} (next: {blockFinish})";
        }
    }
}
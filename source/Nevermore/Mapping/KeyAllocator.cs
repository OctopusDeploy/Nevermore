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
        {
            allocations.Clear();
        }

        public int NextId(string tableName)
        {
            var allocation = allocations.GetOrAdd(tableName, _ => new Allocation(store, tableName, blockSize));
            return allocation.Next();
        }

        public async Task<int> NextIdAsync(string tableName, CancellationToken cancellationToken)
        {
            var allocation = allocations.GetOrAdd(tableName, _ => new Allocation(store, tableName, blockSize));
            return await allocation.NextAsync(cancellationToken).ConfigureAwait(false);
        }

        class Allocation
        {
            readonly IRelationalStore store;
            readonly string collectionName;
            readonly int blockSize;
            readonly SemaphoreSlim sync = new(1, 1);
            int blockStart;
            int blockNext;
            int blockFinish;

            public Allocation(IRelationalStore store, string collectionName, int blockSize)
            {
                this.store = store;
                this.collectionName = collectionName;
                this.blockSize = blockSize;
            }

            public async Task<int> NextAsync(CancellationToken cancellationToken)
            {
                using (await sync.LockAsync(cancellationToken))
                {
                    async Task<int> GetNextMaxValue(CancellationToken ct)
                    {
                        using var transaction = await store.BeginWriteTransactionAsync(IsolationLevel.Serializable, name: $"{nameof(KeyAllocator)}.{nameof(Allocation)}.{nameof(GetNextMaxValue)}", cancellationToken: ct).ConfigureAwait(false);
                        var parameters = new CommandParameterValues
                        {
                            { "collectionName", collectionName },
                            { "blockSize", blockSize }
                        };
                        parameters.CommandType = CommandType.StoredProcedure;

                        var result = await transaction.ExecuteScalarAsync<int>("GetNextKeyBlock", parameters, cancellationToken: ct).ConfigureAwait(false);
                        await transaction.CommitAsync(ct).ConfigureAwait(false);
                        return result;
                    }

                    async Task ExtendAllocation(CancellationToken ct)
                    {
                        var max = await GetNextMaxValue(ct).ConfigureAwait(false);
                        SetRange(max);
                    }

                    if (blockNext == blockFinish)
                    {
                        await GetRetryPolicy().ExecuteActionAsync(ExtendAllocation, cancellationToken).ConfigureAwait(false);
                    }

                    var result = blockNext;
                    blockNext++;

                    return result;
                }
            }

            public int Next()
            {
                using (sync.Lock())
                {
                    int GetNextMaxValue()
                    {
                        using var transaction = store.BeginWriteTransaction(IsolationLevel.Serializable, name: $"{nameof(KeyAllocator)}.{nameof(Allocation)}.{nameof(GetNextMaxValue)}");
                        var parameters = new CommandParameterValues
                        {
                            {"collectionName", collectionName},
                            {"blockSize", blockSize}
                        };
                        parameters.CommandType = CommandType.StoredProcedure;

                        var result = transaction.ExecuteScalar<int>("GetNextKeyBlock", parameters);
                        transaction.Commit();
                        return result;
                    }

                    void ExtendAllocation()
                    {
                        var max = GetNextMaxValue();
                        SetRange(max);
                    }

                    if (blockNext == blockFinish)
                    {
                        GetRetryPolicy().ExecuteAction(ExtendAllocation);
                    }

                    var result = blockNext;
                    blockNext++;

                    return result;
                }
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
            void SetRange(int max)
            {
                var first = (max - blockSize) + 1;
                blockStart = first;
                blockNext = first;
                blockFinish = max + 1;
            }

            public override string ToString()
            {
                return $"{blockStart} to {blockNext} (next: {blockFinish})";
            }
        }
    }
}
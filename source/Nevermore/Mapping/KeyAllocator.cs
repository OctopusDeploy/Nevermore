using System;
using System.Collections.Generic;
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
        readonly Dictionary<string, Allocation> allocations = new(StringComparer.OrdinalIgnoreCase);

        public KeyAllocator(IRelationalStore store, int blockSize)
        {
            this.store = store;
            this.blockSize = blockSize;
        }

        public void Reset()
        {
            lock(allocations) allocations.Clear();
        }

        Allocation GetAllocation(string tableName)
        {
            lock (allocations)
            {
                if (allocations.TryGetValue(tableName, out var allocation)) return allocation;
                allocation = new Allocation(store, tableName, blockSize);
                allocations.Add(tableName, allocation);
                return allocation;
            }
        }

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
                using (await sync.LockAsync(cancellationToken))
                {
                    async Task<long> GetNextMaxValue(CancellationToken ct)
                    {
                        using var transaction = await store.BeginWriteTransactionAsync(IsolationLevel.Serializable, name: $"{nameof(KeyAllocator)}.{nameof(Allocation)}.{nameof(GetNextMaxValue)}", cancellationToken: ct).ConfigureAwait(false);
                        var parameters = new CommandParameterValues
                        {
                            { "collectionName", collectionName },
                            { "blockSize", blockSize }
                        };
                        parameters.CommandType = CommandType.StoredProcedure;

                        var result = await transaction.ExecuteScalarAsync<long>("GetNextKeyBlock", parameters, cancellationToken: ct).ConfigureAwait(false);
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

            public long Next()
            {
                using (sync.Lock())
                {
                    long GetNextMaxValue()
                    {
                        using var transaction = store.BeginWriteTransaction(IsolationLevel.Serializable, name: $"{nameof(KeyAllocator)}.{nameof(Allocation)}.{nameof(GetNextMaxValue)}");
                        var parameters = new CommandParameterValues
                        {
                            {"collectionName", collectionName},
                            {"blockSize", blockSize}
                        };
                        parameters.CommandType = CommandType.StoredProcedure;

                        var result = transaction.ExecuteScalar<long>("GetNextKeyBlock", parameters);
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
            void SetRange(long max)
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
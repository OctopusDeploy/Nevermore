using System;
using System.Collections.Concurrent;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.Transient;

namespace Nevermore.Mapping
{
    public class KeyAllocator : IKeyAllocator
    {
        readonly IRelationalStore store;
        readonly int blockSize;

        readonly ConcurrentDictionary<string, Lazy<Allocation>> allocations = new ConcurrentDictionary<string, Lazy<Allocation>>(StringComparer.OrdinalIgnoreCase);

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
            var allocation = GetAllocation(tableName);
            return allocation.Next();
        }

        public async Task<int> NextIdAsync(string tableName, CancellationToken cancellationToken = default)
        {
            var allocation = GetAllocation(tableName);
            return await allocation.NextAsync(cancellationToken);
        }

        Allocation GetAllocation(string tableName)
        {
            var allocation = allocations.GetOrAdd(tableName, _ => new Lazy<Allocation>(() => new Allocation(store, tableName, blockSize)));
            return allocation.Value;
        }

        class Allocation
        {
            readonly IRelationalStore store;
            readonly string collectionName;
            readonly int blockSize;
            readonly SemaphoreSlim semaphore;
            int blockStart;
            int blockNext;
            int blockFinish;

            public Allocation(IRelationalStore store, string collectionName, int blockSize)
            {
                this.store = store;
                this.collectionName = collectionName;
                this.blockSize = blockSize;
                semaphore = new SemaphoreSlim(1, 1);
            }

            public int Next()
            {
                if (blockNext == blockFinish)
                    GetRetryPolicy().ExecuteAction(ExtendAllocation);

                return GetNextId();
            }

            public async Task<int> NextAsync(CancellationToken cancellationToken)
            {
                if (blockNext == blockFinish)
                    await GetRetryPolicy().ExecuteActionAsync(async () => await ExtendAllocationAsync(cancellationToken));

                return GetNextId();
            }

            int GetNextId()
            {
                var incrementedValue = Interlocked.Increment(ref blockNext);
                //our value is always 1 less than the incremented value
                return incrementedValue - 1;
            }

            static RetryPolicy GetRetryPolicy()
            {
                return new RetryPolicy(new SqlDatabaseTransientErrorDetectionStrategy(),
                        TransientFaultHandling.FastIncremental)
                    .LoggingRetries("Extending key allocation");
            }

            void ExtendAllocation()
            {
                semaphore.Wait();

                try
                {
                    //2 threads were waiting, and one of them has successfully incremented the blocks, so we can jump out
                    if (blockNext != blockFinish)
                        return;

                    var max = GetNextMaxValue();
                    SetRange(max);
                }
                finally
                {
                    semaphore.Release();
                }
            }

            async Task ExtendAllocationAsync(CancellationToken cancellationToken)
            {
                await semaphore.WaitAsync(cancellationToken);

                try
                {
                    //2 threads were waiting, and one of them has successfully incremented the blocks, so we can jump out
                    if (blockNext != blockFinish)
                        return;

                    var max = await GetNextMaxValueAsync(cancellationToken);
                    SetRange(max);
                }
                finally
                {
                    semaphore.Release();
                }
            }

            void SetRange(int max)
            {
                var first = (max - blockSize) + 1;
                Interlocked.Exchange(ref blockStart, first);
                Interlocked.Exchange(ref blockNext, first);
                Interlocked.Exchange(ref blockFinish, max + 1);
            }

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

            async Task<int> GetNextMaxValueAsync(CancellationToken cancellationToken)
            {
                using var transaction = await store.BeginWriteTransactionAsync(IsolationLevel.Serializable, name: $"{nameof(KeyAllocator)}.{nameof(Allocation)}.{nameof(GetNextMaxValueAsync)}");
                var parameters = new CommandParameterValues
                {
                    {"collectionName", collectionName},
                    {"blockSize", blockSize}
                };
                parameters.CommandType = CommandType.StoredProcedure;

                var result = await transaction.ExecuteScalarAsync<int>("GetNextKeyBlock", parameters, cancellationToken: cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return result;
            }

            public override string ToString()
            {
                return $"{blockStart} to {blockNext} (next: {blockFinish})";
            }
        }
    }
}
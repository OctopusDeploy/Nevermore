using System;
using System.Collections.Concurrent;
using System.Data;
using Nevermore.Diagnositcs;
using Nevermore.Transient;

#nullable enable

namespace Nevermore.Mapping
{
    public class KeyAllocator : IKeyAllocator
    {
        readonly Guid uuid = Guid.NewGuid();
        readonly IRelationalStore store;
        readonly int blockSize;
        readonly ILog? log;
        readonly ConcurrentDictionary<string, Allocation> allocations = new(StringComparer.OrdinalIgnoreCase);

        public KeyAllocator(IRelationalStore store, int blockSize, bool loggingEnabled = false)
        {
            this.store = store;
            this.blockSize = blockSize;
            if (loggingEnabled)
            {
                log = LogProvider.For<KeyAllocator>();
            }
        }

        public void Reset()
        {
            allocations.Clear();
        }

        public int NextId(string tableName)
        {
            var allocation = allocations.GetOrAdd(tableName, _ => new Allocation(store, tableName, blockSize, uuid, log));
            var result = allocation.Next();
            return result;
        }

        class Allocation
        {
            readonly IRelationalStore store;
            readonly ILog? log;
            readonly string collectionName;
            readonly int blockSize;
            readonly Guid uuid = Guid.NewGuid();
            readonly Guid parentId;
            readonly object sync = new object();
            volatile int blockStart;
            volatile int blockNext;
            volatile int blockFinish;

            public Allocation(IRelationalStore store, string collectionName, int blockSize, Guid parentId, ILog? log)
            {
                this.store = store;
                this.collectionName = collectionName;
                this.blockSize = blockSize;
                this.parentId = parentId;
                this.log = log;
                log?.DebugFormat($"{nameof(KeyAllocator)} {{0}} {nameof(Allocation)} {{1}} Collection {{2}} was created. BlockSize: {{3}}", parentId, uuid, collectionName, blockSize);
            }

            public int Next()
            {
                lock (sync)
                {
                    if (blockNext == blockFinish)
                        GetRetryPolicy().ExecuteAction(ExtendAllocation);

                    var result = blockNext;
                    blockNext++;
                    log?.DebugFormat($"{nameof(KeyAllocator)} {{0}} {nameof(Allocation)} {{1}} Collection {{2}} allocated {nameof(NextId)}: {{3}}", parentId, uuid, collectionName, result);
                    return result;
                }
            }

            RetryPolicy GetRetryPolicy()
            {
                return new RetryPolicy(new SqlDatabaseTransientErrorDetectionStrategy(),
                    TransientFaultHandling.FastIncremental)
                    .LoggingRetries("Extending key allocation");
            }

            void ExtendAllocation()
            {
                var max = GetNextMaxValue();
                SetRange(max);
                log?.DebugFormat($"{nameof(KeyAllocator)} {{0}} {nameof(Allocation)} {{1}} Collection {{2}} extended allocation to {{3}}", parentId, uuid, collectionName, max);
            }

            void SetRange(int max)
            {
                var first = (max - blockSize) + 1;
                blockStart = first;
                blockNext = first;
                blockFinish = max + 1;
            }

            int GetNextMaxValue()
            {
                using (var transaction = store.BeginWriteTransaction(IsolationLevel.Serializable))
                {
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
            }

            public override string ToString()
            {
                return string.Format("{0} to {1} (next: {2})", blockStart, blockNext, blockFinish);
            }
        }
    }
}
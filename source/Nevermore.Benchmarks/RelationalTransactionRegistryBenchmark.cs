using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Nevermore.Advanced;

namespace Nevermore.Benchmarks;

public class RelationalTransactionRegistryBenchmark
{
    readonly RelationalStoreConfiguration config = new("");
    RelationalTransactionRegistry registry;
    List<ReadTransaction> transactions;

    [Params(5_000, 100_000)]
    public int TransactionCount { get; set; }

    [IterationSetup(Target = nameof(AddTransactions))]
    public void SetupAddBenchmark()
    {
        registry = new RelationalTransactionRegistry(maxSqlConnectionPoolSize: int.MaxValue);
    }

    [Benchmark]
    public void AddTransactions()
    {
        for (var i = 0; i < TransactionCount; i++)
        {
            _ = new ReadTransaction(null!, registry, RetriableOperation.None, config);
        }
    }

    [IterationSetup(Target = nameof(RemoveTransactions))]
    public void SetupRemovalBenchmark()
    {
        registry = new RelationalTransactionRegistry(maxSqlConnectionPoolSize: int.MaxValue);
        transactions = new List<ReadTransaction>(capacity: TransactionCount);

        for (var i = 0; i < TransactionCount; i++)
        {
            transactions.Add(new ReadTransaction(null!, registry, RetriableOperation.None, config));
        }
    }

    [Benchmark]
    public void RemoveTransactions()
    {
        foreach (var t in transactions)
        {
            registry.Remove(t);
        }
    }
}

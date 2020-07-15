using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Nevermore.Benchmarks.Model;
using Nevermore.Benchmarks.SetUp;

namespace Nevermore.Benchmarks
{
    public class LoadManyBenchmark : BenchmarkBase
    {
        IRelationalStore store;
        IReadTransaction transaction;
        List<string> allIdsRandomlySorted;
        
        public override void SetUp()
        {
            base.SetUp();
            var config = new RelationalStoreConfiguration(ConnectionString);
            config.DocumentMaps.Register(new CustomerMap());

            store = new RelationalStore(config);
            transaction = store.BeginReadTransaction();

            allIdsRandomlySorted = transaction.Query<Customer>().ToList().Select(p => p.Id).OrderByDescending(p => Guid.NewGuid()).ToList();
        }
        
        [Params(100, 1000, 10000, 50000)]
        public int NumberToLoad { get; set; }
        
        [Benchmark]
        public List<Customer> LoadMany()
        {
            var result = transaction.LoadMany<Customer>(allIdsRandomlySorted.Take(NumberToLoad).ToArray());
            if (result.Count != NumberToLoad)
                throw new Exception();
            return result;
        }
    }
}
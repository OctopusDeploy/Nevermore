using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Nevermore.Benchmarks.Model;
using Nevermore.Benchmarks.SetUp;

namespace Nevermore.Benchmarks
{
    [Description("Nevermore")]
    public class NevermoreBenchmark : BenchmarkBase
    {
        IRelationalStore store;
        IReadTransaction transaction;

        public override void SetUp()
        {
            base.SetUp();

            var config = new RelationalStoreConfiguration(ConnectionString);
            config.Mappings.Register(new CustomerMap());
            
            store = new RelationalStore(config);
            transaction = store.BeginReadTransaction();
        }

        [Benchmark(Description = "List 100 posts")]
        public List<Post> List100Posts()
        {
            var results = transaction.Stream<Post>("select Top 100 * from Posts").ToList();
            
            if (results.Count != 100)
                throw new Exception("Incorrect results");

            return results;
        }

        [Benchmark(Description = "List 50 tuples")]
        public List<(string Id, long PostLength)> List50Tuples()
        {
            var results = transaction.Stream<(string Id, long PostLength)>("select Top 50 Id, Len([Text]) as PostLength from Posts").ToList();
            
            if (results.Count != 50)
                throw new Exception("Incorrect results");

            return results;
        }

        [Benchmark(Description = "List 100 primitives")]
        public List<long?> List100Primitives()
        {
            var results = transaction.Stream<long?>("select top 100 Len([Text]) as PostLength from Posts").ToList();
            
            if (results.Count != 100)
                throw new Exception("Incorrect results");

            return results;
        }

        [Benchmark(Description = "List customer documents - stream")]
        public List<Customer> List100CustomersStream()
        {
            var results = transaction.Stream<Customer>("select top 100 * from dbo.Customer").ToList();
            
            if (results.Count != 100)
                throw new Exception("Incorrect results");

            return results;
        }
        
        [Benchmark(Description = "List customer documents - query")]
        public List<Customer> List100CustomersQuery()
        {
            var results = transaction.Query<Customer>().Take(100).ToList();
            
            if (results.Count != 100)
                throw new Exception("Incorrect results");

            return results;
        }
    }
}
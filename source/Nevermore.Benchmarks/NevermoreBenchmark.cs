using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Nevermore.Benchmarks.Model;
using Nevermore.Benchmarks.SetUp;

namespace Nevermore.Benchmarks
{
    public class NevermoreBenchmark : BenchmarkBase
    {
        IRelationalStore store;
        IReadTransaction transaction;

        public override void SetUp()
        {
            base.SetUp();

            var config = new RelationalStoreConfiguration(ConnectionString);
            config.DocumentMaps.Register(new CustomerMap());
            
            store = new RelationalStore(config);
            transaction = store.BeginReadTransaction();
        }

        [Benchmark]
        public List<Post> List100Posts()
        {
            var results = transaction.Stream<Post>("select Top 100 * from Posts").ToList();
            
            if (results.Count != 100)
                throw new Exception("Incorrect results");

            return results;
        }

        [Benchmark]
        public List<(string Id, long PostLength)> List50Tuples()
        {
            var results = transaction.Stream<(string Id, long PostLength)>("select Top 50 Id, Len([Text]) as PostLength from Posts").ToList();
            
            if (results.Count != 50)
                throw new Exception("Incorrect results");

            return results;
        }

        [Benchmark]
        public List<long?> List100Primitives()
        {
            var results = transaction.Stream<long?>("select top 100 Len([Text]) as PostLength from Posts").ToList();
            
            if (results.Count != 100)
                throw new Exception("Incorrect results");

            return results;
        }

        [Benchmark(Baseline = true)]
        public List<Customer> List100CustomersStream()
        {
            return EnsureResults(
                transaction.Stream<Customer>("select top 100 * from dbo.Customer where FirstName = @name", new CommandParameterValues { { "name", "Robert" } }).ToList()
            );
        }
        
        [Benchmark]
        public List<Customer> List100CustomersQueryWhereText()
        {
            return EnsureResults(
                transaction.TableQuery<Customer>().Where("FirstName = @name").Parameter("name", "Robert").Take(100).ToList()
            );
        }
        
        [Benchmark]
        public List<Customer> List100CustomersQueryWhereOperand()
        {
            return EnsureResults(
                transaction.TableQuery<Customer>().Where("FirstName", UnarySqlOperand.Equal, "Robert").Take(100).ToList()
            );
        }
        
        [Benchmark]
        public List<Customer> List100CustomersQueryWhereLinq()
        {
            return EnsureResults(
                transaction.TableQuery<Customer>().Where(c => c.FirstName == "Robert").Take(100).ToList()
            );
        }

        static List<Customer> EnsureResults(List<Customer> results)
        {
            if (results.Count != 100)
                throw new Exception("Expected 100 results, got: " + results.Count);

            return results;
        }
    }
}
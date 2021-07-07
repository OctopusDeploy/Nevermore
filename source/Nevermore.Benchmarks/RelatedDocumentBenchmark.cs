using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Nevermore.Benchmarks.Model;
using Nevermore.Benchmarks.SetUp;

namespace Nevermore.Benchmarks
{
    public class RelatedDocumentBenchmark : BenchmarkBase
    {
        IRelationalStore store;
        IReadTransaction transaction;

        public override void SetUp()
        {
            base.SetUp();

            var config = new RelationalStoreConfiguration(ConnectionString);
            config.DocumentMaps.Register(new CustomerMap());
            config.DocumentMaps.Register(new OrderMap());

            store = new RelationalStore(config);
            transaction = store.BeginReadTransaction();
        }

        [Benchmark]
        public void Insert100OrdersWith2000RelatedDocuments()
        {
            using var writer = store.BeginWriteTransaction();

            for (int i = 1; i <= 500; i++)
            {
                var customers = Enumerable.Range(1, 2000).Select(i => "Customer-" + i);
                var order = new Order(customers.Select(c => (c, typeof(Customer))))
                {
                    Name = "Order " + i,
                    Price = i
                };

                writer.Insert(order);
            }

            writer.Commit();
        }
    }
}
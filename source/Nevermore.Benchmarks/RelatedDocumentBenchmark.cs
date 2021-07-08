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
        public void Insert100OrdersWith10RelatedDocuments()
        {
            using var writer = store.BeginWriteTransaction();

            for (int i = 1; i <= 100; i++)
            {
                var customers = Enumerable.Range(1, 10).Select(i => "Customer-" + i);
                var order = new Order(customers.Select(c => (c, typeof(Customer))))
                {
                    Name = "Order " + i,
                    Price = i
                };

                writer.Insert(order);
            }

            writer.Commit();
        }

        [Benchmark]
        public void Insert100OrdersWith20RelatedDocuments()
        {
            using var writer = store.BeginWriteTransaction();

            for (int i = 1; i <= 100; i++)
            {
                var customers = Enumerable.Range(1, 20).Select(i => "Customer-" + i);
                var order = new Order(customers.Select(c => (c, typeof(Customer))))
                {
                    Name = "Order " + i,
                    Price = i
                };

                writer.Insert(order);
            }

            writer.Commit();
        }

        [Benchmark]
        public void Insert100OrdersWith50RelatedDocuments()
        {
            using var writer = store.BeginWriteTransaction();

            for (int i = 1; i <= 100; i++)
            {
                var customers = Enumerable.Range(1, 50).Select(i => "Customer-" + i);
                var order = new Order(customers.Select(c => (c, typeof(Customer))))
                {
                    Name = "Order " + i,
                    Price = i
                };

                writer.Insert(order);
            }

            writer.Commit();
        }

        [Benchmark]
        public void Insert100OrdersWith80RelatedDocuments()
        {
            using var writer = store.BeginWriteTransaction();

            for (int i = 1; i <= 100; i++)
            {
                var customers = Enumerable.Range(1, 80).Select(i => "Customer-" + i);
                var order = new Order(customers.Select(c => (c, typeof(Customer))))
                {
                    Name = "Order " + i,
                    Price = i
                };

                writer.Insert(order);
            }

            writer.Commit();
        }

        [Benchmark]
        public void Insert100OrdersWith100RelatedDocuments()
        {
            using var writer = store.BeginWriteTransaction();

            for (int i = 1; i <= 100; i++)
            {
                var customers = Enumerable.Range(1, 100).Select(i => "Customer-" + i);
                var order = new Order(customers.Select(c => (c, typeof(Customer))))
                {
                    Name = "Order " + i,
                    Price = i
                };

                writer.Insert(order);
            }

            writer.Commit();
        }

        [Benchmark]
        public void Insert100OrdersWith500RelatedDocuments()
        {
            using var writer = store.BeginWriteTransaction();

            for (int i = 1; i <= 100; i++)
            {
                var customers = Enumerable.Range(1, 500).Select(i => "Customer-" + i);
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
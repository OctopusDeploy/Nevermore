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
        IEnumerable<Order> documentsToUpdate;

        public override void SetUp()
        {
            base.SetUp();

            var config = new RelationalStoreConfiguration(ConnectionString);
            config.DocumentMaps.Register(new CustomerMap());
            config.DocumentMaps.Register(new OrderMap());

            store = new RelationalStore(config);

            //These are for the update tests
            InsertDocuments(100, 55);
            using var transaction = store.BeginReadTransaction();
            documentsToUpdate = transaction.Query<Order>().ToList();
        }

        [Benchmark]
        public void Insert100OrdersWith10RelatedDocuments()
        {
            InsertDocuments(100, 10);
        }

        [Benchmark]
        public void Insert100OrdersWith20RelatedDocuments()
        {
            InsertDocuments(100, 20);
        }

        [Benchmark]
        public void Insert100OrdersWith50RelatedDocuments()
        {
            InsertDocuments(100, 50);
        }

        [Benchmark]
        public void Insert100OrdersWith80RelatedDocuments()
        {
            InsertDocuments(100, 80);
        }

        [Benchmark]
        public void Insert100OrdersWith100RelatedDocuments()
        {
            InsertDocuments(100, 100);
        }

        [Benchmark]
        public void Insert100OrdersWith500RelatedDocuments()
        {
            InsertDocuments(100, 500);
        }

        [Benchmark]
        public void Update100OrdersWith10RelatedDocuments()
        {
            UpdateDocuments(10);
        }

        [Benchmark]
        public void Update100OrdersWith20RelatedDocuments()
        {
            UpdateDocuments(20);
        }

        [Benchmark]
        public void Update100OrdersWith50RelatedDocuments()
        {
            UpdateDocuments(50);
        }

        [Benchmark]
        public void Update100OrdersWith80RelatedDocuments()
        {
            UpdateDocuments(80);
        }

        [Benchmark]
        public void Update100OrdersWith100RelatedDocuments()
        {
            UpdateDocuments(100);
        }
        [Benchmark]
        public void Update100OrdersWith300RelatedDocuments()
        {
            UpdateDocuments(300);
        }

        [Benchmark]
        public void Update100OrdersWith500RelatedDocuments()
        {
            UpdateDocuments(500);
        }
        [Benchmark]
        public void Update100OrdersWith700RelatedDocuments()
        {
            UpdateDocuments(700);
        }

        void UpdateDocuments(int numberOfRelatedDocuments)
        {

            Random rand = new Random();
            foreach (var document in documentsToUpdate)
            {
                using var writer = store.BeginWriteTransaction();
                if (document.SerializedRelatedDocuments.Count() > numberOfRelatedDocuments)
                {
                    var newCustomers = Enumerable.Range(1, 7).Select(i => "Customer-" + (rand.Next(1000)+5000)).Select(c => (c, typeof(Customer)));
                    document.SerializedRelatedDocuments = newCustomers.Concat(document.RelatedDocuments).Take(numberOfRelatedDocuments).ToArray();
                }
                else
                {
                    var newCustomers = Enumerable.Range(1, numberOfRelatedDocuments).Select(i => "Customer-" + (rand.Next(1000)+5000)).Select(c => (c, typeof(Customer)));
                    document.SerializedRelatedDocuments = document.RelatedDocuments.Concat(newCustomers).Take(numberOfRelatedDocuments).ToArray();
                }

                writer.Update(document);
                writer.TryCommit();
            }
        }

        void InsertDocuments(int numberOfDocuments, int numberOfRelatedDocuments)
        {
            using var writer = store.BeginWriteTransaction();

            Random rand = new Random();
            for (int i = 1; i <= numberOfDocuments; i++)
            {
                var customers = Enumerable.Range(1, numberOfRelatedDocuments).Select(i => ("Customer-" + rand.Next(1000), typeof(Customer))).ToArray();
                var order = new Order()
                {
                    Name = "Order " + i,
                    Price = i,
                    SerializedRelatedDocuments = customers
                };

                writer.Insert(order);
            }

            writer.TryCommit();
        }
    }
}
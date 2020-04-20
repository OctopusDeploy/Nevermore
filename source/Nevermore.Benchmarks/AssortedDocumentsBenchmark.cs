using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using BenchmarkDotNet.Attributes;
using Nevermore.Benchmarks.Model;
using Nevermore.Benchmarks.SetUp;
using Nevermore.Mapping;

namespace Nevermore.Benchmarks
{
    public class AssortedDocumentsBenchmark : BenchmarkBase
    {
        IRelationalStore store;
        IReadTransaction readTransaction;

        public override void SetUp()
        {
            base.SetUp();
            var config = new RelationalStoreConfiguration(ConnectionString);
            config.Mappings.Register(new BigObjectMap(JsonStorageFormat.TextOnly));

            store = new RelationalStore(config);
            readTransaction = store.BeginReadTransaction();

            using var writer = store.BeginWriteTransaction();

            var history = GenerateHistory();
            var doc1 = history.Take(1).ToList();
            var doc10 = history.Take(10).ToList();
            var doc100 = history.Take(100).ToList();
            var doc500 = history.Take(500).ToList();
            
            var rand = new Random(42);
            
            for (var i = 0; i < 1000; i++)
            {
                var doc = new BigObject {Name = "Document " + i};
                
                var distribution = rand.Next(1, 100);
                if (distribution < 70) doc.History = doc1;
                else if (distribution < 85) doc.History = doc10;
                else if (distribution < 95) doc.History = doc100;
                else if (distribution <= 100) doc.History = doc500;
 
                writer.Insert(doc, new InsertOptions { CommandTimeout = TimeSpan.FromSeconds(180) });
                if (i % 100 == 0)
                {
                    Console.WriteLine($"Inserted: {i} history: {doc.History.Count} rand: {distribution}");
                }
            }

            foreach (var item in writer.Stream<(long? Bucket, int? Count)>(
                "select len([JSON]) as Bucket, count(*) from BigObject group by len([JSON]) order by len([JSON])"))
            {
                Console.WriteLine($"{item.Bucket} bytes: {item.Count} documents");
            }

            writer.Commit();
        }

        static List<object> GenerateHistory()
        {
            return Enumerable.Range(1, 500).Select(n => new BigObjectHistoryEntry {Id = Guid.NewGuid(), Comment = new string('N', 256), Date = DateTime.Today.AddDays(n)}).OfType<object>().ToList();
        }

        [Benchmark]
        public void QueryDocumentsManySizes()
        {
            var count = readTransaction.Stream<BigObject>("select * from dbo.BigObject").Count();
            if (count != 1000)
                throw new Exception($"Didn't get 1000 items! Got {count} instead?!");
        }

        [Benchmark]
        public void QueryDocumentsSmallOnly()
        {
            var count = readTransaction.Stream<BigObject>("select * from dbo.BigObject where len([JSON]) < 1000").Count();
            if (count == 0)
                throw new Exception($"Didn't get any items! Got {count} instead?!");
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Nevermore.Benchmarks.Model;
using Nevermore.Benchmarks.SetUp;

namespace Nevermore.Benchmarks
{
    [Description("Document size")]
    public class DocumentSizeBenchmark : BenchmarkBase
    {
        IRelationalStore store;
        IReadTransaction transaction;

        public override void SetUp()
        {
            base.SetUp();

            var config = new RelationalStoreConfiguration(ConnectionString);
            config.Mappings.Register(new BigObjectMap());
            
            store = new RelationalStore(config);
            transaction = store.BeginReadTransaction();

            using (var writer = store.BeginWriteTransaction())
            {
                var text = new string('A', 256);
                var luckyNumbers = Enumerable.Range(1, DocumentSize / 256).Select(n => text);
                writer.Insert(new BigObject { Id = "BigObject-1", LuckyNames = luckyNumbers.ToArray()});
                writer.Commit();
            }
        }

        [ParamsSource(nameof(ValuesForBigObjectSize))]
        public int DocumentSize { get; set; }

        public IEnumerable<int> ValuesForBigObjectSize
        {
            get
            {
                var start = 256;

                while (start < 1024 * 1024)
                {
                    yield return start;
                    start = start * 2;
                } 
            }
        }
        
        [Benchmark(Description = "Load big object")]
        public BigObject LoadBigObject()
        {
            var item = transaction.Load<BigObject>("BigObject-1");

            if (item.Id != "BigObject-1" && item.LuckyNames.Length < 10)
                throw new Exception("Incorrect results");
            return item;
        }
    }
}
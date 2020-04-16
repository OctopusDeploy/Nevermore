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
    [Description("Document size")]
    public class DocumentSizeBenchmark : BenchmarkBase
    {
        IRelationalStore store;
        IReadTransaction transaction;

        public override void SetUp()
        {
            base.SetUp();
            var config = new RelationalStoreConfiguration(ConnectionString);
            config.Mappings.Register(new BigObjectMap(Format));

            store = new RelationalStore(config);
            transaction = store.BeginReadTransaction();

            using var writer = store.BeginWriteTransaction();
            var rand = new Random(42);
            var randomString = new Func<string>(() =>
            {
                var buffer = new byte[256];
                rand.NextBytes(buffer);
                return Convert.ToBase64String(buffer);
            });
            var historyEntries = Enumerable.Range(1, DocumentSize / 256).Select(n => new BigObjectHistoryEntry
                {Id = Guid.NewGuid(), Comment = randomString(), Date = DateTime.Today.AddDays(n)});
            writer.Insert(new BigObject {Id = "BigObject-1", History = historyEntries.OfType<object>().ToList()});
            writer.Commit();
        }

        [Params(JsonStorageFormat.TextOnly, JsonStorageFormat.CompressedOnly, JsonStorageFormat.MixedPreferCompressed)]
        public JsonStorageFormat Format { get; set; }

        [Params(256, 1024, 4096, 16384, 65536, 65536*8)]
        public int DocumentSize { get; set; }
        
        [Benchmark(Description = "Load big object")]
        public BigObject LoadBigObject()
        {
            var item = transaction.Load<BigObject>("BigObject-1");

            if (item.Id != "BigObject-1" || item.History.Count == 0)
                throw new Exception("Incorrect results");
            return item;
        }
    }
}
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
    public class DocumentSizeBenchmark : BenchmarkBase
    {
        IRelationalStore store;
        IReadTransaction readTransaction;

        public override void SetUp()
        {
            base.SetUp();
            var config = new RelationalStoreConfiguration(ConnectionString);
            config.Mappings.Register(new BigObjectMap(Format));

            store = new RelationalStore(config);
            readTransaction = store.BeginReadTransaction();

            using var writer = store.BeginWriteTransaction();
            var rand = new Random(42);
            var randomString = new Func<string>(() =>
            {
                var buffer = new byte[3];
                rand.NextBytes(buffer);
                return Convert.ToBase64String(buffer);
            });
            var historyEntries = Enumerable.Range(1, DocumentSize / 256).Select(n => new BigObjectHistoryEntry
                {Id = Guid.NewGuid(), Comment = randomString(), LuckyNumbers = Enumerable.Range(0, rand.Next(130, 330)).ToArray(), Date = DateTime.Today.AddDays(n)});
            writer.Insert(new BigObject {Id = "BigObject-1", History = historyEntries.OfType<object>().ToList()});
            writer.Commit();
        }

        [Params(JsonStorageFormat.TextOnly)]
        public JsonStorageFormat Format { get; set; }

        [Params(256, 1024, 4096, 16384 * 2, 65536*8)]
        public int DocumentSize { get; set; }
        
        [Benchmark]
        public BigObject Load()
        {
            var item = readTransaction.Load<BigObject>("BigObject-1");

            if (item.Id != "BigObject-1" || item.History.Count == 0)
                throw new Exception("Incorrect results");
            return item;
        }
        
        [Benchmark]
        public void LoadSave()
        {
            using var transaction = store.BeginTransaction();
            var item = transaction.Load<BigObject>("BigObject-1");
            item.Name = Guid.NewGuid().ToString();
            transaction.Update(item);
            transaction.Commit();
        }
    }
}
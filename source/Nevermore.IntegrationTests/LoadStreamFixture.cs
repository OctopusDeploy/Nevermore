using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    public class LoadStreamFixture : FixtureWithRelationalStore
    {
        [Test, Ignore("Performance")]
        public void LoadManyPerformanceTest()
        {
            using (var creator = Store.BeginWriteTransaction())
            {
                for (var i = 0; i < 10000; i++)
                {
                    creator.Insert(new Product { Name = "Product " + i, Price = i, Type = ProductType.Dodgy});
                }
                
                creator.Commit();
            }

            using (var reader = Store.BeginReadTransaction())
            {
                var all = reader.Query<Product>().ToList().Select(p => p.Id).OrderByDescending(p => Guid.NewGuid()).ToList();
            
                DoLoad(reader, all.Take(100).ToList());
                DoLoad(reader, all.Take(100).ToList());
                DoLoad(reader, all.Take(300).ToList());
                DoLoad(reader, all.Take(600).ToList());
                DoLoad(reader, all.Take(1200).ToList());
                DoLoad(reader, all.Take(1800).ToList());
                DoLoad(reader, all.Take(2400).ToList());
            }
        }

        static void DoLoad(IReadQueryExecutor transaction, List<string> ids)
        {
            var watch = Stopwatch.StartNew();
            var products = transaction.Load<Product>(ids);
            Assert.That(products.Count, Is.EqualTo(ids.Count));
            Console.WriteLine($"Loaded {products.Count} products in {watch.ElapsedMilliseconds}ms");
        }
    }
}
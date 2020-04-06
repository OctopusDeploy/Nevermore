using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Nevermore.IntegrationTests.Model;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    public class AsyncFixture : FixtureWithRelationalStore
    {
        [Test]
        public async Task InsertAndLoad()
        {
            using (var transaction = await Store.BeginWriteTransactionAsync())
            {
                await transaction.InsertAsync(new Product { Name = "First product", Price = 100.00M, Type = ProductType.Dodgy}, "Product-First");
                transaction.Commit();
            }

            using (var reader = await Store.BeginReadTransactionAsync())
            {
                var first = await reader.LoadAsync<Product>("Product-First");
                Assert.That(first.Name, Is.EqualTo("First product"));
                Assert.That(first.Price, Is.EqualTo(100.00M));
                Assert.That(first.Type, Is.EqualTo(ProductType.Dodgy));
            }
        }
    }
}
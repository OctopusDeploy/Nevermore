using System.Threading.Tasks;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    public class AsyncExamples : FixtureWithRelationalStore
    {
        [Test]
        public async Task InsertAndLoad()
        {
            using (var transaction = await Store.BeginWriteTransactionAsync())
            {
                await transaction.InsertAsync(
                    new Product { Name = "First product", Price = 100.00M, Type = ProductType.Dodgy}, 
                    new InsertOptions { CustomAssignedId = "Product-First"});
                transaction.Commit();
            }

            using (var reader = await Store.BeginReadTransactionAsync())
            {
                var first = reader.Load<Product>("Product-First");
                Assert.That(first.Name, Is.EqualTo("First product"));
                Assert.That(first.Price, Is.EqualTo(100.00M));
                Assert.That(first.Type, Is.EqualTo(ProductType.Dodgy));
            }
        }
        
        [Test]
        public async Task Query()
        {
            using (var transaction = await Store.BeginWriteTransactionAsync())
            {
                await transaction.InsertAsync(
                    new Product { Name = "First product", Price = 100.00M, Type = ProductType.Dodgy}, 
                    new InsertOptions { CustomAssignedId = "Product-First"});
                await transaction.InsertAsync(
                    new Product { Name = "Second product", Price = 200.00M, Type = ProductType.Dodgy}, 
                    new InsertOptions { CustomAssignedId = "Product-Second"});
                transaction.Commit();
            }

            using (var reader = await Store.BeginReadTransactionAsync())
            {
                var results = await reader.Query<Product>().ToListAsync();
                results.Count.Should().Be(2);
                
                var results2 = await reader.Query<Product>().Where(p => p.Name == "Second product").ToListAsync();
                results2.Count.Should().Be(1);
            }
        }
    }
}
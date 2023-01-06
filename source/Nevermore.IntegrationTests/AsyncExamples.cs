using System.Threading.Tasks;
using FluentAssertions;
using Nevermore.Diagnostics;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;
// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace Nevermore.IntegrationTests
{
    public class AsyncExamples : FixtureWithRelationalStore
    {
        public override void SetUp()
        {
            base.SetUp();
        }

        [Test]
        public async Task InsertAndLoad()
        {
            using (var transaction = await Store.BeginWriteTransactionAsync().ConfigureAwait(false))
            {
                await transaction.InsertAsync(
                    new Product { Name = "First product", Price = 100.00M, Type = ProductType.Dodgy}, 
                    new InsertOptions { CustomAssignedId = "Product-First"}).ConfigureAwait(false);
                transaction.Commit();
            }

            using (var reader = await Store.BeginReadTransactionAsync().ConfigureAwait(false))
            {
                var first = await reader.LoadAsync<Product>("Product-First").ConfigureAwait(false);
                Assert.That(first.Name, Is.EqualTo("First product"));
                Assert.That(first.Price, Is.EqualTo(100.00M));
                Assert.That(first.Type, Is.EqualTo(ProductType.Dodgy));
            }
        }
        
        [Test]
        public async Task Query()
        {
            using (var transaction = await Store.BeginWriteTransactionAsync().ConfigureAwait(false))
            {
                await transaction.InsertAsync(
                    new Product { Name = "First product", Price = 100.00M, Type = ProductType.Dodgy}, 
                    new InsertOptions { CustomAssignedId = "Product-First"}).ConfigureAwait(false);
                await transaction.InsertAsync(
                    new Product { Name = "Second product", Price = 200.00M, Type = ProductType.Dodgy}, 
                    new InsertOptions { CustomAssignedId = "Product-Second"}).ConfigureAwait(false);
                await transaction.CommitAsync().ConfigureAwait(false);
            }

            using (var reader = await Store.BeginReadTransactionAsync().ConfigureAwait(false))
            {
                var results = await reader.Query<Product>().ToListAsync().ConfigureAwait(false);
                results.Count.Should().Be(2);
                
                var results2 = await reader.Query<Product>().Where(p => p.Name == "Second product").ToListAsync().ConfigureAwait(false);
                results2.Count.Should().Be(1);
            }
        }

        [Test]
        public void SynchronousOperationsWillFail()
        {
            using var transaction = Store.BeginTransaction();
            
            // Set this to cause an exception if a synchronous operation is detected. This helps to find code paths 
            // that result in synchronous operations.
            Store.Configuration.AllowSynchronousOperations = false;
            
            Assert.Throws<SynchronousOperationsDisabledException>(() => transaction.Load<Product>("Product-First"));
        }
    }
}
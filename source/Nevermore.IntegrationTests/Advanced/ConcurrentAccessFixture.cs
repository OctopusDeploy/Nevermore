using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using Nito.AsyncEx;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class ConcurrentAccessFixture : FixtureWithRelationalStore
    {
        [Test]
        public void ConcurrentAccessDoesNotGoBoom()
        {
            NoMonkeyBusiness();

            var namePrefix = $"{Guid.NewGuid()}-";
            const int numberOfDocuments = 100;

            // Create a bunch of documents so that we can query for them.
            using (var transaction = Store.BeginTransaction())
            {
                Enumerable.Range(0, numberOfDocuments)
                    .Select(i => new DocumentWithIdentityId {Name = $"{namePrefix}{i}"})
                    .AsParallel()
                    .WithDegreeOfParallelism(512)
                    .Select(document =>
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        transaction.Insert(document);
                        return 0;
                    })
                    .ToArray();
                transaction.Commit();
            }

            // Now hit it really hard and see if we can provoke a failure.
            using (var transaction = Store.BeginTransaction())
            {
                Enumerable.Range(0, 64)
                    .AsParallel()
                    .WithDegreeOfParallelism(512)
                    .Select(i =>
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        var documents = transaction.Query<DocumentWithIdentityId>()
                            .Where(x => x.Name.StartsWith(namePrefix))
                            .ToArray();
                        documents.Should().HaveCount(numberOfDocuments);
                        return 0;
                    })
                    .ToArray();
            }
        }

        [Test]
        public async Task AsyncConcurrentAccessDoesNotGoBoom()
        {
            NoMonkeyBusiness();

            var namePrefix = $"{Guid.NewGuid()}-";
            const int numberOfDocuments = 100;

            // Create a bunch of documents so that we can query for them.
            using (var transaction = await Store.BeginWriteTransactionAsync())
            {
                await Enumerable.Range(0, numberOfDocuments)
                    .Select(i => new DocumentWithIdentityId {Name = $"{namePrefix}{i}"})
                    // ReSharper disable once AccessToDisposedClosure
                    .Select(document => transaction.InsertAsync(document))
                    .WhenAll();
                await transaction.CommitAsync();
            }

            // Now hit it really hard and see if we can provoke a failure.
            using (var transaction = await Store.BeginWriteTransactionAsync())
            {
                await Enumerable.Range(0, 64)
                    .Select(async i =>
                    {
                        // ReSharper disable once AccessToDisposedClosure
                        var documents = await transaction.Query<DocumentWithIdentityId>()
                            .Where(x => x.Name.StartsWith(namePrefix))
                            .ToListAsync();
                        documents.Should().HaveCount(numberOfDocuments);
                    })
                    .WhenAll();
            }
        }
    }
}
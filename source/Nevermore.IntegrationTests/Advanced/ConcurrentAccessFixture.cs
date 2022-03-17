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
                        // ReSharper disable AccessToDisposedClosure
                        // ReSharper disable ReturnValueOfPureMethodIsNotUsed

                        var documents = transaction.Query<DocumentWithIdentityId>()
                            .Where(x => x.Name.StartsWith(namePrefix))
                            .ToArray();
                        documents.Should().HaveCount(numberOfDocuments);

                        var id = documents.First().Id;
                        var ids = documents.Select(d => d.Id).ToArray();

                         transaction.Load<DocumentWithIdentityId>(id);
                         transaction.LoadRequired<DocumentWithIdentityId>(id);
                         transaction.LoadMany<DocumentWithIdentityId>(ids);
                         transaction.LoadManyRequired<DocumentWithIdentityId>(ids);
                         transaction.Query<DocumentWithIdentityId>().Any();
                         transaction.Query<DocumentWithIdentityId>().Count();
                         transaction.Query<DocumentWithIdentityId>().ToList();
                         transaction.Query<DocumentWithIdentityId>().ToArray();
                         transaction.Query<DocumentWithIdentityId>().FirstOrDefault();
                         transaction.Query<DocumentWithIdentityId>().ToDictionary(x => x.Id.ToString());

                        return 0;
                        // ReSharper restore ReturnValueOfPureMethodIsNotUsed
                        // ReSharper restore AccessToDisposedClosure
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
                        // ReSharper disable AccessToDisposedClosure
                        // ReSharper disable ReturnValueOfPureMethodIsNotUsed
                        var documents = await transaction.Query<DocumentWithIdentityId>()
                            .Where(x => x.Name.StartsWith(namePrefix))
                            .ToListAsync();
                        documents.Should().HaveCount(numberOfDocuments);

                        var id = documents.First().Id;
                        var ids = documents.Select(d => d.Id).ToArray();

                        await transaction.LoadAsync<DocumentWithIdentityId>(id);
                        await transaction.LoadRequiredAsync<DocumentWithIdentityId>(id);
                        await transaction.LoadManyAsync<DocumentWithIdentityId>(ids);
                        await transaction.LoadManyRequiredAsync<DocumentWithIdentityId>(ids);
                        await transaction.Query<DocumentWithIdentityId>().AnyAsync();
                        await transaction.Query<DocumentWithIdentityId>().CountAsync();
                        await transaction.Query<DocumentWithIdentityId>().ToListAsync();
                        await transaction.Query<DocumentWithIdentityId>().FirstOrDefaultAsync();
                        await transaction.Query<DocumentWithIdentityId>().ToListWithCountAsync(0,numberOfDocuments);
                        await transaction.Query<DocumentWithIdentityId>().ToDictionaryAsync(x => x.Id.ToString());

                        // ReSharper restore ReturnValueOfPureMethodIsNotUsed
                        // ReSharper restore AccessToDisposedClosure
                    })
                    .WhenAll();
            }
        }
    }
}
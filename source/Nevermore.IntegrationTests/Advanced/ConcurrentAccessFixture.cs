using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Nevermore.Advanced.Queryable;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using Nito.AsyncEx;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class ConcurrentAccessFixture : FixtureWithRelationalStore
    {
        const int NumberOfDocuments = 256;
        const int DegreeOfParallelism = NumberOfDocuments;

        [Test]
        public void ConcurrentAccessDoesNotGoBoom()
        {
            NoMonkeyBusiness();

            var namePrefix = $"{Guid.NewGuid()}-";

            // Create a bunch of documents so that we can query for them.
            using (var transaction = Store.BeginTransaction())
            {
                Enumerable.Range(0, NumberOfDocuments)
                    .Select(i => new DocumentWithIdentityId {Name = $"{namePrefix}{i}"})
                    .AsParallel()
                    .WithDegreeOfParallelism(DegreeOfParallelism)
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
                ThreadWaitAll(
                    Enumerable.Range(0, DegreeOfParallelism)
                        .Select(i =>
                        {
                            // ReSharper disable AccessToDisposedClosure
                            // ReSharper disable ReturnValueOfPureMethodIsNotUsed

                            return ThreadDotRun(() =>
                            {
                                var documents = transaction.Query<DocumentWithIdentityId>()
                                    .Where(x => x.Name.StartsWith(namePrefix))
                                    .ToArray();
                                documents.Should().HaveCount(NumberOfDocuments);

                                var firstDocument = documents.First();
                                var id = firstDocument.Id;
                                var ids = documents.Select(d => d.Id).ToArray();

                                ThreadWaitAll(
                                    ThreadDotRun(() => transaction.Load<DocumentWithIdentityId>(id)),
                                    ThreadDotRun(() => transaction.Load<DocumentWithIdentityId>(id)),
                                    ThreadDotRun(() => transaction.LoadRequired<DocumentWithIdentityId>(id)),
                                    ThreadDotRun(() => transaction.LoadMany<DocumentWithIdentityId>(ids)),
                                    ThreadDotRun(() => transaction.LoadManyRequired<DocumentWithIdentityId>(ids)),
                                    ThreadDotRun(() => transaction.Query<DocumentWithIdentityId>().Any()),
                                    ThreadDotRun(() => transaction.Query<DocumentWithIdentityId>().Count()),
                                    ThreadDotRun(() => transaction.Query<DocumentWithIdentityId>().ToList()),
                                    ThreadDotRun(() => transaction.Query<DocumentWithIdentityId>().ToArray()),
                                    ThreadDotRun(() => transaction.Query<DocumentWithIdentityId>().FirstOrDefault()),
                                    ThreadDotRun(() => transaction.Query<DocumentWithIdentityId>().ToDictionary(x => x.Id.ToString())),
                                    ThreadDotRun(() => transaction.Queryable<DocumentWithIdentityId>().Any()),
                                    ThreadDotRun(() => transaction.Queryable<DocumentWithIdentityId>().Count()),
                                    ThreadDotRun(() => transaction.Queryable<DocumentWithIdentityId>().ToList()),
                                    ThreadDotRun(() => transaction.Queryable<DocumentWithIdentityId>().ToArray()),
                                    ThreadDotRun(() => transaction.Queryable<DocumentWithIdentityId>().FirstOrDefault()),
                                    ThreadDotRun(() => transaction.Queryable<DocumentWithIdentityId>().ToDictionary(x => x.Id.ToString())),
                                    ThreadDotRun(() => transaction.Update(firstDocument))
                                );
                            });

                            // ReSharper restore ReturnValueOfPureMethodIsNotUsed
                            // ReSharper restore AccessToDisposedClosure
                        })
                        .ToArray()
                );

                static Thread ThreadDotRun(Action action)
                {
                    var thread = new Thread(new ThreadStart(action));
                    thread.Start();
                    return thread;
                }

                static void ThreadWaitAll(params Thread[] threads)
                {
                    foreach (var thread in threads) thread.Join();
                }
            }
        }

        [Test]
        public async Task AsyncConcurrentAccessDoesNotGoBoom()
        {
            NoMonkeyBusiness();

            var namePrefix = $"{Guid.NewGuid()}-";

            // Create a bunch of documents so that we can query for them.
            using (var transaction = await Store.BeginWriteTransactionAsync())
            {
                await Enumerable.Range(0, NumberOfDocuments)
                    .Select(i => new DocumentWithIdentityId {Name = $"{namePrefix}{i}"})
                    // ReSharper disable once AccessToDisposedClosure
                    .Select(document => transaction.InsertAsync(document))
                    .WhenAll();
                await transaction.CommitAsync();
            }

            // Now hit it really hard and see if we can provoke a failure.
            using (var transaction = await Store.BeginWriteTransactionAsync())
            {
                await Enumerable.Range(0, DegreeOfParallelism)
                    .Select(async i =>
                    {
                        // ReSharper disable AccessToDisposedClosure
                        // ReSharper disable ReturnValueOfPureMethodIsNotUsed
                        var documents = await transaction.Query<DocumentWithIdentityId>()
                            .Where(x => x.Name.StartsWith(namePrefix))
                            .ToListAsync();
                        documents.Should().HaveCount(NumberOfDocuments);

                        var firstDocument = documents.First();
                        var id = firstDocument.Id;
                        var ids = documents.Select(d => d.Id).ToArray();

                        await Task.WhenAll(
                            transaction.LoadAsync<DocumentWithIdentityId>(id),
                            transaction.LoadRequiredAsync<DocumentWithIdentityId>(id),
                            transaction.LoadManyAsync<DocumentWithIdentityId>(ids),
                            transaction.LoadManyRequiredAsync<DocumentWithIdentityId>(ids),
                            transaction.Query<DocumentWithIdentityId>().AnyAsync(),
                            transaction.Query<DocumentWithIdentityId>().CountAsync(),
                            transaction.Query<DocumentWithIdentityId>().ToListAsync(),
                            transaction.Query<DocumentWithIdentityId>().FirstOrDefaultAsync(),
                            transaction.Query<DocumentWithIdentityId>().ToListWithCountAsync(0, NumberOfDocuments),
                            transaction.Query<DocumentWithIdentityId>().ToDictionaryAsync(x => x.Id.ToString()),
                            transaction.Queryable<DocumentWithIdentityId>().AnyAsync(),
                            transaction.Queryable<DocumentWithIdentityId>().CountAsync(),
                            transaction.Queryable<DocumentWithIdentityId>().ToListAsync(),
                            transaction.Queryable<DocumentWithIdentityId>().FirstOrDefaultAsync(),
                            transaction.UpdateAsync(firstDocument)
                        );

                        // ReSharper restore ReturnValueOfPureMethodIsNotUsed
                        // ReSharper restore AccessToDisposedClosure
                    })
                    .WhenAll();
            }
        }
    }
}
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    public class KeyAllocatorFixture : FixtureWithRelationalStore
    {
        [Test]
        public void ShouldAllocateKeysInChunks()
        {
            var allocatorA = new KeyAllocator(Store, 10);
            var allocatorB = new KeyAllocator(Store, 10);

            // A gets 1-10
            AssertNext(allocatorA, "Todos", 1);
            AssertNext(allocatorA, "Todos", 2);
            AssertNext(allocatorA, "Todos", 3);
            AssertNext(allocatorA, "Todos", 4);
            AssertNext(allocatorA, "Todos", 5);

            // B gets 11->20
            AssertNext(allocatorB, "Todos", 11);
            AssertNext(allocatorB, "Todos", 12);
            AssertNext(allocatorB, "Todos", 13);

            // A will keep allocating
            AssertNext(allocatorA, "Todos", 6);
            AssertNext(allocatorA, "Todos", 7);
            AssertNext(allocatorA, "Todos", 8);
            AssertNext(allocatorA, "Todos", 9);
            AssertNext(allocatorA, "Todos", 10);

            // ... until it needs a new block
            AssertNext(allocatorA, "Todos", 21);
            AssertNext(allocatorA, "Todos", 22);
            AssertNext(allocatorA, "Todos", 23);

            AssertNext(allocatorB, "Todos", 14);
            AssertNext(allocatorB, "Todos", 15);
            AssertNext(allocatorB, "Todos", 16);
            AssertNext(allocatorB, "Todos", 17);
            AssertNext(allocatorB, "Todos", 18);
            AssertNext(allocatorB, "Todos", 19);
            AssertNext(allocatorB, "Todos", 20);

            // Now B needs a new block
            AssertNext(allocatorB, "Todos", 31);
        }

        [Test]
        public void ShouldAllocateForDifferentCollections()
        {
            var allocator = new KeyAllocator(Store, 10);
            AssertNext(allocator, "Todos", 1);
            AssertNext(allocator, "Todos", 2);
            AssertNext(allocator, "Todos", 3);

            AssertNext(allocator, "TodoItems", 1);
            AssertNext(allocator, "TodoItems", 2);
            AssertNext(allocator, "TodoItems", 3);

            AssertNext(allocator, "Todos", 4);
            AssertNext(allocator, "Todos", 5);
        }

        [Test]
        public void ShouldAllocateInParallel()
        {
            const int allocationCount = 20;
            const int threadCount = 10;

            var customerIds = new ConcurrentBag<CustomerId>();
            var deploymentIds = new ConcurrentBag<string>();
            var random = new Random(1);

            var tasks = Enumerable.Range(0, threadCount)
                .Select(_ => Task.Factory.StartNew(()=>
                {
                for (var i = 0; i < allocationCount; i++)
                {
                    using (var transaction = Store.BeginTransaction())
                    {
                        var sequence = random.Next(3);
                        if (sequence == 0)
                        {
                            var id = transaction.AllocateId<Customer, CustomerId>();
                            customerIds.Add(id);
                            transaction.Commit();
                        }
                        else if (sequence == 1)
                        {
                            // Abandon some transactions (just projects to make it easier)
                            var id = transaction.AllocateId<Customer, CustomerId>();
                            // Abandoned Ids are not returned to the pool
                            customerIds.Add(id);
                            transaction.Dispose();
                        }
                        else if (sequence == 2)
                        {
                            var id = transaction.AllocateId<Order, string>();
                            deploymentIds.Add(id);
                            transaction.Commit();
                        }
                    }
                }
            })).ToArray();

            Task.WaitAll(tasks);
            Func<string, int> removePrefix = x => int.Parse(x.Split('-')[1]);

            var customerIdsAfter = customerIds.Select(x => removePrefix(x.Value)).OrderBy(x => x).ToArray();
            var deploymentIdsAfter = deploymentIds.Select(removePrefix).OrderBy(x => x).ToArray();

            customerIdsAfter.Distinct().Count().Should().Be(customerIdsAfter.Length);
            deploymentIdsAfter.Distinct().Count().Should().Be(deploymentIdsAfter.Length);

            // Check that there are no gaps in sequence

            var firstProjectId = customerIdsAfter.First();
            var lastProjectId = customerIdsAfter.Last();

            var expectedProjectIds = Enumerable.Range(firstProjectId, lastProjectId - firstProjectId + 1)
                .ToList();

            customerIdsAfter.Should().BeEquivalentTo(expectedProjectIds);
        }

        static void AssertNext(KeyAllocator allocator, string collection, int expected)
        {
            allocator.NextId(collection).Should().Be(expected);
        }
    }
}
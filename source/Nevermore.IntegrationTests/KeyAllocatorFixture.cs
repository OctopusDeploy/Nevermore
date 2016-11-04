using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Nevermore.IntegrationTests.Model;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    public class KeyAllocatorFixture : FixtureWithRelationalStore
    {
        public override void SetUp()
        {
            base.SetUp();
            Mappings.Install(
                new DocumentMap[]
                {
                    new CustomerMap(),
                    new OrderMap()
                }
            );
        }

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
            const int threadCount = 100;

            var projectIds = new ConcurrentBag<string>();
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
                            var id = transaction.AllocateId(typeof (Customer));
                            projectIds.Add(id);
                            transaction.Commit();
                        }
                        else if (sequence == 1)
                        {
                            // Abandon some transactions (just projects to make it easier)
                            var id = transaction.AllocateId(typeof(Customer));
                            // Abandoned Ids are not returned to the pool
                            projectIds.Add(id);
                            transaction.Dispose();
                        }
                        else if (sequence == 2)
                        {
                            var id = transaction.AllocateId(typeof(Order));
                            deploymentIds.Add(id);
                            transaction.Commit();
                        }
                    }
                }
            })).ToArray();

            Task.WaitAll(tasks);
            Func<string, int> removePrefix = x => int.Parse(x.Split('-')[1]);

            var projectIdsAfter = projectIds.Select(removePrefix).OrderBy(x => x).ToArray();
            var deploymentIdsAfter = deploymentIds.Select(removePrefix).OrderBy(x => x).ToArray();

            Assert.That(projectIdsAfter, Is.Unique, "Duplicate project IDs generated");
            Assert.That(deploymentIdsAfter, Is.Unique, "Duplicate environment IDs generated");

            // Check that there are no gaps in sequence

            var firstProjectId = projectIdsAfter.First();
            var lastProjectId = projectIdsAfter.Last();

            var expectedProjectIds = Enumerable.Range(firstProjectId, lastProjectId - firstProjectId + 1)
                .ToList();

            Assert.That(projectIdsAfter, Is.EqualTo(expectedProjectIds), "Ids should be in order with no gaps despite failed transactions");
        }
  
        static void AssertNext(KeyAllocator allocator, string collection, int expected)
        {
            Assert.That(allocator.NextId(collection), Is.EqualTo(expected));
        }


    }
}
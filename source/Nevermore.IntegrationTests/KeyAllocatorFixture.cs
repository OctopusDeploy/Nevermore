using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using Xunit;

namespace Nevermore.IntegrationTests
{
    public class KeyAllocatorFixture : FixtureWithRelationalStore
    {
        [Fact]
        public void ShouldAllocateKeysInChunks()
        {
            var allocatorA = new KeyAllocator(Store, 10);
            var allocatorB = new KeyAllocator(Store, 10);

            // A gets 0-9 (but starts with 1)
            AssertNext(allocatorA, "Todos", 1);
            AssertNext(allocatorA, "Todos", 2);
            AssertNext(allocatorA, "Todos", 3);
            AssertNext(allocatorA, "Todos", 4);
            AssertNext(allocatorA, "Todos", 5);

            // B gets 10->19
            AssertNext(allocatorB, "Todos", 10);
            AssertNext(allocatorB, "Todos", 11);
            AssertNext(allocatorB, "Todos", 12);
            AssertNext(allocatorB, "Todos", 13);

            // A will keep allocating
            AssertNext(allocatorA, "Todos", 6);
            AssertNext(allocatorA, "Todos", 7);
            AssertNext(allocatorA, "Todos", 8);
            AssertNext(allocatorA, "Todos", 9);

            // ... until it needs a new block
            AssertNext(allocatorA, "Todos", 20);
            AssertNext(allocatorA, "Todos", 21);
            AssertNext(allocatorA, "Todos", 22);

            AssertNext(allocatorB, "Todos", 14);
            AssertNext(allocatorB, "Todos", 15);
            AssertNext(allocatorB, "Todos", 16);
            AssertNext(allocatorB, "Todos", 17);
            AssertNext(allocatorB, "Todos", 18);
            AssertNext(allocatorB, "Todos", 19);

            // Now B needs a new block
            AssertNext(allocatorB, "Todos", 30);
        }

        [Fact]
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

        [Fact]
        public void ShouldAllocateInParallel()
        {
            var projectIds = new List<int>();
            var environmentIds = new List<int>();

            var threads = Enumerable.Range(0, 4).Select(_ => new Thread(new ThreadStart(delegate
            {
                var allocator = new KeyAllocator(Store, 10);
                for (var i = 0; i < 1000; i++)
                {
                    projectIds.Add(allocator.NextId("Todos"));
                }
            }))).Concat(Enumerable.Range(0, 4).Select(_ => new Thread(new ThreadStart(delegate
            {
                var allocator = new KeyAllocator(Store, 10);
                for (var i = 0; i < 1000; i++)
                {
                    environmentIds.Add(allocator.NextId("TodoItems"));
                }
            })))).ToList();

            foreach (var thread in threads)
                thread.Start();

            foreach (var thread in threads)
                thread.Join();

            Assert.Equal(projectIds.Count, 4000);
            Assert.Equal(environmentIds.Count, 4000);
            Assert.Equal(projectIds.GroupBy(g => g).Count(g => g.Count() > 1), 0); // "Duplicate project IDs generated"
            Assert.Equal(environmentIds.GroupBy(g => g).Count(g => g.Count() > 1), 0); // "Duplicate environment IDs generated"
        }

        static void AssertNext(KeyAllocator allocator, string collection, int expected)
        {
            Assert.Equal(allocator.NextId(collection), expected);
        }
    }
}
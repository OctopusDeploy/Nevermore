using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Nevermore.Advanced;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    public class ToListWithCountAsyncFixture: FixtureWithRelationalStore
    {
        
        [Test]
        [TestCase(false)]
        [TestCase(true)] //Temporary test to cover and compare legacy mechanism for loading data. Remove once legacy approach deprecated
        public async Task QueryReturnsCorrectDataPageAndCount(bool useCteOperation)
        {
            using (var t = Store.BeginTransaction())
            {
                foreach (var c in new[]
                         {
                             new Customer {FirstName = "Alice", LastName = "Apple", Nickname = null},
                             new Customer {FirstName = "Bob", LastName = "Banana", Nickname = ""},
                             new Customer {FirstName = "Charlie", LastName = "Cherry", Nickname = "Chazza"},
                             new Customer {FirstName = "David", LastName = "Derkins", Nickname = "Dazza"},
                             new Customer {FirstName = "Eric", LastName = "Evans", Nickname = "Bob"}
                         })
                    t.Insert(c);
                await t.CommitAsync(CancellationToken.None).ConfigureAwait(false);

                FeatureFlags.UseCteBasedListWithCount = useCteOperation;
                var (items, count) = await t.Query<Customer>()
                    .OrderByDescending(o => o.LastName)
                    .Where(n => n.Nickname != null)
                    .ToListWithCountAsync(1, 2, CancellationToken.None).ConfigureAwait(false);

                CollectionAssert.AreEqual(items.Select(p => p.FirstName), new []{"David", "Charlie"});
                count.Should().Be(4);
            }
        }
        
        
        [Test]
        [TestCase(1, 0, Description = "Zero sized page")]
        [TestCase(100, 1, Description = "Skip beyond available")]
        public async Task WhenPageReturnsNoDataThenNonZeroCountStillReturns(int skip, int take)
        {
            using (var t = Store.BeginTransaction())
            {
                foreach (var c in new[]
                         {
                             new Customer {FirstName = "Alice", LastName = "Apple", Nickname = null},
                             new Customer {FirstName = "Bob", LastName = "Banana", Nickname = "Bazza"},
                             new Customer {FirstName = "Charlie", LastName = "Cherry", Nickname = "Chazza"},
                         })
                    t.Insert(c);
                await t.CommitAsync(CancellationToken.None).ConfigureAwait(false);

                FeatureFlags.UseCteBasedListWithCount = true;
                var (items, count) = await t.Query<Customer>()
                    .Where(c => c.Nickname != null)
                    .ToListWithCountAsync(skip, take, CancellationToken.None).ConfigureAwait(false);

                items.Should().BeEmpty();
                count.Should().Be(2);
            }
        }
    }
}
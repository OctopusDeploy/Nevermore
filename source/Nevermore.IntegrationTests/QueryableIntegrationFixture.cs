using System.Linq;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    public class QueryableIntegrationFixture : FixtureWithRelationalStore
    {
        [Test]
        public void Where()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customers = t.Queryable<Customer>()
                .Where(c => c.FirstName == "Alice")
                .ToList();

            customers.Select(c => c.LastName).Should().BeEquivalentTo("Apple");
        }

        [Test]
        public void FirstWithPredicate()
        {
            using var t = Store.BeginTransaction();

            var testCustomers = new[]
            {
                new Customer { FirstName = "Alice", LastName = "Apple" },
                new Customer { FirstName = "Bob", LastName = "Banana" },
                new Customer { FirstName = "Charlie", LastName = "Cherry" }
            };

            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var customer = t.Queryable<Customer>()
                .FirstOrDefault(c => c.FirstName == "Alice");

            customer.LastName.Should().BeEquivalentTo("Apple");
        }
    }
}
using System.Linq;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    public class QueryBuilderIntegrationFixture : FixtureWithRelationalStore
    {
        [Test]
        public void WhereInClause()
        {
            using (var trn = Store.BeginTransaction())
            {
                trn.TableQuery<Product>()
                    .Where("[Id] IN @ids")
                    .Parameter("ids", new[] {"A", "B"})
                    .ToList();
            }
        }

        [Test]
        public void WhereInClauseWhenParameterNamesDifferByCase()
        {
            using (var trn = Store.BeginTransaction())
            {
                trn.TableQuery<Product>()
                    .Where("[Id] IN @Ids")
                    .Parameter("ids", new[] {"A", "B"})
                    .ToList();
            }
        }

        [Test]
        public void WhereNullClause()
        {
            using (var t = Store.BeginTransaction())
            {
                foreach (var c in new[]
                {
                    new Customer {FirstName = "Alice", LastName = "Apple", Nickname = null},
                    new Customer {FirstName = "Bob", LastName = "Banana", Nickname = new Nickname("")},
                    new Customer {FirstName = "Charlie", LastName = "Cherry", Nickname = new Nickname("Chazza")}
                })
                    t.Insert(c);
                t.Commit();

                var customersNull = t.TableQuery<Customer>()
                    .Where(c => c.Nickname == null)
                    .ToList();

                var customersNotNull = t.TableQuery<Customer>()
                    .Where(c => c.Nickname != null)
                    .ToList();

                customersNull.Select(c => c.FirstName).Should().BeEquivalentTo("Alice");
                customersNotNull.Select(c => c.FirstName).Should().BeEquivalentTo("Bob", "Charlie");
            }
        }

        [Test]
        public void WhereEqualsTypedStringClause()
        {
            using (var t = Store.BeginTransaction())
            {
                foreach (var c in new[]
                {
                    new Customer {FirstName = "Alice", LastName = "Apple", Nickname = null},
                    new Customer {FirstName = "Bob", LastName = "Banana", Nickname = new Nickname("")},
                    new Customer {FirstName = "Charlie", LastName = "Cherry", Nickname = new Nickname("Chazza")}
                })
                    t.Insert(c);
                t.Commit();

                var customers = t.TableQuery<Customer>()
                    .Where(c => c.Nickname == new Nickname("Chazza"))
                    .ToList();

                customers.Select(c => c.FirstName).Should().BeEquivalentTo("Charlie");
            }
        }
    }
}
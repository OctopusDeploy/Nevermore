using System.Linq;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
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
                trn.Query<Product>()
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
                #pragma warning disable NV0006
                trn.Query<Product>()
                    .Where("[Id] IN @Ids")
                    .Parameter("ids", new[] {"A", "B"})
                    .ToList();
                #pragma warning restore NV0006
            }
        }

        [Test]
        public void WhereNullClause()
        {
            using (var t = Store.BeginTransaction())
            {
                foreach (var c in new []
                {
                    new Customer {FirstName = "Alice", LastName = "Apple", Nickname = null},
                    new Customer {FirstName = "Bob", LastName = "Banana", Nickname = ""},
                    new Customer {FirstName = "Charlie", LastName = "Cherry", Nickname = "Chazza"}
                })
                    t.Insert(c);
                t.Commit();
                
                var customersNull = t.Query<Customer>()
                    .Where(c => c.Nickname == null)
                    .ToList();
                
                var customersNotNull = t.Query<Customer>()
                    .Where(c => c.Nickname != null)
                    .ToList();

                customersNull.Select(c => c.FirstName).Should().BeEquivalentTo("Alice");
                customersNotNull.Select(c => c.FirstName).Should().BeEquivalentTo("Bob", "Charlie");
            }
        }
    }
}
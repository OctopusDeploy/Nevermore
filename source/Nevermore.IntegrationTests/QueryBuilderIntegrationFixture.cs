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
        public void WhereSubQueryClause()
        {
            using (var t = Store.BeginTransaction())
            {
                var product1 = new Product() {Name = "Item1", Type = ProductType.Normal};
                t.Insert(product1);
                var product2 = new Product() {Name = "Item2", Type = ProductType.Dodgy};
                t.Insert(product2);
                foreach (var lineItems in new []
                         {
                             new LineItem(){ProductId = product1.Id, Name = "NormalLine1"},
                             new LineItem(){ProductId = product1.Id, Name = "NormalLine2"},
                             new LineItem(){ProductId = product2.Id, Name = "DodgyLine1"}
                         })
                    t.Insert(lineItems);
                t.Commit();
                
                var productSubQuery = t.Query<Product>()
                    .Column(nameof(Product.Id))
                    .Where(nameof(Product.Type), UnarySqlOperand.Like, ProductType.Normal.ToString());
                
                var normalLineItems = t.Query<LineItem>().AllColumns()
                    .WhereIn(nameof(LineItem.ProductId), productSubQuery)
                    .ToList().Select(p => p.Name);

                var dodgyLineItems = t.Query<LineItem>().AllColumns()
                    .WhereNotIn(nameof(LineItem.ProductId), productSubQuery)
                    .ToList().Select(p => p.Name);

                normalLineItems.Should().BeEquivalentTo("NormalLine1", "NormalLine2");
                dodgyLineItems.Should().BeEquivalentTo("DodgyLine1");
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
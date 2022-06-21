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
        
        
        [Test]
        public void CrossJoin()
        {
            using (var t = Store.BeginTransaction())
            {
                t.InsertMany(new [] {
                    new Product { Name = "Shoe Horn" },
                    new Product { Name = "Widget" }}
                );
                t.InsertMany(new []
                {
                    new Customer {FirstName = "Alice", LastName = "Apple", Nickname = null},
                    new Customer {FirstName = "Bob", LastName = "Barker", Nickname = "Bazza"},
                    new Customer {FirstName = "Charlie", LastName = "Cherry", Nickname = "Chazza"}
                });
                t.Commit();
                
                var customersNull = t.Query<Customer>()
                    .Where(n => n.Nickname != null)
                    .Subquery().Alias("Customer");
                
                var customersNotNull = t.Query<Product>()
                    .Alias("Product")
                    .CrossJoin(customersNull)
                    .AsType<CustomerProductCross>()
                    .Column(nameof(Customer.Nickname), "CustomerName", "Customer")
                    .Column(nameof(Product.Name), "ProductName", "Product")
                    .ToList();

                customersNotNull.Select(cpc => $"{cpc.CustomerName}-{cpc.ProductName}")
                    .Should().BeEquivalentTo(new[] {"Bazza-Shoe Horn", "Bazza-Widget", "Chazza-Shoe Horn", "Chazza-Widget"});
            }
        }

        class CustomerProductCross
        {
            public string CustomerName { get; set; }
            public string ProductName { get; set; }
        }
    }
}
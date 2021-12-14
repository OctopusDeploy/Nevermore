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
                var testCustomers = new []
                {
                    new Customer {FirstName = "Alice", LastName = "Apple", Nickname = null},
                    new Customer {FirstName = "Bob", LastName = "Banana", Nickname = ""},
                    new Customer {FirstName = "Charlie", LastName = "Cherry", Nickname = "Chazza"}
                };
                foreach (var c in testCustomers)
                {
                    t.Insert(c);
                }

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
        public void WhereJsonValueUnaryClause()
        {
            using var t = Store.BeginTransaction();
            var testProducts = new []
            {
                new Product {Name = "Product 1", Price = 100},
                new Product {Name = "Product 2", Price = 200},
                new Product {Name = "Product 3", Price = 300},
            };
            foreach (var c in testProducts)
            {
                t.Insert(c);
            }

            t.Commit();

            var products = t.Query<Product>()
                .Where(c => c.Price == 100)
                .ToList();

            products.Select(c => c.Name).Should().BeEquivalentTo("Product 1");
        }

        [Test]
        public void WhereJsonValueBinaryClause()
        {
            using var t = Store.BeginTransaction();
            var testProducts = new []
            {
                new Product {Name = "Product 1", Price = 100},
                new Product {Name = "Product 2", Price = 200},
                new Product {Name = "Product 3", Price = 300},
            };
            foreach (var c in testProducts)
            {
                t.Insert(c);
            }

            t.Commit();

            var products = t.Query<Product>()
                .Where("Price", BinarySqlOperand.Between, 90.0, 110.0)
                .ToList();

            products.Select(c => c.Name).Should().BeEquivalentTo("Product 1");
        }

        [Test]
        public void WhereJsonValueArrayClause()
        {
            using var t = Store.BeginTransaction();
            var testProducts = new[]
            {
                new Product { Name = "Product 1", Price = 100 },
                new Product { Name = "Product 2", Price = 200 },
                new Product { Name = "Product 3", Price = 300 }
            };
            foreach (var c in testProducts)
            {
                t.Insert(c);
            }

            t.Commit();

            var products = t.Query<Product>()
                .Where("Price", ArraySqlOperand.In, new[] { 100m, 110m, 120m })
                .ToList();

            products.Select(c => c.Name).Should().BeEquivalentTo("Product 1");
        }

        [Test]
        public void WhereJsonValueIsNullClause()
        {
            using var t = Store.BeginTransaction();
            var testCustomers = new []
            {
                new Customer { FirstName = "Alice", LastName = "Apply", ApiKey = "API-77182873" },
                new Customer { FirstName = "Bob", LastName = "Banana", ApiKey = null },
                new Customer { FirstName = "Charlie", LastName = "Cherry", ApiKey = "API-9876123" }
            };
            foreach (var c in testCustomers)
            {
                t.Insert(c);
            }

            t.Commit();

            var products = t.Query<Customer>()
                .WhereNull("ApiKey")
                .ToList();

            products.Select(c => c.FirstName).Should().BeEquivalentTo("Bob");
        }
    }
}
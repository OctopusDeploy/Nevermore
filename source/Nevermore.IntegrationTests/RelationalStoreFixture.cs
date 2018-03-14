using System;
using System.Linq;
using Nevermore.IntegrationTests.Model;
using Xunit;
using Xunit.Abstractions;

namespace Nevermore.IntegrationTests
{
    public class RelationalStoreFixture : FixtureWithRelationalStore
    {
        public RelationalStoreFixture(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void ShouldGenerateIdsUnlessExplicitlyAssigned()
        {
            // The K and Id columns allow you to give records an ID, or use an auto-generated, unique ID
            using (var transaction = Store.BeginTransaction())
            {
                var customer1 = new Customer { Id = "Customers-Alice", FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] { 12, 13 }, Nickname = "Ally", Roles = { "web-server", "app-server" } };
                var customer2 = new Customer { FirstName = "Bob", LastName = "Banana", LuckyNumbers = new[] { 12, 13 }, Nickname = "B-man", Roles = { "web-server", "app-server" } };
                var customer3 = new Customer { FirstName = "Charlie", LastName = "Cherry", LuckyNumbers = new[] { 12, 13 }, Nickname = "Chazza", Roles = { "web-server", "app-server" } };
                transaction.Insert(customer1);
                transaction.Insert(customer2);
                transaction.Insert(customer3, "Customers-Chazza");

                Assert.Equal("Customers-Alice", customer1.Id);
                Assert.StartsWith("Customers-", customer2.Id);
                Assert.Equal("Customers-Chazza", customer3.Id);

                transaction.Commit();
            }
        }

        [Fact]
        public void ShouldPersistReferenceCollectionsToAllowLikeSearches()
        {
            using(var transaction = Store.BeginTransaction())
            {
                var customer1 = new Customer { FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] { 12, 13 }, Nickname = "Ally", Roles = { "web-server", "app-server" } };
                var customer2 = new Customer { FirstName = "Bob", LastName = "Banana", LuckyNumbers = new[] { 12, 13 }, Nickname = "B-man", Roles = { "db-server", "app-server" } };
                var customer3 = new Customer { FirstName = "Charlie", LastName = "Cherry", LuckyNumbers = new[] { 12, 13 }, Nickname = "Chazza", Roles = { "web-server", "app-server" } };
                transaction.Insert(customer1);
                transaction.Insert(customer2);
                transaction.Insert(customer3);
                transaction.Commit();
            }

            // ReferenceCollection columns that are indexed are always stored in pipe-separated format with pipes at the front and end: |foo|bar|baz|
            using (var transaction = Store.BeginTransaction())
            {
                var customers = transaction.Query<Customer>()
                    .Where("[Roles] LIKE @role")
                    .LikeParameter("role", "web-server")
                    .ToList();
                Assert.Equal(2, customers.Count);
            }
        }

        [Fact]
        public void ShouldPersistCollectionsToAllowInSearches()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var customer1 = new Customer { FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] { 12, 13 }, Nickname = "Ally", Roles = { "web-server", "app-server" } };
                var customer2 = new Customer { FirstName = "Bob", LastName = "Banana", LuckyNumbers = new[] { 12, 13 }, Nickname = "B-man", Roles = { "db-server", "app-server" } };
                var customer3 = new Customer { FirstName = "Charlie", LastName = "Cherry", LuckyNumbers = new[] { 12, 13 }, Nickname = "Chazza", Roles = { "web-server", "app-server" } };
                transaction.Insert(customer1);
                transaction.Insert(customer2);
                transaction.Insert(customer3);
                transaction.Commit();
            }

            // ReferenceCollection columns that are indexed are always stored in pipe-separated format with pipes at the front and end: |foo|bar|baz|
            using (var transaction = Store.BeginTransaction())
            {
                var customers = transaction.Query<Customer>()
                    .Where("LastName", ArraySqlOperand.In, new[] { "Apple", "Banana" })
                    .ToList();
                Assert.Equal(2, customers.Count);
            }
        }

        [Fact]
        public void ShouldHandleIdsWithInOperand()
        {
            string customerId;
            using (var transaction = Store.BeginTransaction())
            {
                var customer = new Customer { FirstName = "Alice", LastName = "Apple" };
                transaction.Insert(customer);
                transaction.Commit();
                customerId = customer.Id;
            }

            using (var transaction = Store.BeginTransaction())
            {
                var customer = transaction.Query<Customer>()
                                            .Where("Id", ArraySqlOperand.In, new[] { customerId })
                                            .Stream()
                                            .Single();
                Assert.Equal("Alice", customer.FirstName);
            }
        }

        [Fact]
        public void ShouldMultiSelect()
        {
            using(var transaction = Store.BeginTransaction())
            {
                transaction.Insert(new Product { Name = "Talking Elmo", Price = 100 }, "product-1");
                transaction.Insert(new SpecialProduct() { Name = "Lego set", Price = 200, BonusMaterial = "Out-takes"}, "product-2");

                transaction.Insert(new LineItem { ProductId = "product-1", Name = "Line 1", Quantity = 10 });
                transaction.Insert(new LineItem { ProductId = "product-1", Name = "Line 2", Quantity = 10 });
                transaction.Insert(new LineItem { PurchaseDate = DateTime.MaxValue, ProductId = "product-2", Name = "Line 3", Quantity = 20 });

                transaction.Commit();
            }

            using (var transaction = Store.BeginTransaction())
            {
                var lines = transaction.ExecuteReaderWithProjection("SELECT line.Id as line_id, line.Name as line_name, line.PurchaseDate as line_PurchaseDate, line.ProductId as line_productid, line.JSON as line_json, prod.Id as prod_id, prod.Name as prod_name, prod.JSON as prod_json from LineItem line inner join Product prod on prod.Id = line.ProductId", new CommandParameterValues(), map => new
                {
                    LineItem = map.Map<LineItem>("line"),
                    Product = map.Map<Product>("prod")
                }).ToList();

                Assert.Equal(3, lines.Count);
                Assert.True(lines[0].LineItem.Name == "Line 1" && lines[0].Product.Name == "Talking Elmo" && lines[0].Product.Price == 100);
                Assert.True(lines[1].LineItem.Name == "Line 2" && lines[1].Product.Name == "Talking Elmo" && lines[1].Product.Price == 100);
                Assert.True(lines[2].LineItem.Name == "Line 3" && lines[2].Product.Name == "Lego set" && lines[2].Product.Price == 200 &&
                            lines[2].Product is SpecialProduct sp && sp.BonusMaterial == "Out-takes");
            }
        }

        [Fact]
        public void ShouldShowNiceErrorIfFieldsAreTooLong()
        {
            // SQL normally thows "String or binary data would be truncated. The statement has been terminated."
            // Since we know the lengths, we show a better error first
            using (var transaction = Store.BeginTransaction())
            {
                var ex = Assert.Throws<StringTooLongException>(() => transaction.Insert(new Customer { FirstName = new string('A', 21), LastName = "Apple", LuckyNumbers = new[] { 12, 13 }, Nickname = "Ally", Roles = { "web-server", "app-server" } }));
                Assert.Equal("An attempt was made to store 21 characters in the Customer.FirstName column, which only allows 20 characters.", ex.Message);
            }
        }

        [Fact]
        public void ShouldShowFriendlyUniqueConstraintErrors()
        {
            using(var transaction = Store.BeginTransaction())
            {
                var customer1 = new Customer { FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] { 12, 13 }, Nickname = "Ally", Roles = { "web-server", "app-server" } };
                var customer2 = new Customer { FirstName = "Alice", LastName = "Appleby", LuckyNumbers = new[] { 12, 13 }, Nickname = "Ally", Roles = { "web-server", "app-server" } };
                var customer3 = new Customer { FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] { 12, 13 }, Nickname = "Ally", Roles = { "web-server", "app-server" } };

                transaction.Insert(customer1);
                transaction.Insert(customer2);
                var ex = Assert.Throws<UniqueConstraintViolationException>(() => transaction.Insert(customer3));

                Assert.Equal("Customers must have a unique name", ex.Message);
            }
        }
    }
}

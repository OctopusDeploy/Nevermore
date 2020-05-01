using System;
using System.Linq;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests
{
    public class RelationalStoreFixture : FixtureWithRelationalStore
    {
        [Test]
        public void ShouldGenerateIdsUnlessExplicitlyAssigned()
        {
            // The Id columns allow you to give records an ID, or use an auto-generated, unique ID
            using (var transaction = Store.BeginTransaction())
            {
                var customer1 = new Customer {Id = "Customers-Alice", FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] {12, 13}, Nickname = "Ally", Roles = {"web-server", "app-server"}};
                var customer2 = new Customer {FirstName = "Bob", LastName = "Banana", LuckyNumbers = new[] {12, 13}, Nickname = "B-man", Roles = {"web-server", "app-server"}};
                var customer3 = new Customer {FirstName = "Charlie", LastName = "Cherry", LuckyNumbers = new[] {12, 13}, Nickname = "Chazza", Roles = {"web-server", "app-server"}};
                transaction.Insert(customer1);
                transaction.Insert(customer2);
                transaction.Insert(customer3, new InsertOptions { CustomAssignedId = "Customers-Chazza"});

                customer1.Id.Should().Be("Customers-Alice");
                customer2.Id.Should().StartWith("Customers-");
                customer3.Id.Should().Be("Customers-Chazza");

                transaction.Commit();
            }
        }

        [Test]
        public void ShouldPersistReferenceCollectionsToAllowLikeSearches()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var customer1 = new Customer {FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] {12, 13}, Nickname = "Ally", Roles = {"web-server", "app-server"}};
                var customer2 = new Customer {FirstName = "Bob", LastName = "Banana", LuckyNumbers = new[] {12, 13}, Nickname = "B-man", Roles = {"db-server", "app-server"}};
                var customer3 = new Customer {FirstName = "Charlie", LastName = "Cherry", LuckyNumbers = new[] {12, 13}, Nickname = "Chazza", Roles = {"web-server", "app-server"}};
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
                customers.Count.Should().Be(2);
            }
        }

        [Test]
        public void ShouldPersistCollectionsToAllowInSearches()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var customer1 = new Customer {FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] {12, 13}, Nickname = "Ally", Roles = {"web-server", "app-server"}};
                var customer2 = new Customer {FirstName = "Bob", LastName = "Banana", LuckyNumbers = new[] {12, 13}, Nickname = "B-man", Roles = {"db-server", "app-server"}};
                var customer3 = new Customer {FirstName = "Charlie", LastName = "Cherry", LuckyNumbers = new[] {12, 13}, Nickname = "Chazza", Roles = {"web-server", "app-server"}};
                transaction.Insert(customer1);
                transaction.Insert(customer2);
                transaction.Insert(customer3);
                transaction.Commit();
            }

            // ReferenceCollection columns that are indexed are always stored in pipe-separated format with pipes at the front and end: |foo|bar|baz|
            using (var transaction = Store.BeginTransaction())
            {
                var customers = transaction.Query<Customer>()
                    .Where("LastName", ArraySqlOperand.In, new[] {"Apple", "Banana"})
                    .ToList();
                customers.Count.Should().Be(2);
            }
        }

        [Test]
        public void ShouldHandleIdsWithInOperand()
        {
            string customerId;
            using (var transaction = Store.BeginTransaction())
            {
                var customer = new Customer {FirstName = "Alice", LastName = "Apple"};
                transaction.Insert(customer);
                transaction.Commit();
                customerId = customer.Id;
            }

            using (var transaction = Store.BeginTransaction())
            {
                var customer = transaction.Query<Customer>()
                    .Where("Id", ArraySqlOperand.In, new[] {customerId})
                    .Stream()
                    .Single();
                customer.FirstName.Should().Be("Alice");
            }
        }

        [Test]
        public void ShouldMultiSelect()
        {
            using (var transaction = Store.BeginTransaction())
            {
                transaction.Insert(new Product {Name = "Talking Elmo", Price = 100}, new InsertOptions { CustomAssignedId = "product-1"});
                transaction.Insert(new SpecialProduct() {Name = "Lego set", Price = 200, BonusMaterial = "Out-takes"}, new InsertOptions { CustomAssignedId = "product-2"});

                transaction.Insert(new LineItem {ProductId = "product-1", Name = "Line 1", Quantity = 10});
                transaction.Insert(new LineItem {ProductId = "product-1", Name = "Line 2", Quantity = 10});
                transaction.Insert(new LineItem {PurchaseDate = DateTime.MaxValue, ProductId = "product-2", Name = "Line 3", Quantity = 20});

                transaction.Commit();
            }

            using (var transaction = Store.BeginTransaction())
            {
                var lines = transaction.Stream("SELECT line.Id as line_id, line.Name as line_name, line.PurchaseDate as line_PurchaseDate, line.ProductId as line_productid, line.JSON as line_json, prod.Id as prod_id, prod.Name as prod_name, prod.Type as prod_type, prod.JSON as prod_json from TestSchema.LineItem line inner join TestSchema.Product prod on prod.Id = line.ProductId", new CommandParameterValues(), map => new
                {
                    LineItem = map.Map<LineItem>("line"),
                    Product = map.Map<Product>("prod")
                }).ToList();

                lines.Count.Should().Be(3);
                Assert.True(lines[0].LineItem.Name == "Line 1" && lines[0].Product.Name == "Talking Elmo" && lines[0].Product.Price == 100);
                Assert.True(lines[1].LineItem.Name == "Line 2" && lines[1].Product.Name == "Talking Elmo" && lines[1].Product.Price == 100);
                Assert.True(lines[2].LineItem.Name == "Line 3" && lines[2].Product.Name == "Lego set" && lines[2].Product.Price == 200 &&
                            lines[2].Product is SpecialProduct sp && sp.BonusMaterial == "Out-takes");
            }
        }
        
        [Test]
        public void ShouldAllowNoPrefixOnProjectionMapping()
        {
            using (var transaction = Store.BeginTransaction())
            {
                transaction.Insert(new Product {Name = "Talking Elmo", Price = 100}, new InsertOptions { CustomAssignedId = "product-1"});
                transaction.Insert(new SpecialProduct() {Name = "Lego set", Price = 200, BonusMaterial = "Out-takes"}, new InsertOptions { CustomAssignedId = "product-2"});

                transaction.Insert(new LineItem {ProductId = "product-1", Name = "Line 1", Quantity = 10});
                transaction.Insert(new LineItem {ProductId = "product-1", Name = "Line 2", Quantity = 10});
                transaction.Insert(new LineItem {PurchaseDate = DateTime.MaxValue, ProductId = "product-2", Name = "Line 3", Quantity = 20});

                transaction.Commit();
            }

            using (var transaction = Store.BeginTransaction())
            {
                var lines = transaction.Stream("SELECT line.*, prod.Name as prod_name from TestSchema.LineItem line inner join TestSchema.Product prod on prod.Id = line.ProductId", new CommandParameterValues(), map => new
                {
                    LineItem = map.Map<LineItem>(string.Empty),
                    Product = map.Read(reader => reader["prod_name"].ToString())
                }).ToList();

                lines.Count.Should().Be(3);
                Assert.True(lines[0].LineItem.Name == "Line 1" && lines[0].Product == "Talking Elmo");
                Assert.True(lines[1].LineItem.Name == "Line 2" && lines[1].Product == "Talking Elmo");
                Assert.True(lines[2].LineItem.Name == "Line 3" && lines[2].Product == "Lego set");
            }
        }

        [Test]
        public void ShouldInsertOneRecordWithInsertMany()
        {
            var product = new Product {Name = "Talking Elmo", Price = 100};

            using (var transaction = Store.BeginTransaction())
            {
                transaction.InsertMany(new[] { product });
                transaction.Commit();
            }

            product.Id.Should().Be("Products-1");
        }
        
        [Test]
        public void ShouldInsertManyRecordsWithInsertMany()
        {
            var product1 = new Product {Name = "Talking Elmo", Price = 100};
            var product2 = new Product {Name = "Talking Elmo", Price = 100};

            using (var transaction = Store.BeginTransaction())
            {
                transaction.InsertMany(new[] {product1, product2});
                transaction.Commit();
            }

            product1.Id.Should().Be("Products-1");
            product2.Id.Should().Be("Products-2");
        }

        [Test]
        public void ShouldShowNiceErrorIfFieldsAreTooLong()
        {
            // SQL normally thows "String or binary data would be truncated. The statement has been terminated."
            // Since we know the lengths, we show a better error first
            using (var transaction = Store.BeginTransaction())
            {
                Action exec = () => transaction.Insert(
                    new Customer
                    {
                        FirstName = new string('A', 21),
                        LastName = "Apple",
                        LuckyNumbers = new[] {12, 13},
                        Nickname = "Ally",
                        Roles = {"web-server", "app-server"}
                    });
                exec.ShouldThrow<StringTooLongException>()
                    .WithMessage("An attempt was made to store 21 characters in the Customer.FirstName column, which only allows 20 characters.");
            }
        }

        [Test]
        public void ShouldShowFriendlyUniqueConstraintErrors()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var customer1 = new Customer {FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] {12, 13}, Nickname = "Ally", Roles = {"web-server", "app-server"}};
                var customer2 = new Customer {FirstName = "Alice", LastName = "Appleby", LuckyNumbers = new[] {12, 13}, Nickname = "Ally", Roles = {"web-server", "app-server"}};
                var customer3 = new Customer {FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] {12, 13}, Nickname = "Ally", Roles = {"web-server", "app-server"}};

                transaction.Insert(customer1);
                transaction.Insert(customer2);
                var ex = Assert.Throws<UniqueConstraintViolationException>(() => transaction.Insert(customer3));

                ex.Message.Should().Be("Customers must have a unique name");
            }
        }

        [Test]
        public void ShouldHandleRowVersionColumn()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var customer1 = new Customer {FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] {12, 13}, Nickname = "Ally", Roles = {"web-server", "app-server"}};
                transaction.Insert(customer1); // customer has a RowVersion column, but would have an error when inserting if the ReadOnly mapping was ignored
                var dbCustomer = transaction.Query<Customer>().Where(c => c.Id == customer1.Id).ToList().Single();
                dbCustomer.RowVersion.Length.Should().Be(8);
                dbCustomer.RowVersion.All(v => v == 0).Should().BeFalse();
            }
        }
        
        [Test]
        public void ShouldPersistAndLoadReferenceCollectionsOnSingleDocuments()
        {
            var customerId = string.Empty;
            using (var transaction = Store.BeginTransaction())
            {
                var customer = new Customer {FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] {12, 13}, Nickname = "Ally", Roles = {"web-server", "app-server"}};
                transaction.Insert(customer);
                customerId = customer.Id;
                transaction.Commit();
            }
            using (var transaction = Store.BeginTransaction())
            {
                var loadedCustomer = transaction.Load<Customer>(customerId);
                loadedCustomer.Roles.Count.Should().Be(2);
            }
        }
        
        
        [Test]
        public void ShouldUseIdPassedInToInsertMethod()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var customer = new Customer {FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] {12, 13}, Nickname = "Ally", Roles = {"web-server", "app-server"}};
                transaction.Insert(customer, new InsertOptions { CustomAssignedId = "12345" });
                Assert.That(customer.Id, Is.EqualTo("12345"), "Id passed in should be used");
            }
        }

        [Test]
        public void ShouldUseIdPassedInIfSame()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var customer = new Customer {Id = "12345", FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] {12, 13}, Nickname = "Ally", Roles = {"web-server", "app-server"}};
                transaction.Insert(customer, new InsertOptions { CustomAssignedId = "12345" });
                Assert.That(customer.Id, Is.EqualTo("12345"), "Id passed in should be used if same");
            }
        }

        [Test]
        public void ShouldThrowIfConflictingIdsPassedIn()
        {
            using (var transaction = Store.BeginTransaction())
            {
                Assert.Throws<ArgumentException>(() =>
                {
                    var customer = new Customer {Id = "123456", FirstName = "Alice", LastName = "Apple", LuckyNumbers = new[] {12, 13}, Nickname = "Ally", Roles = {"web-server", "app-server"}};
                    transaction.Insert(customer, new InsertOptions { CustomAssignedId = "12345" });
                });
            }
        }
    }
}
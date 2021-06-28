using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

#pragma warning disable 618

namespace Nevermore.IntegrationTests.RelationalTransaction
{
    public class LoadFixture : FixtureWithRelationalStore
    {
        [Test]
        public void LoadWithSingleId()
        {
            using var trn = Store.BeginTransaction();
            var _ = trn.Load<Product>("A");
        }

        [Test]
        public void LoadWithMultipleIds()
        {
            using var trn = Store.BeginTransaction();
            var _ = trn.LoadMany<Product>(new[] { "A", "B" });
        }

        [Test]
        public void LoadWithMultipleIdsWithDifferentLength()
        {
            using var trn = Store.BeginTransaction();

            var product1 = new Product
            {
                Id = "Products-1",
                Name = "TestProduct",
                Price = 2,
                Type = ProductType.Normal
            };
            trn.Insert(product1);

            var product2 = new Product
            {
                Id = "Products-133",
                Name = "TestProduct",
                Price = 2,
                Type = ProductType.Normal
            };
            trn.Insert(product2);
            trn.Commit();

            var ids = new[] {product1.Id, product2.Id};

            var products = trn.LoadMany<Product>(ids);

            products.Should().HaveCount(ids.Length);
        }

        [Test]
        public void LoadWithMoreThan2100Ids()
        {
            using var trn = Store.BeginTransaction();
            var _ = trn.LoadMany<Product>(Enumerable.Range(1, 3000).Select(n => "ID-" + n));
        }

        [Test]
        public void LoadStreamWithMoreThan2100Ids()
        {
            using var trn = Store.BeginTransaction();
            var _ = trn.LoadStream<Product>(Enumerable.Range(1, 3000).Select(n => "ID-" + n));
        }

        [Test]
        public void StoreNonInheritedTypesSerializesCorrectly()
        {
            using var trn = Store.BeginTransaction();

            var customer = new Customer
            {
                FirstName = "Bob",
                LastName = "Tester",
                Nickname = "Bob the builder",
                Id = "Customers-01".ToCustomerId()
            };

            trn.Insert(customer);

            var customers = trn.Stream<(string Id, string FirstName, string Nickname, string JSON)>("select Id, FirstName, Nickname, [JSON] from TestSchema.Customer").ToList();

            var c = customers.Single(p => p.Id == "Customers-01");
            c.FirstName.Should().Be("Bob", "Type isn't serializing into column correctly");
            c.Nickname.Should().Be("Bob the builder", "Type isn't serializing into column correctly");
            c.JSON.Should().Be("{\"LuckyNumbers\":null,\"ApiKey\":null,\"Passphrases\":null}");
        }

        [Test]
        public void StoreEnumInheritedTypesSerializesCorrectlyForNormalProduct()
        {
            using var trn = Store.BeginTransaction();
            var originalNormal = new Product()
            {
                Name = "Unicorn Dust",
                Id = "UD-01",
                Price = 11.1m,
            };

            trn.Insert(originalNormal);

            var allProducts = trn.Stream<(string Id, string Type, string JSON)>("select Id, Type, [JSON] from TestSchema.Product").ToList();

            var special = allProducts.Single(p => p.Id == "UD-01");
            special.Type.Should().Be("Normal", "Type isn't serializing into column correctly");
            special.JSON.Should().Be("{\"Price\":11.1}");
        }

        [Test]
        public void StoreEnumInheritedTypesSerializesCorrectlyForSpecialProduct()
        {
            using var trn = Store.BeginTransaction();
            var originalSpecial = new SpecialProduct
            {
                Name = "Unicorn Dust",
                BonusMaterial = "Directors Commentary",
                Id = "UD-01",
                Price = 11.1m,
            };

            var originalDud = new DodgyProduct
            {
                Id = "DO-01",
                Name = "Something",
                Price = 12.3m,
                Tax = 15m
            };

            trn.Insert(originalSpecial);
            trn.Insert(originalDud);

            var allProducts = trn.Stream<(string Id, string Type, string JSON)>("select Id, Type, [JSON] from TestSchema.Product").ToList();

            var special = allProducts.Single(p => p.Id == "UD-01");
            special.Type.Should().Be("Special", "Type isn't serializing into column correctly");
            special.JSON.Should().Be("{\"BonusMaterial\":\"Directors Commentary\",\"Price\":11.1}");
        }

        [Test]
        public void StoreAndLoadEnumInheritedTypes()
        {
            using var trn = Store.BeginTransaction();
            var originalNormal = new Product
            {
                Name = "Norm",
                Id = "NL-01",
                Price = 15.5m
            };

            var originalSpecial = new SpecialProduct
            {
                Name = "Unicorn Dust",
                BonusMaterial = "Directors Commentary",
                Id = "UD-01",
                Price = 11.1m
            };

            var originalDud = new DodgyProduct
            {
                Id = "DO-01",
                Name = "Something",
                Price = 12.3m,
                Tax = 15m
            };

            trn.Insert(originalNormal);
            trn.Insert(originalSpecial);
            trn.Insert(originalDud);

            var allProducts = trn.Query<Product>().ToList();
            Assert.True(allProducts.Exists(p =>
                p is SpecialProduct sp && sp.BonusMaterial == originalSpecial.BonusMaterial), "Special product didn't load correctly");
            Assert.True(allProducts.Exists(p => p is DodgyProduct dp && dp.Tax == originalDud.Tax), "Dodgy product didn't load correctly");

            var onlySpecial = trn.Query<SpecialProduct>().ToList();
            onlySpecial.Count.Should().Be(1);
            onlySpecial[0].BonusMaterial.Should().Be(originalSpecial.BonusMaterial);
        }

        [Test]
        public void StoreStringInheritedTypesSerializeCorrectly()
        {
            using var trn = Store.BeginTransaction();
            var brandA = new BrandA { Name = "Brand A", Description = "Details for Brand A." };
            var brandB = new BrandB { Name = "Brand B", Description = "Details for Brand B." };

            trn.Insert<Brand>(brandA);
            trn.Insert<Brand>(brandB);
            trn.Commit();

            var allBrands = trn.Stream<(string Name, string JSON)>("select Name, [JSON] from TestSchema.Brand").ToList();

            allBrands.SingleOrDefault(x => x.Name == "Brand A").Should().NotBeNull("Didn't retrieve BrandA");
            var brandToTestSerialization = allBrands.Single(x => x.Name == "Brand A");
            brandToTestSerialization.JSON.Should().Be("{\"Description\":\"Details for Brand A.\"}");
        }

        [Test]
        public void StoreAndLoadStringInheritedTypes()
        {
            using var trn = Store.BeginTransaction();
            var brandA = new BrandA { Name = "Brand A", Description = "Details for Brand A." };
            var brandB = new BrandB { Name = "Brand B", Description = "Details for Brand B." };

            trn.Insert<Brand>(brandA);
            trn.Insert<Brand>(brandB);
            trn.Commit();

            var allBrands = trn.Query<Brand>().ToList();

            allBrands.SingleOrDefault(x => x.Name == "Brand A").Should().NotBeNull("Didn't retrieve BrandA");
            allBrands.Single(x => x.Name == "Brand A").Should().BeOfType<BrandA>();
        }

        [Test]
        public void StoreAndLoadStringInheritedTypesThatAreProperties()
        {
            using var trn = Store.BeginTransaction();
            var machineA = new Machine { Name = "Machine A", Description = "Details for Machine A.", Endpoint = new PassiveTentacleEndpoint { Name = "Quiet tentacle" }};
            var machineB = new Machine { Name = "Machine B", Description = "Details for Machine B.", Endpoint = new ActiveTentacleEndpoint { Name = "Noisy tentacle" } };

            trn.Insert(machineA);
            trn.Insert(machineB);
            trn.Commit();

            var allMachines = trn.Query<Machine>().ToList();

            allMachines.SingleOrDefault(x => x.Name == "Machine A").Should().NotBeNull("Didn't retrieve Machine A");
            allMachines.Single(x => x.Name == "Machine A").Endpoint.Should().BeOfType<PassiveTentacleEndpoint>();
        }

        [Test]
        public void StoreAndLoadFromParameterizedRawSql()
        {
            using var trn = Store.BeginTransaction();
            InsertProductAndLineItems("Unicorn Hair", 2m, trn, 1);         // subtotal: $2 of Unicorn Hair
            InsertProductAndLineItems("Unicorn Poop", 3m, trn, 2, 3);      // subtotal: $15 of Unicorn Poop
            InsertProductAndLineItems("Unicorn Dust", 1m, trn, 2, 1, 7);   // subtotal: $10 of Unicorn Dust
            InsertProductAndLineItems("Fairy Bread", 10m, trn, 4);         // subtotal: $40 of Fairy Bread
            trn.Commit();

            var productSubtotalQuery =    @"SELECT
                                                    Id,
                                                    ProductId,
                                                    ProductName,
                                                    Subtotal
                                                FROM (
                                                    SELECT 
                                                        CAST(NEWID() AS nvarchar(36)) Id,
                                                        p.ProductId,
                                                        p.ProductName,
                                                        SUM(p.Price * l.Quantity) Subtotal
                                                    FROM (
                                                        SELECT
                                                            ProductId,
                                                            CAST(JSON_VALUE([JSON], '$.Quantity') AS int) Quantity
                                                        FROM TestSchema.LineItem
                                                    ) l
                                                    INNER JOIN (
                                                    SELECT 
                                                        Id ProductId,
                                                        [Name] ProductName,
                                                        CAST(JSON_VALUE([JSON], '$.Price') AS decimal) Price
                                                        FROM TestSchema.Product
                                                    ) p ON p.ProductId = l.ProductId
                                                    GROUP BY p.ProductId, p.ProductName
                                                ) p3
                                                WHERE Subtotal >= @minimum_subtotal";

            var doubleDigitUnicornProductSubtotals = trn.RawSqlQuery<ProductSubtotal>(productSubtotalQuery)
                .Where(s => s.ProductName.Contains("Unicorn"))
                .Parameter("minimum_subtotal", 10)
                .ToList();

            doubleDigitUnicornProductSubtotals.Should().HaveCount(2);
            doubleDigitUnicornProductSubtotals.Should().ContainSingle(s => s.ProductName == "Unicorn Poop").Which.Subtotal.Should().Be(15m);
            doubleDigitUnicornProductSubtotals.Should().ContainSingle(s => s.ProductName == "Unicorn Dust").Which.Subtotal.Should().Be(10m);
        }

        void InsertProductAndLineItems(string productName, decimal productPrice, IWriteTransaction trn, params int[] quantities)
        {
            var product = new Product
            {
                Name = productName,
                Price = productPrice
            };
            trn.Insert(product);
            foreach (var quantity in quantities)
            {
                var lineItem = new LineItem { ProductId = product.Id, Name = "Some line item", Quantity = quantity };
                trn.Insert(lineItem);
            }
        }

        [Test]
        public void StoreAndLoadAnyIdTypes()
        {
            using var trn = Store.BeginTransaction();
            var messageA = new MessageWithGuidId { Id = Guid.NewGuid(), Sender = "Sender A", Body = "Body of Message A" };

            trn.Insert(messageA);
            trn.Commit();

            var loadedMessageA = trn.Load<MessageWithGuidId>(messageA.Id);

            loadedMessageA.Sender.Should().Be(messageA.Sender);
            loadedMessageA.Body.Should().Be(messageA.Body);
        }

        [Test]
        public void LoadByWrongIdType_ShouldThrowArgumentException()
        {
            Action target = () =>
            {
                using var trn = Store.BeginTransaction();
                var _ = trn.Load<MessageWithGuidId>(1);
            };

            target.ShouldThrow<ArgumentException>().Which.Message.Should().Be("Provided Id of type 'System.Int32' does not match configured type of 'System.Guid'.");
        }

        [Test]
        public void StoreAndLoadManyForStringIdType()
        {
            using var trn = Store.BeginTransaction();
            var messages = new List<MessageWithStringId>
            {
                new MessageWithStringId {Id = "Messages-1", Sender = "Sender A", Body = "Body of Message A"},
                new MessageWithStringId {Id = "Messages-12", Sender = "Sender A", Body = "Body of Message A"}
            };

            foreach (var message in messages)
            {
                trn.Insert(message);
            }
            trn.Commit();

            var loadedMessages = trn.LoadMany<MessageWithStringId>(messages.Select(m => m.Id));

            loadedMessages.ShouldAllBeEquivalentTo(messages);
        }

        [Test]
        public void StoreAndLoadManyForIntIdType()
        {
            using var trn = Store.BeginTransaction();
            var messages = new List<MessageWithIntId>
            {
                new MessageWithIntId {Id = int.MinValue, Sender = "Sender A", Body = "Body of Message A"},
                new MessageWithIntId {Id = int.MaxValue, Sender = "Sender A", Body = "Body of Message A"}
            };

            foreach (var message in messages)
            {
                trn.Insert(message);
            }
            trn.Commit();

            var loadedMessages = trn.LoadMany<MessageWithIntId>(messages.Select(m => m.Id));

            loadedMessages.ShouldAllBeEquivalentTo(messages);
        }

        [Test]
        public void StoreAndLoadManyForLongIdType()
        {
            using var trn = Store.BeginTransaction();
            var messages = new List<MessageWithLongId>
            {
                new MessageWithLongId {Id = long.MinValue, Sender = "Sender A", Body = "Body of Message A"},
                new MessageWithLongId {Id = long.MaxValue, Sender = "Sender A", Body = "Body of Message A"}
            };

            foreach (var message in messages)
            {
                trn.Insert(message);
            }
            trn.Commit();

            var loadedMessages = trn.LoadMany<MessageWithLongId>(messages.Select(m => m.Id));

            loadedMessages.ShouldAllBeEquivalentTo(messages);
        }

        [Test]
        public void StoreAndLoadManyForGuidIdType()
        {
            using var trn = Store.BeginTransaction();
            var messages = new List<MessageWithGuidId>
            {
                new MessageWithGuidId {Id = Guid.NewGuid(), Sender = "Sender A", Body = "Body of Message A"},
                new MessageWithGuidId {Id = Guid.NewGuid(), Sender = "Sender A", Body = "Body of Message A"}
            };

            foreach (var message in messages)
            {
                trn.Insert(message);
            }
            trn.Commit();

            var loadedMessages = trn.LoadMany<MessageWithGuidId>(messages.Select(m => m.Id));

            loadedMessages.ShouldAllBeEquivalentTo(messages);
        }

        [Test]
        public void LoadManyByWrongIdType_ShouldThrowArgumentException()
        {
            Action target = () =>
            {
                using var trn = Store.BeginTransaction();
                var _ = trn.LoadMany<MessageWithGuidId>("Messages-1");
            };

            target.ShouldThrow<ArgumentException>().Which.Message.Should().Be("Provided Id of type 'System.String' does not match configured type of 'System.Guid'.");
        }
    }
}
using System;
using Nevermore.IntegrationTests.Model;
using NUnit.Framework;
using FluentAssertions;
using Nevermore.IntegrationTests.SetUp;

namespace Nevermore.IntegrationTests.RelationalTransaction
{
    public class DeleteFixture : FixtureWithRelationalStore
    {
        [Test]
        public void DeleteByEntity()
        {
            var id = AddTestProduct();

            using (var trn = Store.BeginTransaction())
            {
                var product = trn.Load<Product>(id);
                trn.Delete<Product, string>(product);
                trn.Commit();
            }

            using (var trn = Store.BeginTransaction())
                trn.Load<Product>(id).Should().BeNull();
        }

        [Test]
        public void DeleteById()
        {
            var id = AddTestProduct();

            using (var trn = Store.BeginTransaction())
            {
                trn.Delete<Product>(id);
                trn.Commit();
            }

            using (var trn = Store.BeginTransaction())
                trn.Load<Product>(id).Should().BeNull();
        }

        [Test]
        public void DeleteByWrongIdType_ShouldThrowArgumentException()
        {
            using (var trn = Store.BeginTransaction())
            {
                Action target = () => trn.Delete<Product>(Guid.NewGuid());

                target.ShouldThrow<ArgumentException>().Which.Message.Should().Be("Provided Id of type 'System.Guid' does not match configured type of 'System.String'.");
            }
        }

        string AddTestProduct()
        {
            using (var trn = Store.BeginTransaction())
            {
                var product = new Product()
                {
                    Name = "foo"
                };
                trn.Insert(product);
                trn.Commit();
                return product.Id;
            }
        }
    }
}
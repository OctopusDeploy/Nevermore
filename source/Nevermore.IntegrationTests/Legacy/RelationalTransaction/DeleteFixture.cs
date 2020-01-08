using FluentAssertions;
using Nevermore.IntegrationTests.Legacy.Model;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Legacy.RelationalTransaction
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
                trn.Delete(product);
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
                trn.DeleteById<Product>(id);
                trn.Commit();
            }

            using (var trn = Store.BeginTransaction())
                trn.Load<Product>(id).Should().BeNull();
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
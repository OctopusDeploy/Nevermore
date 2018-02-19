using System.Linq;
using Nevermore.Contracts;
using Nevermore;
using Nevermore.IntegrationTests.Model;
using Xunit;
using Xunit.Abstractions;

namespace Nevermore.IntegrationTests.RelationalTransaction
{
    public class LoadFixture : FixtureWithRelationalStore
    {
        public LoadFixture(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void LoadWithSingleId()
        {
            using (var trn = Store.BeginTransaction())
            {
                trn.Load<Product>("A");
            }
        }

        [Fact]
        public void LoadWithMultipleIds()
        {
            using (var trn = Store.BeginTransaction())
            {
                trn.Load<Product>(new[] {"A", "B"});
            }
        }


        [Fact]
        public void StoreAndLoadInheritedTypes()
        {
            using (var trn = Store.BeginTransaction())
            {
                var originalSpecial = new SpecialProduct()
                {
                    Name = "Unicorn Dust",
                    BonusMaterial = "Directors Commentary",
                    Id = "UD-01"
                };

                var originalDud = new DodgyProduct()
                {
                    Id = "DO-01",
                    Name = "Something",
                    Price = 12.3m,
                    Tax = 15m
                };

                trn.Insert<SpecialProduct>(originalSpecial);
                trn.Insert<DodgyProduct>(originalDud);

                var allProducts = trn.Query<Product>().ToList();
                Assert.True(allProducts.Exists(p => p is SpecialProduct sp && sp.BonusMaterial == originalSpecial.BonusMaterial));
                Assert.True(allProducts.Exists(p => p is DodgyProduct dp && dp.Tax == originalDud.Tax));

                var onlySpecial = trn.Query<SpecialProduct>().ToList();
                Assert.Equal(1, onlySpecial.Count);
                Assert.Equal(originalSpecial.BonusMaterial, onlySpecial[0].BonusMaterial);
            }
        }
    }
}
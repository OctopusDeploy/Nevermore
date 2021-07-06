using FluentAssertions;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class NoJsonFixture : FixtureWithRelationalStore
    {
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            NoMonkeyBusiness();

            ExecuteSql("create table TestSchema.Car([Id] nvarchar(50), [Name] nvarchar(100))");

            Store.Configuration.DocumentMaps.Register(new CarMap());
        }

        class Car
        {
            public string Id { get; set; }
            public string Name { get; set; }
        }

        class CarMap : DocumentMap<Car>
        {
            public CarMap()
            {
                Column(m => m.Name);

                JsonStorageFormat = JsonStorageFormat.NoJson;
            }
        }

        [Test]
        public void ShouldMapWithoutJson()
        {
            using var transaction = Store.BeginTransaction();
            transaction.Insert(new Car { Name = "Volvo" });

            var car = transaction.Load<Car>("Cars-1");
            car.Should().NotBeNull();
            car.Name.Should().Be("Volvo");

            car.Name = "Bertie";
            transaction.Update(car);

            car = transaction.Query<Car>().Where("Name = @name").Parameter("name", "Bertie").FirstOrDefault();
            car.Should().NotBeNull();
            car.Name.Should().Be("Bertie");

            transaction.Query<Car>().Count().Should().Be(1);

            transaction.Delete<Car, string>(car);

            transaction.Query<Car>().Count().Should().Be(0);

            car = transaction.Load<Car>("Cars-1");
            car.Should().BeNull();
        }
    }
}
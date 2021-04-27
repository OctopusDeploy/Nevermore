using System.Linq;
using BenchmarkDotNet.Jobs;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class MappingWithInheritanceFixture : FixtureWithRelationalStore
    {
        [Test]
        public void ShouldNotSerializeIfColumnExists()
        {
            // Arrange
            using var transaction = Store.BeginTransaction();
            var aircraft = new Aircraft(AircraftType.FixedWing, "VH-ABC");
            var boat = new Boat("Titanic", "Liverpool, UK");
            
            // Act
            transaction.Insert(aircraft);
            transaction.Insert(boat);
            transaction.Commit();
            
            // Assert
            var aircraftJObject = GetJObject<Aircraft>(transaction, aircraft.Id);
            var aircraftRegistrationToken = aircraftJObject[nameof(aircraft.Registration)];
            Assert.IsNull(aircraftRegistrationToken);
            
            var boatJObject = GetJObject<Boat>(transaction, boat.Id);
            var boatRegistrationToken = boatJObject[nameof(boat.Registration)];
            Assert.IsNull(boatRegistrationToken);
        }
        
        [Test]
        public void ShouldSerializeIfColumnDoesNotExist()
        {
            // Arrange
            using var transaction = Store.BeginTransaction();
            var aircraft = new Aircraft(AircraftType.FixedWing, "VH-ABC");
            var boat = new Boat("Titanic", "Liverpool, UK");
            
            // Act
            transaction.Insert(aircraft);
            transaction.Insert(boat);
            transaction.Commit();
            
            // Assert
            var aircraftJObject = GetJObject<Aircraft>(transaction, aircraft.Id);
            var aircraftTypeToken = aircraftJObject[nameof(aircraft.Type)];
            Assert.NotNull(aircraftTypeToken);
            Assert.AreEqual(aircraftTypeToken.Value<string>(), aircraft.Type.ToString());
            
            var boatJObject = GetJObject<Boat>(transaction, boat.Id);
            var boatPortOfRegistryToken = boatJObject[nameof(boat.PortOfRegistry)];
            Assert.NotNull(boatPortOfRegistryToken);
            Assert.AreEqual(boatPortOfRegistryToken.Value<string>(), boat.PortOfRegistry);
        }

        JObject GetJObject<TVehicle>(IReadTransaction transaction, string id) where TVehicle : Vehicle
        {
            string typeName = typeof(TVehicle).Name;
            
            // Command parameters cannot be used for table names, hence the string interpolation
            #pragma warning disable NV0007
            var json = transaction.Stream<string>(
                    $"SELECT [JSON] FROM [TestSchema].[{typeName}] WHERE [Id] = @id",
                    new CommandParameterValues
                    {
                        {"id", id}
                    })
                .Single();
            var jObject = JObject.Parse(json);

            return jObject;
        }
    }
}
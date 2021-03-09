using System.Linq;
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
            
            // Act
            transaction.Insert(aircraft);
            transaction.Commit();
            
            // Assert
            var jObject = GetJObject(transaction, aircraft.Id);
            var registrationToken = jObject[nameof(aircraft.Registration)];
            Assert.IsNull(registrationToken);
        }
        
        [Test]
        public void ShouldSerializeIfColumnDoesNotExist()
        {
            // Arrange
            using var transaction = Store.BeginTransaction();
            var aircraft = new Aircraft(AircraftType.FixedWing, "VH-ABC");
            
            // Act
            transaction.Insert(aircraft);
            transaction.Commit();
            
            // Assert
            var jObject = GetJObject(transaction, aircraft.Id);
            var typeToken = jObject[nameof(aircraft.Type)];
            Assert.NotNull(typeToken);
            Assert.AreEqual(typeToken.Value<string>(), aircraft.Type.ToString());
        }

        JObject GetJObject(IRelationalTransaction transaction, string id)
        {   
            var parameters = new CommandParameterValues();
            parameters.Add("id", id);
            var json = transaction.Stream<string>(
                "SELECT [JSON] FROM [TestSchema].[Aircraft] WHERE [Id] = @id",
                parameters)
                .Single();
            var jObject = JObject.Parse(json);

            return jObject;
        }
    }
}
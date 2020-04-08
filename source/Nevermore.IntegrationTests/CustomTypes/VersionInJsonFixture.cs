using System.Collections.Generic;
using FluentAssertions;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.CustomTypes
{
    public class VersionInJsonFixture : FixtureWithRelationalStore
    {
        [Test]
        public void ShouldWorkInJsonData()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new Package
                {
                    Version =  new Version(1, 2, 3)
                };
                transaction.Insert(model);

                var read = transaction.Query<PackageToTestSerialization>()
                    .FirstOrDefault();
                read.JSON.Should().Be(JsonConvert.SerializeObject(new
                {
                    Version = "1.2.3"
                }));
            }
        }

        [Test]
        public void ShouldBeAbleToReadBackFromJson()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new Package
                {
                    Version =  new Version(1, 2, 3)
                };
                transaction.Insert(model);

                var read = transaction.Query<Package>()
                    .FirstOrDefault();
                read.Should().NotBeSameAs(model);
                read.Version.Should().NotBeSameAs(model.Version);
                read.Version.Major.Should().Be(1);
                read.Version.Minor.Should().Be(2);
                read.Version.Patch.Should().Be(3);
            }
        }
        
        protected override IEnumerable<DocumentMap> AddCustomMappingsForSchemaGeneration()
        {
            return new DocumentMap[]
            {
                new PackageWithJsonMap()
            };
        }

        protected override IEnumerable<DocumentMap> AddCustomMappings()
        {
            return new DocumentMap[]
            {
                new PackageWithJsonMap(),
                new PackageToTestSerializationMap()
            };
        }

        protected override IEnumerable<CustomTypeSerialization> CustomTypeDefinitions()
        {
            return new[]
            {
                new VersionCustomTypeSerialization()
            };
        }

        class  PackageWithJsonMap : DocumentMap<Package>
        {
            public PackageWithJsonMap()
            {
                TableName = "PackageWithVersionInJson";
            }
        }

        class PackageToTestSerialization : IId
        {
            public string Id { get; set; }
            public string JSON { get; set; }
        }

        class PackageToTestSerializationMap : DocumentMap<PackageToTestSerialization>
        {
            public PackageToTestSerializationMap()
            {
                TableName = "PackageWithVersionInJson";

                Column(m => m.JSON);
            }
        }
    }
}
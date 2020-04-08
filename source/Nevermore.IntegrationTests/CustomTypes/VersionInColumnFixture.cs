using System.Collections.Generic;
using FluentAssertions;
using Nevermore.Contracts;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.CustomTypes
{
    public class VersionInColumnFixture : FixtureWithRelationalStore
    {
        [Test]
        public void ShouldWorkInColumn()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new Package
                {
                    Version =  new Version(1, 2, 3)
                };
                transaction.Insert(model);

                var read = transaction.Query<CustomTypeWithColumnToTestSerialization>()
                    .FirstOrDefault();
                read.Version.Should().Be("1.2.3");
            }
        }

        [Test]
        public void ShouldBeAbleToReadBackFromColumn()
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
                new PackageWithColumnMap()
            };
        }

        protected override IEnumerable<DocumentMap> AddCustomMappings()
        {
            return new DocumentMap[]
            {
                new PackageWithColumnMap(),
                new PackageWithColumnToTestSerializationMap()
            };
        }

        protected override IEnumerable<CustomTypeSerialization> CustomTypeDefinitions()
        {
            return new[]
            {
                new VersionCustomTypeSerialization()
            };
        }

        class PackageWithColumnMap : DocumentMap<Package>
        {
            public PackageWithColumnMap()
            {
                TableName = "PackageWithVersionInColumn";

                Column(x => x.Version);
            }
        }

        class CustomTypeWithColumnToTestSerialization : IId
        {
            public string Id { get; set; }
            public string Version { get; set; }
            public string JSON { get; set; }
        }

        class PackageWithColumnToTestSerializationMap : DocumentMap<CustomTypeWithColumnToTestSerialization>
        {
            public PackageWithColumnToTestSerializationMap()
            {
                TableName = "PackageWithVersionInColumn";
                
                Column(m => m.Version);
                Column(m => m.JSON);
            }
        }
    }
}
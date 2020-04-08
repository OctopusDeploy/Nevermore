using System.Collections.Generic;
using FluentAssertions;
using Nevermore.Contracts;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.CustomTypes
{
    public class TinyTypeInColumnFixture : FixtureWithRelationalStore
    {
        [Test]
        public void ShouldWorkInColumn()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new Release
                {
                    ProjectId =  new ProjectId("Projects-1")
                };
                transaction.Insert(model);

                var read = transaction.Query<ReleaseWithColumnToTestSerialization>()
                    .FirstOrDefault();
                read.ProjectId.Should().Be("Projects-1");
            }
        }

        [Test]
        public void ShouldBeAbleToReadBackFromColumn()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new Release
                {
                    ProjectId =  new ProjectId("Projects-2")
                };
                transaction.Insert(model);

                var read = transaction.Query<Release>()
                    .FirstOrDefault();
                read.Should().NotBeSameAs(model);
                read.ProjectId.Should().NotBeSameAs(model.ProjectId);
                read.ProjectId.Value.Should().Be("Projects-2");
            }
        }

        protected override IEnumerable<DocumentMap> AddCustomMappingsForSchemaGeneration()
        {
            return new DocumentMap[]
            {
                new ReleaseWithColumnMap()
            };
        }

        protected override IEnumerable<DocumentMap> AddCustomMappings()
        {
            return new DocumentMap[]
            {
                new ReleaseWithColumnMap(),
                new ReleaseWithColumnToTestSerializationMap()
            };
        }

        protected override IEnumerable<CustomTypeSerialization> CustomTypeDefinitions()
        {
            return new[]
            {
                new TinyTypeCustomTypeSerialization()
            };
        }

        class Release : IId
        {
            public string Id { get; set; }
            public ProjectId ProjectId { get; set; }
        }

        class ReleaseWithColumnMap : DocumentMap<Release>
        {
            public ReleaseWithColumnMap()
            {
                TableName = "ReleaseWithTinyTypeColumn";

                Column(x => x.ProjectId);
            }
        }

        class ReleaseWithColumnToTestSerialization : IId
        {
            public string Id { get; set; }
            public string ProjectId { get; set; }
            public string JSON { get; set; }
        }

        class ReleaseWithColumnToTestSerializationMap : DocumentMap<ReleaseWithColumnToTestSerialization>
        {
            public ReleaseWithColumnToTestSerializationMap()
            {
                TableName = "ReleaseWithTinyTypeColumn";
                
                Column(m => m.ProjectId);
                Column(m => m.JSON);
            }
        }
    }
}
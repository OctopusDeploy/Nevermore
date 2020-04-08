using System.Collections.Generic;
using FluentAssertions;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.CustomTypes
{
    public class TinyTypeInJsonFixture : FixtureWithRelationalStore
    {
        [Test]
        public void ShouldWorkInJson()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new ReleaseWithJson
                {
                    ProjectId =  new ProjectId("Projects-1")
                };
                transaction.Insert(model);

                var read = transaction.Query<ReleaseWithJsonToTestSerialization>()
                    .FirstOrDefault();
                read.JSON.Should().Be(JsonConvert.SerializeObject(new
                {
                    ProjectId = "Projects-1"
                }));
            }
        }

        [Test]
        public void ShouldBeAbleToReadBackFromJson()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new ReleaseWithJson
                {
                    ProjectId =  new ProjectId("Projects-2")
                };
                transaction.Insert(model);

                var read = transaction.Query<ReleaseWithJson>()
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
                new ReleaseWithJsonMap()
            };
        }

        protected override IEnumerable<DocumentMap> AddCustomMappings()
        {
            return new DocumentMap[]
            {
                new ReleaseWithJsonMap(),
                new ReleaseWithJsonToTestSerializationMap()
            };
        }

        protected override IEnumerable<CustomTypeSerialization> CustomTypeDefinitions()
        {
            return new[]
            {
                new TinyTypeCustomTypeSerialization()
            };
        }

        class ReleaseWithJson : IId
        {
            public string Id { get; set; }
            public ProjectId ProjectId { get; set; }
        }

        class ReleaseWithJsonMap : DocumentMap<ReleaseWithJson>
        {
            public ReleaseWithJsonMap()
            {
                TableName = "ReleaseWithTinyTypeJson";
            }
        }

        class ReleaseWithJsonToTestSerialization : IId
        {
            public string Id { get; set; }
            public string JSON { get; set; }
        }

        class ReleaseWithJsonToTestSerializationMap : DocumentMap<ReleaseWithJsonToTestSerialization>
        {
            public ReleaseWithJsonToTestSerializationMap()
            {
                TableName = "ReleaseWithTinyTypeJson";
                
                Column(m => m.JSON);
            }
        }
    }
}
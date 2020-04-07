using System.Collections.Generic;
using FluentAssertions;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.CustomTypes
{
    public class CustomSingleTypeInJsonFixture : FixtureWithRelationalStore
    {
        [Test]
        public void ShouldWorkInJsonData()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new ModelTypeWithJson
                {
                    CustomTypeUsedAsAProperty =  new CustomTypeUsedAsAProperty
                    {
                        Name = "foo",
                        Value = 10
                    }
                };
                transaction.Insert(model);

                var read = transaction.Query<CustomTypeToTestSerialization>()
                    .FirstOrDefault();
                read.JSON.Should().Be(JsonConvert.SerializeObject(new
                {
                    CustomTypeUsedAsAProperty = JsonConvert.SerializeObject(new CustomTypeUsedAsAProperty
                    {
                        Name = "foo",
                        Value = 10
                    })
                }));
            }
        }

        [Test]
        public void ShouldBeAbleToReadBackFromJson()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new ModelTypeWithJson
                {
                    CustomTypeUsedAsAProperty =  new CustomTypeUsedAsAProperty
                    {
                        Name = "bar",
                        Value = 15
                    }
                };
                transaction.Insert(model);

                var read = transaction.Query<ModelTypeWithJson>()
                    .FirstOrDefault();
                read.Should().NotBeSameAs(model);
                read.CustomTypeUsedAsAProperty.Should().NotBeSameAs(model.CustomTypeUsedAsAProperty);
                read.CustomTypeUsedAsAProperty.Name.Should().Be("bar");
                read.CustomTypeUsedAsAProperty.Value.Should().Be(15);
            }
        }
        
        protected override IEnumerable<DocumentMap> AddCustomMappingsForSchemaGeneration()
        {
            return new DocumentMap[]
            {
                new ModelTypeWithJsonMap()
            };
        }

        protected override IEnumerable<DocumentMap> AddCustomMappings()
        {
            return new DocumentMap[]
            {
                new ModelTypeWithJsonMap(),
                new CustomTypeToTestSerializationMap()
            };
        }

        protected override IEnumerable<CustomTypeDefinition> CustomTypeDefinitions()
        {
            return new[]
            {
                new CustomTypeUsedAsAPropertyCustomTypeDefinition()
            };
        }

        class ModelTypeWithJson : IId
        {
            public string Id { get; set; }
            public CustomTypeUsedAsAProperty CustomTypeUsedAsAProperty { get; set; }
        }

        class  ModelTypeWithJsonMap : DocumentMap<ModelTypeWithJson>
        {
        }

        class CustomTypeToTestSerialization : IId
        {
            public string Id { get; set; }
            public string JSON { get; set; }
        }

        class CustomTypeToTestSerializationMap : DocumentMap<CustomTypeToTestSerialization>
        {
            public CustomTypeToTestSerializationMap()
            {
                TableName = "ModelTypeWithJson";

                Column(m => m.JSON);
            }
        }
    }
}
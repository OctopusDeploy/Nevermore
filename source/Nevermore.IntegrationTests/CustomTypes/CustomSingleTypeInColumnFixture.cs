using System.Collections.Generic;
using FluentAssertions;
using Nevermore.Contracts;
using Nevermore.Mapping;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.CustomTypes
{
    public class CustomSingleTypeInColumnFixture : FixtureWithRelationalStore
    {
        [Test]
        public void ShouldWorkInColumn()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new ModelTypeWithColumn
                {
                    CustomTypeUsedAsAProperty =  new CustomTypeUsedAsAProperty
                    {
                        Name = "foo",
                        Value = 10
                    }
                };
                transaction.Insert(model);

                var read = transaction.Query<CustomTypeWithColumnToTestSerialization>()
                    .FirstOrDefault();
                read.CustomTypeUsedAsAProperty.Should().Be(JsonConvert.SerializeObject(model.CustomTypeUsedAsAProperty));
            }
        }

        [Test]
        public void ShouldBeAbleToReadBackFromColumn()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new ModelTypeWithColumn
                {
                    CustomTypeUsedAsAProperty =  new CustomTypeUsedAsAProperty
                    {
                        Name = "bar",
                        Value = 15
                    }
                };
                transaction.Insert(model);

                var read = transaction.Query<ModelTypeWithColumn>()
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
                new ModelTypeWithColumnMap()
            };
        }

        protected override IEnumerable<DocumentMap> AddCustomMappings()
        {
            return new DocumentMap[]
            {
                new ModelTypeWithColumnMap(),
                new CustomTypeWithColumnToTestSerializationMap()
            };
        }

        protected override IEnumerable<CustomTypeDefinition> CustomTypeDefinitions()
        {
            return new[]
            {
                new CustomTypeUsedAsAPropertyCustomTypeDefinition()
            };
        }

        public class ModelTypeWithColumn : IId
        {
            public string Id { get; set; }
            public CustomTypeUsedAsAProperty CustomTypeUsedAsAProperty { get; set; }
        }

        class ModelTypeWithColumnMap : DocumentMap<ModelTypeWithColumn>
        {
            public ModelTypeWithColumnMap()
            {
                Column(x => x.CustomTypeUsedAsAProperty);
            }
        }

        class CustomTypeWithColumnToTestSerialization : IId
        {
            public string Id { get; set; }
            public string CustomTypeUsedAsAProperty { get; set; }
            public string JSON { get; set; }
        }

        class CustomTypeWithColumnToTestSerializationMap : DocumentMap<CustomTypeWithColumnToTestSerialization>
        {
            public CustomTypeWithColumnToTestSerializationMap()
            {
                TableName = "ModelTypeWithColumn";
                
                Column(m => m.CustomTypeUsedAsAProperty);
                Column(m => m.JSON);
            }
        }
    }
}
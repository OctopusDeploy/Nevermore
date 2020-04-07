using System;
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
                var model = new ModelTypeWithJson
                {
                    ProjectId =  new ProjectId("Projects-1")
                };
                transaction.Insert(model);

                var read = transaction.Query<CustomTypeWithJsonToTestSerialization>()
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
                var model = new ModelTypeWithJson
                {
                    ProjectId =  new ProjectId("Projects-2")
                };
                transaction.Insert(model);

                var read = transaction.Query<ModelTypeWithJson>()
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
                new ModelTypeWithJsonMap()
            };
        }

        protected override IEnumerable<DocumentMap> AddCustomMappings()
        {
            return new DocumentMap[]
            {
                new ModelTypeWithJsonMap(),
                new CustomTypeWithJsonToTestSerializationMap()
            };
        }

        protected override IEnumerable<CustomTypeDefinition> CustomTypeDefinitions()
        {
            return new[]
            {
                new TinyTypeCustomTypeDefinition()
            };
        }

        class TinyTypeCustomTypeDefinition : CustomTypeDefinition
        {
            public override bool CanConvertType(Type type)
            {
                return typeof(TinyType<string>).IsAssignableFrom(type);
            }

            public override object ToDbValue(object instance, bool isForJsonSerialization)
            {
                return ((TinyType<string>) instance).Value;
            }

            public override object ConvertToDbValue(object instance)
            {
                throw new NotImplementedException();
            }

            public override object FromDbValue(object value, Type targetType, bool isForJsonSerialization)
            {
                var tinyType = Activator.CreateInstance(targetType, value);
                return tinyType;
            }

            public override object ConvertFromDbValue(object value, Type targetType)
            {
                throw new NotImplementedException();
            }
        }

        class TinyType<T>
        {
            public TinyType(T value)
            {
                Value = value;
            }

            public T Value { get; }
        }

        class ProjectId : TinyType<string>
        {
            public ProjectId(string value) : base(value)
            {
            }
        }

        class ModelTypeWithJson : IId
        {
            public string Id { get; set; }
            public ProjectId ProjectId { get; set; }
        }

        class ModelTypeWithJsonMap : DocumentMap<ModelTypeWithJson>
        {
            public ModelTypeWithJsonMap()
            {
                TableName = "TinyTypeWithJson";
            }
        }

        class CustomTypeWithJsonToTestSerialization : IId
        {
            public string Id { get; set; }
            public string JSON { get; set; }
        }

        class CustomTypeWithJsonToTestSerializationMap : DocumentMap<CustomTypeWithJsonToTestSerialization>
        {
            public CustomTypeWithJsonToTestSerializationMap()
            {
                TableName = "TinyTypeWithJson";
                
                Column(m => m.JSON);
            }
        }
    }
}
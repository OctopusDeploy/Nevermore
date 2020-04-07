using System;
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
                var model = new ModelTypeWithColumn
                {
                    ProjectId =  new ProjectId("Projects-1")
                };
                transaction.Insert(model);

                var read = transaction.Query<CustomTypeWithColumnToTestSerialization>()
                    .FirstOrDefault();
                read.ProjectId.Should().Be("Projects-1");
            }
        }

        [Test]
        public void ShouldBeAbleToReadBackFromColumn()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new ModelTypeWithColumn
                {
                    ProjectId =  new ProjectId("Projects-2")
                };
                transaction.Insert(model);

                var read = transaction.Query<ModelTypeWithColumn>()
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

        class ModelTypeWithColumn : IId
        {
            public string Id { get; set; }
            public ProjectId ProjectId { get; set; }
        }

        class ModelTypeWithColumnMap : DocumentMap<ModelTypeWithColumn>
        {
            public ModelTypeWithColumnMap()
            {
                TableName = "TinyTypeWithColumn";

                Column(x => x.ProjectId);
            }
        }

        class CustomTypeWithColumnToTestSerialization : IId
        {
            public string Id { get; set; }
            public string ProjectId { get; set; }
            public string JSON { get; set; }
        }

        class CustomTypeWithColumnToTestSerializationMap : DocumentMap<CustomTypeWithColumnToTestSerialization>
        {
            public CustomTypeWithColumnToTestSerializationMap()
            {
                TableName = "TinyTypeWithColumn";
                
                Column(m => m.ProjectId);
                Column(m => m.JSON);
            }
        }
    }
}
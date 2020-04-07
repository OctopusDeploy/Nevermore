using System;
using System.Collections.Generic;
using System.Data;
using FluentAssertions;
using Nevermore.Contracts;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.CustomTypes
{
    public class CustomTypeFixture : FixtureWithRelationalStore<CustomIntegrationTestDatabase>
    {
        [Test]
        public void ShouldWorkInColumn()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new CustomIntegrationTestDatabase.ModelTypeWithColumn
                {
                    SomeCustomType =  new CustomIntegrationTestDatabase.SomeCustomType
                    {
                        Name = "foo"
                    }
                };
                transaction.Insert(model);

                var read = transaction.Query<CustomIntegrationTestDatabase.CustomTypeWithColumnToTestSerialization>()
                    .FirstOrDefault();
                read.SomeCustomType.Should().Be("foo");
            }
        }

        [Test]
        public void ShouldBeAbleToReadBackFromColumn()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new CustomIntegrationTestDatabase.ModelTypeWithColumn
                {
                    SomeCustomType =  new CustomIntegrationTestDatabase.SomeCustomType
                    {
                        Name = "foo"
                    }
                };
                transaction.Insert(model);

                var read = transaction.Query<CustomIntegrationTestDatabase.ModelTypeWithColumn>()
                    .FirstOrDefault();
                read.Should().NotBeSameAs(model);
                read.SomeCustomType.Should().NotBeSameAs(model.SomeCustomType);
                read.SomeCustomType.Name.Should().Be("foo");
            }
        }

        [Test]
        public void ShouldWorkInJsonData()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new CustomIntegrationTestDatabase.ModelTypeWithJson
                {
                    SomeCustomType =  new CustomIntegrationTestDatabase.SomeCustomType
                    {
                        Name = "foo"
                    }
                };
                transaction.Insert(model);

                var read = transaction.Query<CustomIntegrationTestDatabase.CustomTypeToTestSerialization>()
                    .FirstOrDefault();
                read.SomeCustomType.Should().Be("foo");
            }
        }

        [Test]
        public void ShouldBeAbleToReadBackFromJson()
        {
            using (var transaction = Store.BeginTransaction())
            {
                var model = new CustomIntegrationTestDatabase.ModelTypeWithJson
                {
                    SomeCustomType =  new CustomIntegrationTestDatabase.SomeCustomType
                    {
                        Name = "foo"
                    }
                };
                transaction.Insert(model);

                var read = transaction.Query<CustomIntegrationTestDatabase.ModelTypeWithJson>()
                    .FirstOrDefault();
                read.Should().NotBeSameAs(model);
                read.SomeCustomType.Should().NotBeSameAs(model.SomeCustomType);
                read.SomeCustomType.Name.Should().Be("foo");
            }
        }
    }

    public class CustomIntegrationTestDatabase : IntegrationTestDatabase
    {
        public class SomeCustomType
        {
            public string Name { get; set; }
        }

        class SomeCustomTypeDefinition : CustomSingleTypeDefinition
        {
            public override Type ModelType => typeof(SomeCustomType);
            public override DbType DbType => DbType.String;
            public override int MaxLength => 50;

            public override object ToDbValue(object instance)
            {
                return ((SomeCustomType) instance).Name;
            }
            public override object FromDbValue(object value)
            {
                return new SomeCustomType { Name = (string)value };
            }
        }

        public class ModelTypeWithColumn : IId
        {
            public string Id { get; set; }
            public SomeCustomType SomeCustomType { get; set; }
        }

        class ModelTypeWithColumnMap : DocumentMap<ModelTypeWithColumn>
        {
            public ModelTypeWithColumnMap()
            {
                Column(x => x.SomeCustomType);
            }
        }

        public class ModelTypeWithJson : IId
        {
            public string Id { get; set; }
            public SomeCustomType SomeCustomType { get; set; }
        }

        class ModelTypeWithJsonMap : DocumentMap<ModelTypeWithJson>
        {
        }

        protected override IEnumerable<DocumentMap> AddCustomMappingsForSchemaGeneration()
        {
            return new DocumentMap[]
            {
                new ModelTypeWithColumnMap(),
                new ModelTypeWithJsonMap()
            };
        }

        protected override IEnumerable<DocumentMap> AddCustomMappings()
        {
            return new DocumentMap[]
            {
                new ModelTypeWithColumnMap(),
                new CustomTypeWithColumnToTestSerializationMap(), 
                new ModelTypeWithJsonMap(),
                new CustomTypeToTestSerializationMap()
            };
        }

        protected override IEnumerable<ICustomTypeDefinition> CustomTypeDefinitions()
        {
            return new[] {new SomeCustomTypeDefinition()};
        }
        
        public class CustomTypeWithColumnToTestSerialization : IId
        {
            public string Id { get; set; }
            public string SomeCustomType { get; set; }
            public string JSON { get; set; }
        }

        public class CustomTypeWithColumnToTestSerializationMap : DocumentMap<CustomTypeWithColumnToTestSerialization>
        {
            public CustomTypeWithColumnToTestSerializationMap()
            {
                TableName = "ModelTypeWithColumn";
                
                Column(m => m.SomeCustomType);
                Column(m => m.JSON);
            }
        }

        public class CustomTypeToTestSerialization : IId
        {
            public string Id { get; set; }
            public string SomeCustomType { get; set; }
            public string JSON { get; set; }
        }

        public class CustomTypeToTestSerializationMap : DocumentMap<CustomTypeToTestSerialization>
        {
            public CustomTypeToTestSerializationMap()
            {
                TableName = "ModelTypeWithJson";

                Column(m => m.JSON);
            }
        }
    }
}
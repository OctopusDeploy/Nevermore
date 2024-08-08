using System;
using System.Data;
using System.Data.Common;
using FluentAssertions;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using NUnit.Framework;
using Microsoft.Data.SqlClient.Server;
using Nevermore.Advanced.TypeHandlers;

namespace Nevermore.IntegrationTests.Advanced
{
    [TestFixture]
    public class CompositeIdentityFixture : FixtureWithRelationalStore
    {
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            ExecuteSql("create table TestSchema.DatabaseModel (Id INT IDENTITY, Name varchar(12))");
            Mappings.Register(new DatabaseModelMap());
            Configuration.TypeHandlers.Register(new CompositeTypeHandler());
        }

        class Composite
        {
            public int Value { get; }

            public Composite(int value)
            {
                Value = value;
            }
        }

        class DatabaseModel
        {
            public Composite Id { get; private set; }
            public string Name { get; private set; }

            DatabaseModel()
            {
            }

            public DatabaseModel(string name)
            {
                Name = name;
            }
        }

        class DatabaseModelMap : DocumentMap<DatabaseModel>
        {
            public DatabaseModelMap()
            {
                Id(m => m.Id)
                    .Identity()
                    .KeyHandler(new CompositePrimaryKeyHandler());
                Column(m => m.Name);
                JsonStorageFormat = JsonStorageFormat.NoJson;
            }
        }
        class CompositeTypeHandler : ITypeHandler
        {
            public bool CanConvert(Type objectType)
            {
                return objectType == typeof(Composite);
            }

            public object ReadDatabase(DbDataReader reader, int columnIndex)
            {
                if (reader.IsDBNull(columnIndex))
                    return default(Composite);
                var value = reader.GetInt32(columnIndex);
                return new Composite(value);
            }

            public void WriteDatabase(DbParameter parameter, object value)
            {
                parameter.Value = ((Composite) value)?.Value;
            }
        }
        class CompositePrimaryKeyHandler : PrimaryKeyHandler<Composite>
        {
            public override SqlMetaData GetSqlMetaData(string name) => new SqlMetaData(name, SqlDbType.Int);

            public override object GetNextKey(IKeyAllocator keyAllocator, string tableName)
            {
                return keyAllocator.NextId(tableName);
            }
        }

        [Test]
        public void ShouldRoundTrip()
        {
            var m = new DatabaseModel("Test");
            using (var writeTransaction = Store.BeginWriteTransaction())
            {
                writeTransaction.Insert(m);
                writeTransaction.Commit();
            }

            using (var readTransaction = Store.BeginReadTransaction())
            {
                readTransaction.Load<DatabaseModel>(m.Id.Value)
                    .Should()
                    .NotBeNull();
            }
        }
    }


}
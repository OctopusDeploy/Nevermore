using System;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class CheckConstraintsFixture : FixtureWithRelationalStore
    {
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();

            ExecuteSql(@"
                create table Person (
                    Id nvarchar(200) not null,
                    [JSON] nvarchar(max) null constraint CK_Person_JSON check ([JSON] is null or IsJson([JSON]) > 0),
                    [JSONBlob] varbinary(max) null constraint CK_Person_JSONBlob check ([JSONBlob] is null or IsJson(cast(DECOMPRESS([JSONBlob]) as nvarchar(max))) > 0)
                )");
            Mappings.Register(new PersonMap());
        }

        class Person
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Text { get; set; }
        }

        class PersonMap : DocumentMap<Person>
        {
            public PersonMap()
            {
                JsonStorageFormat = JsonStorageFormat.MixedPreferCompressed;
            }
        }
        
        [Test]
        public void CanInsertValidJson()
        {
            using var transaction = Store.BeginTransaction();
            transaction.ExecuteNonQuery("insert into dbo.Person (Id, [JSON]) values ('Persons-1', N'{\"Name\":\"Tom\",\"Text\":\"BBB\"}')");
            transaction.ExecuteNonQuery("insert into dbo.Person (Id, [JSONBlob]) values ('Persons-1', COMPRESS(N'{\"Name\":\"Tom\",\"Text\":\"BBB\"}'))");
            transaction.Commit();
        }
        
        [Test]
        public void CannotInsertInvalidJson()
        {
            using var transaction = Store.BeginTransaction();
            Assert.Throws<Exception>(() => transaction.ExecuteNonQuery("insert into dbo.Person (Id, [JSON]) values ('Persons-1', N'osiv9dsi')"));
        }
        
        [Test]
        public void CannotInsertInvalidJsonBlob()
        {
            using var transaction = Store.BeginTransaction();
            Assert.Throws<Exception>(() => transaction.ExecuteNonQuery("insert into dbo.Person (Id, [JSONBlob]) values ('Persons-1', COMPRESS(N'akjsjaisj'))"));
        }
        
        
    }
}
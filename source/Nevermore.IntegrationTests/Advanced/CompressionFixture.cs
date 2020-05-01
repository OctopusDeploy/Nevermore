using System.Linq;
using FluentAssertions;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    [TestFixture]
    public class CompressionFixture : FixtureWithRelationalStore
    {
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            
            ExecuteSql("create table TestSchema.Person (Id nvarchar(200),[JSONBlob] varbinary(max))");
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
                JsonStorageFormat = JsonStorageFormat.CompressedOnly;
            }
        }

        [Test]
        public void ShouldReadAndWrite()
        {
            using var transaction = Store.BeginTransaction();
            transaction.Insert(new Person() { Name = "Paul ¥ⶀＦ", Text = new string('A', 20000)});

            var loaded = transaction.Load<Person>("Persons-1");

            loaded.Name.Should().Be("Paul ¥ⶀＦ");
            loaded.Text.Should().StartWith("AAAAAAAAA");
        }

        [Test]
        public void ShouldCompress()
        {
            using var transaction = Store.BeginTransaction();
            transaction.Insert(new Person() { Name = "Paul ¥ⶀＦ", Text = new string('A', 20000)});

            var jsonLength = transaction.ExecuteScalar<long>("select max(datalength([JSONBlob])) from TestSchema.Person");
            jsonLength.Should().BeInRange(10, 350);
            var jsonCompressed = transaction.Stream<byte[]>("select [JSONBlob] from TestSchema.Person").First();
            jsonCompressed.Length.Should().BeInRange(10, 350);
        }

        [Test]
        public void CanQueryInSql()
        {
            using var transaction = Store.BeginTransaction();
            transaction.Insert(new Person() { Name = "Paul ¥ⶀＦ", Text = new string('A', 20000)});
            
            var count = transaction.ExecuteScalar<int>("select count(*) from TestSchema.Person where JSON_VALUE(CAST(DECOMPRESS([JSONBlob]) as nvarchar(max)), '$.Name') = N'Paul ¥ⶀＦ'", new CommandParameterValues { { "name", "Paul ¥ⶀＦ"} });
            count.Should().Be(1);
        }

        [Test]
        public void CanReadDataSetDirectlyInSql()
        {
            using var transaction = Store.BeginTransaction();
            transaction.ExecuteNonQuery("insert into TestSchema.Person (Id, [JSONBlob]) values ('Persons-1', COMPRESS(N'{\"Name\":\"Fred ¥ⶀＦ\",\"Text\":\"BBB\"}'))");

            var count = transaction.ExecuteScalar<int>("select count(*) from TestSchema.Person where JSON_VALUE(CAST(DECOMPRESS([JSONBlob]) as nvarchar(max)), '$.Name') = @name", new CommandParameterValues { { "name", "Fred ¥ⶀＦ"} });
            count.Should().Be(1);
            
            var loaded = transaction.Load<Person>("Persons-1");
            loaded.Name.Should().Be("Fred ¥ⶀＦ");
            loaded.Text.Should().Be("BBB");
        }
    }
}
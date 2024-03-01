using System;
using System.Linq;
using FluentAssertions;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using NUnit.Framework;
// ReSharper disable ReturnValueOfPureMethodIsNotUsed

namespace Nevermore.IntegrationTests.Advanced
{
    [TestFixture]
    public class CompressionMigrationFixture : FixtureWithRelationalStore
    {
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            
            NoMonkeyBusiness();
            KeepDataBetweenTests();
            ExecuteSql("create table TestSchema.Person (Id nvarchar(200), [JSON] nvarchar(max), [JSONBlob] varbinary(max))");
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

        [Test, Order(1)]
        public void DataExistsAlreadyAsText()
        {
            using (var transaction = Store.BeginTransaction())
            {
                transaction.ExecuteNonQuery("insert into TestSchema.Person (Id, [JSON]) values ('Persons-1', N'{\"Name\":\"Tom\",\"Text\":\"BBB\"}')");
                transaction.ExecuteNonQuery("insert into TestSchema.Person (Id, [JSON]) values ('Persons-2', N'{\"Name\":\"Ben\",\"Text\":\"BBB\"}')");
                transaction.ExecuteNonQuery("insert into TestSchema.Person (Id, [JSON]) values ('Persons-3', N'{\"Name\":\"Bob\",\"Text\":\"BBB\"}')");
                transaction.Commit();
            }
            
            AssertText("Persons-1");
            AssertText("Persons-2");
            AssertText("Persons-3");
        }
        
        [Test, Order(2)]
        public void InsertingWillWriteCompressed()
        {
            using (var transaction = Store.BeginTransaction())
            {
                transaction.Insert(new Person { Id = "Persons-4", Name = "Bill", Text = "AAA" });
                transaction.Commit();
            }
            AssertCompressed("Persons-4");
        }
        
        [Test, Order(3)]
        public void LoadingWillReadFromText()
        {
            AssertText("Persons-2");
            
            using var transaction = Store.BeginTransaction();
            var person = transaction.Load<Person>("Persons-2");
            person.Name.Should().Be("Ben");
        }
        
        [Test, Order(4)]
        public void UpdatingWillWriteCompressed()
        {
            AssertText("Persons-2");

            using (var transaction = Store.BeginTransaction())
            {
                var person = transaction.Load<Person>("Persons-2");
                person.Text = "ZZZ";
                transaction.Update(person);
                transaction.Commit();
            }
            
            AssertCompressed("Persons-2");
        }
        
        [Test, Order(5)]
        public void LoadingWillReadFromCompressed()
        {
            AssertCompressed("Persons-2");
            
            using var transaction = Store.BeginTransaction();
            var person = transaction.Load<Person>("Persons-2");
            person.Name.Should().Be("Ben");
        }
        
        [Test, Order(6)]
        public void DataCanBeMigratedManuallyInSql()
        {
            AssertText("Persons-3");

            using (var transaction = Store.BeginTransaction())
            {
                transaction.ExecuteNonQuery("update TestSchema.Person set JSONBlob = COMPRESS([JSON]) where Id = 'Persons-3'");
                transaction.ExecuteNonQuery("update TestSchema.Person set [JSON] = null where Id = 'Persons-3'");
                transaction.Commit();
            }
            
            AssertCompressed("Persons-3");

            using (var transaction = Store.BeginTransaction())
            {
                var person = transaction.Load<Person>("Persons-3");
                person.Name.Should().Be("Bob");
            }
        }

        [Test, Order(100)]
        public void BothColumnsAreAlwaysRequired()
        {
            using var transaction = Store.BeginTransaction();
            Assert.Throws<InvalidOperationException>(() => transaction.Stream<Person>("select Id from TestSchema.Person").ToList()).Message.Should().Contain("query does not include either the 'JSON' or 'JSONBlob' column");
            Assert.Throws<InvalidOperationException>(() => transaction.Stream<Person>("select Id,[JSON] from TestSchema.Person").ToList()).Message.Should().Contain("query does not include the 'JSONBlob' column");
            Assert.Throws<InvalidOperationException>(() => transaction.Stream<Person>("select Id,[JSONBlob] from TestSchema.Person").ToList()).Message.Should().Contain("query does not include the 'JSON' column");
        }

        (string Json, byte[] JsonBlob) GetCompression(string id)
        {
            using var transaction = Store.BeginTransaction();
            return transaction.Stream<(string Json, byte[] JsonBlob)>("select [JSON],[JSONBlob] from TestSchema.Person where Id = @id", new CommandParameterValues { { "id", id} }).Single();
        }
        
        void AssertCompressed(string id)
        {
            GetCompression(id).Json.Should().BeNull();
            GetCompression(id).JsonBlob.Should().NotBeNull();
            GetCompression(id).JsonBlob.Length.Should().BeGreaterThan(5);
        }
        
        void AssertText(string id)
        {
            GetCompression(id).JsonBlob.Should().BeNull();
            GetCompression(id).Json.Should().NotBeNull();
            GetCompression(id).Json.Length.Should().BeGreaterThan(5);
        }
    }
}
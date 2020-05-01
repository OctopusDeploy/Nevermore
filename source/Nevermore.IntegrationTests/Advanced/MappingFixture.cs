using System;
using System.Linq;
using FluentAssertions;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Mapping;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class MappingFixture : FixtureWithRelationalStore
    {
        class User
        {
            public string UserId { get; set; }        // Custom ID
            public string FirstName { get; set; }
            public int Age { get; set; }
            public int? FavoriteNumber { get; set; }
            public Education Education { get; set; }

            // Test accessibility
            public string Prop1 { get; set; }
            public string Prop2 { get; } = "Hello"; 
            public string Prop3 { get; private set; }
            public string Prop4 { get; protected set; }
            public string Prop5 { get; private set; }

            public void SetOtherProps(string prop3, string prop4, string prop5)
            {
                Prop3 = prop3;
                Prop4 = prop4;
                Prop5 = prop5;
            }
        }
        
        enum Education { School, HighSchool, College }

        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            
            NoMonkeyBusiness();
            KeepDataBetweenTests();

            ExecuteSql(@"create table TestSchema.[User] (
                UserId nvarchar(200) not null,
                FirstName nvarchar(200) not null,
                Age int not null,
                FavoriteNumber int null,
                Education nvarchar(200),
                Prop1 nvarchar(200),
                Prop2 nvarchar(200),
                Prop3 nvarchar(200),
                Prop4 nvarchar(200),
                Prop5 nvarchar(200),
                [JSON] nvarchar(max)
            )");
        }

        class UserMap : DocumentMap<User>
        {
            public UserMap()
            {
                Id(u => u.UserId);
                Column(u => u.FirstName);
                Column(u => u.Age);
                Column(u => u.FavoriteNumber);
                Column(u => u.Education);
                Column(u => u.Prop1);
                Column(u => u.Prop2);
                Column(u => u.Prop3);
                Column(u => u.Prop4);
                Column(u => u.Prop5).SaveOnly();
            }
        }

        [Test, Order(1)]
        public void ShouldFailIfReadOnlyNotMapped()
        {
            var error = Assert.Throws<InvalidOperationException>(delegate
            {
                Configuration.DocumentMaps.Register(new UserMap());
            });
            error.Message.Should().Contain("'Prop2' is invalid. The property has no setter");
        }

        [Test, Order(2)]
        public void ShouldWorkIfProp2IsSaveOnly()
        {
            var map = ((IDocumentMap)new UserMap()).Build();
            // Pretend the user edited their document map to set it to SaveOnly
            ((IColumnMappingBuilder) map.Columns.Single(c => c.ColumnName == "Prop2")).SaveOnly();
            Configuration.DocumentMaps.Register(map);
        }

        [Test, Order(3)]
        public void ShouldInsert()
        {
            using var transaction = Store.BeginTransaction();
            var user = new User
            {
                UserId = "users-123",
                Age = 18,
                Education = Education.College,
                FavoriteNumber = 37,
                Prop1 = "Prop1",
                FirstName = "Fred"
            };
            
            user.SetOtherProps("Prop3", "Prop4", "Prop5");
            
            transaction.Insert(user);
            transaction.Update(user);
            transaction.Commit();
        }

        [Test, Order(4)]
        public void ShouldLoad()
        {
            using var transaction = Store.BeginTransaction();
            
            var user = transaction.Load<User>("users-123");
            user.Should().NotBeNull();
            user.Age.Should().Be(18);
            user.Education.Should().Be(Education.College);
            user.Prop1.Should().Be("Prop1");
            user.Prop3.Should().Be("Prop3");
            user.Prop4.Should().Be("Prop4");
            user.Prop5.Should().BeNull();  // It's declared SaveOnly, so we won't load it
        }

        [Test, Order(6)]
        public void ShouldDelete()
        {
            using var transaction = Store.BeginTransaction();
            
            transaction.Delete<User>("users-123");
            var user = transaction.Load<User>("users-123");
            user.Should().BeNull();
            transaction.Commit();
        }
    }
}
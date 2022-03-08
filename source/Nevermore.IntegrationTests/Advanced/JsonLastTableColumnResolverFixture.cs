using System;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.TableColumnNameResolvers;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class JsonLastTableColumnResolverFixture : FixtureWithRelationalStore
    {
        public override void OneTimeSetUp()
        {
            base.OneTimeSetUp();
            NoMonkeyBusiness();
            Configuration.TableColumnNameResolver = executor => new JsonLastTableColumnNameResolver(executor);
        }

        [Test]
        public void ShouldSelectAllColumnNamesWithJsonLast()
        {
            using var readTransaction = Store.BeginReadTransaction();
            var selectQuery = readTransaction.Query<Customer>().DebugViewRawQuery();

            selectQuery.Should().Be($"SELECT Id,FirstName,LastName,Nickname,Roles,Balance,IsVip,JSON{Environment.NewLine}FROM [TestSchema].[Customer]{Environment.NewLine}ORDER BY [Id]");
        }
    }
}
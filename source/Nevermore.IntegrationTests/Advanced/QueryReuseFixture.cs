using System;
using System.Threading.Tasks;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class QueryReuseFixture: FixtureWithRelationalStore
    {
        [Test]
        public async Task UseSubqueryToShareQuery()
        {
            using var readTran = await Store.BeginReadTransactionAsync();

            var customers1Query = readTran.Query<Customer>().Where(c => c.FirstName == "Tom");

            if (await customers1Query.Subquery().Where(c => c.LastName == "Jones").AnyAsync())
                throw new InvalidOperationException("Bad stuffs bad");

            var customers  = await customers1Query.Where(c => c.FirstName != "Thomas").ToListAsync();

            customers.Should().NotBeNull();
        }
    }
}
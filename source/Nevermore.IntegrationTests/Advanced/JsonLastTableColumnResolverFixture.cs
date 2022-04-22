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

        [Test]
        public void ShouldNotDeadlockWhenRunningAQuery()
        {
            // We need at least one customer in the database so that we can trigger another operation while
            // we're streaming records.
            using (var writeTransaction = Store.BeginWriteTransaction(retriableOperation: RetriableOperation.None))
            {
                var id = writeTransaction.AllocateId<CustomerId>(typeof(Customer));
                writeTransaction.Insert(new Customer {Id = id});
                writeTransaction.Commit();
            }

            using var readTransaction = Store.BeginReadTransaction();

            var customersQuery = readTransaction.Query<Customer>();
            var customers = customersQuery.Stream();

            foreach (var _ in customers)
            {
                // Now that we already have a lock on our queryable, attempt to start building (not running; just building)
                // another query. This should _not_ provoke a deadlock but if we're hanging here then that means we have one.
                var brandsQuery = readTransaction.Query<Brand>().Subquery().Alias("b");

                // Attempting to enumerate this _should_ genuinely result in a deadlock as we already have a lock on a reader,
                // so all we're really asserting here is that the compiler didn't somehow optimize this call away.
                brandsQuery.Should().NotBeNull();
            }
        }
    }
}
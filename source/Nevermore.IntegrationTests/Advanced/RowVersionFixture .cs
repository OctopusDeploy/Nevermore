using System;
using System.Linq;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class RowVersionFixture : FixtureWithRelationalStore
    {
        [Test]
        public void UpdateChangesRowVersion()
        {
            var customer1 = new Customer {FirstName = "FirstName", LastName = "LastName", Nickname = "NickName"};
            RunInTransaction(transaction => transaction.Insert(customer1));

            var customer2 = RunInTransaction(transaction => transaction.Query<Customer>().Where(c => c.FirstName == "FirstName").ToArray().Single());

            customer2.RowVersion.Should().Equal(customer1.RowVersion);

            customer2.LastName = "LastName2";
            RunInTransaction(transaction => transaction.Update(customer2));

            customer2.RowVersion.Should().NotEqual(customer1.RowVersion);
        }


        [Test]
        public void RefreshesRowVersion()
        {
            var customer = new Customer {FirstName = "FirstName", LastName = "LastName", Nickname = "NickName"};
            RunInTransaction(transaction => transaction.Insert(customer));

            customer.RowVersion.Should().NotBeNull();

            customer = RunInTransaction(transaction => transaction.Load<Customer>(customer.Id));

            customer.LastName = "LastName1";
            RunInTransaction(transaction => transaction.Update(customer));

            customer.LastName = "LastName2";
            RunInTransaction(transaction => transaction.Update(customer));
        }

        [Test]
        public void FailsUpdateWhenDataBecomesStale()
        {
            RunInTransaction(transaction =>
            {
                transaction.Insert( new Customer {FirstName = "FirstName", LastName = "LastName", Nickname = "NickName"});
            });

            var customer1 = RunInTransaction(transaction => transaction.Query<Customer>().Where(c => c.FirstName == "FirstName").ToArray().Single());
            var customer2 = RunInTransaction(transaction => transaction.Query<Customer>().Where(c => c.FirstName == "FirstName").ToArray().Single());

            customer1.LastName = "LastName1";
            RunInTransaction(transaction => transaction.Update(customer1));

            customer2.LastName = "LastName2";
            Action invalidUpdate = () => RunInTransaction(transaction => transaction.Update(customer2));

            invalidUpdate.ShouldThrow<StaleDataException>();
        }

        TResult RunInTransaction<TResult>(Func<IRelationalTransaction, TResult> func)
        {
            using var transaction = Store.BeginTransaction();
            var result =  func(transaction);
            transaction.Commit();

            return result;
        }

        void RunInTransaction(Action<IRelationalTransaction> action)
        {
            RunInTransaction(transaction =>
            {
                action(transaction);
                return string.Empty;
            });
        }
    }
}
using System;
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

            var customer2  = RunInTransaction(transaction => transaction.Load<Customer>(customer1.Id));

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

            var previousRowVersion = customer.RowVersion;
            customer.LastName = "LastName1";
            RunInTransaction(transaction => transaction.Update(customer));

            customer.RowVersion.Should().NotEqual(previousRowVersion);

            previousRowVersion = customer.RowVersion;
            customer.LastName = "LastName2";
            RunInTransaction(transaction => transaction.Update(customer));

            customer.RowVersion.Should().NotEqual(previousRowVersion);
        }

        [Test]
        public void FailsUpdateWhenDataBecomesStale()
        {
            var customer = new Customer {FirstName = "FirstName", LastName = "LastName", Nickname = "NickName"};
            RunInTransaction(transaction => transaction.Insert( customer));

            var customer1 = RunInTransaction(transaction => transaction.Load<Customer>(customer.Id));
            var customer2 = RunInTransaction(transaction => transaction.Load<Customer>(customer.Id));

            customer1.LastName = "LastName1";
            RunInTransaction(transaction => transaction.Update(customer1));

            customer2.LastName = "LastName2";
            Action invalidUpdate = () => RunInTransaction(transaction => transaction.Update(customer2));

            invalidUpdate.ShouldThrow<StaleDataException>();

            var customer3 = RunInTransaction(transaction => transaction.Load<Customer>(customer.Id));
            customer3.LastName.Should().Be(customer1.LastName);
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
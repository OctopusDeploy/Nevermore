using System;
using System.IO;
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
            using var transaction = Store.BeginTransaction();
            transaction.Insert( new Customer {FirstName = "FirstName", LastName = "LastName", Nickname = "NickName"});
            transaction.Commit();

            var customer = transaction.Query<Customer>().Where(c => c.FirstName == "FirstName").ToArray().Single();
            customer.LastName = "LastName2";
            transaction.Update(customer);

            var updatedCustomer = transaction.Query<Customer>().Where(c => c.FirstName == "FirstName").ToArray().Single();

            customer.RowVersion.Should().NotEqual(updatedCustomer.RowVersion);
        }

        [Test]
        public void DetectsPartiallyPopulatedDocuments()
        {
            var customer = new Customer {FirstName = "FirstName", LastName = "LastName", Nickname = "NickName"};
            RunInTransaction(transaction => transaction.Insert(customer));

            customer.FirstName = "FirstName1";
            Action invalidUpdate = () => RunInTransaction(transaction => transaction.Update(customer));
            invalidUpdate.ShouldThrow<InvalidDataException>();
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
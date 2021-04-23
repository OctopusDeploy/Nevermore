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
        public void HandlesMultipleDocuments()
        {
            var insertedCustomer1 = new Customer {FirstName = "FirstName1", LastName = "LastName1", Nickname = "NickName1"};
            var insertedCustomer2 = new Customer {FirstName = "FirstName2", LastName = "LastName2", Nickname = "NickName2"};

            RunInTransaction(transaction => transaction.InsertMany(new []{insertedCustomer1, insertedCustomer2}));

            var customer1 = RunInTransaction(transaction => transaction.Load<Customer>(insertedCustomer1.Id));
            var customer2 = RunInTransaction(transaction => transaction.Load<Customer>(insertedCustomer2.Id));

            customer1.RowVersion.Should().Equal(insertedCustomer1.RowVersion);
            customer2.RowVersion.Should().Equal(insertedCustomer2.RowVersion);
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

        [Test]
        public void DoesNotAffectNonVersionedDocuments()
        {
            var machine = new Machine()
            {
                Name = "1"
            };

            RunInTransaction(t => t.Insert(machine));

            machine.Name = "2";
            RunInTransaction(t => t.Update(machine));

            var machine2 = RunInTransaction(transaction => transaction.Load<Machine>(machine.Id));

            machine2.Name.Should().Be("2");
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
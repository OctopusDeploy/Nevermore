using System;
using System.Threading.Tasks;
using FluentAssertions;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class IdentityIdFixture : FixtureWithRelationalStore
    {
        [Test]
        public void InsertUpdatesDocumentId()
        {
            // ChaosSqlCommand is set to retry some of the reads which breaks row versioning code because INSERTS/UPDATES,
            // even executed via SqlReader, must not be retired.
            NoMonkeyBusiness();

            var document1 = new DocumentWithIdentityId { Name = "Name" };

            document1.Id.Should().Be(0);

            RunInTransaction(transaction => transaction.Insert(document1), $"{nameof(IdentityIdFixture)}.{nameof(InsertUpdatesDocumentId)}");

            document1.Id.Should().NotBe(0);
        }

        [Test]
        public void InsertManyUpdatesDocumentIds()
        {
            // ChaosSqlCommand is set to retry some of the reads which breaks row versioning code because INSERTS/UPDATES,
            // even executed via SqlReader, must not be retired.
            NoMonkeyBusiness();

            var document1 = new DocumentWithIdentityId { Name = "Name" };
            var document2 = new DocumentWithIdentityId { Name = "Name" };

            document1.Id.Should().Be(0);
            document2.Id.Should().Be(0);

            RunInTransaction(transaction => transaction.InsertMany(new[]
            {
                document1,
                document2
            }), $"{nameof(IdentityIdFixture)}.{nameof(InsertManyUpdatesDocumentIds)}");

            document1.Id.Should().NotBe(0);
            document2.Id.Should().NotBe(0);
            document1.Id.Should().NotBe(document2.Id);
        }

        [Test]
        public async Task InsertAsyncUpdatesDocumentIdAndRowVersion()
        {
            // ChaosSqlCommand is set to retry some of the reads which breaks row versioning code because INSERTS/UPDATES,
            // even executed via SqlReader, must not be retired.
            NoMonkeyBusiness();

            var document1 = new DocumentWithIdentityId { Name = "Name" };

            document1.Id.Should().Be(0);

            await RunInTransactionAsync(async transaction => await transaction.InsertAsync(document1), $"{nameof(IdentityIdFixture)}.{nameof(InsertAsyncUpdatesDocumentIdAndRowVersion)}");

            document1.Id.Should().NotBe(0);
        }


        [Test]
        public async Task InsertAsyncUpdatesDocumentId()
        {
            // ChaosSqlCommand is set to retry some of the reads which breaks row versioning code because INSERTS/UPDATES,
            // even executed via SqlReader, must not be retired.
            NoMonkeyBusiness();

            var document1 = new DocumentWithIdentityIdAndRowVersion { Name = "Name" };

            document1.Id.Should().Be(0);

            await RunInTransactionAsync(async transaction => await transaction.InsertAsync(document1), $"{nameof(IdentityIdFixture)}.{nameof(InsertAsyncUpdatesDocumentId)}1");

            document1.Id.Should().NotBe(0);

            var document2 = RunInTransaction(transaction => transaction.Load<DocumentWithIdentityIdAndRowVersion>(document1.Id), $"{nameof(IdentityIdFixture)}.{nameof(InsertAsyncUpdatesDocumentId)}2");

            document2.RowVersion.Should().Equal(document1.RowVersion);

            document2.Name = "Name2";
            RunInTransaction(transaction => transaction.Update(document2), $"{nameof(IdentityIdFixture)}.{nameof(InsertAsyncUpdatesDocumentId)}3");

            document2.RowVersion.Should().NotEqual(document1.RowVersion);
        }

        TResult RunInTransaction<TResult>(Func<IRelationalTransaction, TResult> func, string name)
        {
            using var transaction = Store.BeginTransaction(name: name);
            var result = func(transaction);
            transaction.Commit();

            return result;
        }

        void RunInTransaction(Action<IRelationalTransaction> action, string name)
        {
            RunInTransaction(transaction =>
            {
                action(transaction);
                return string.Empty;
            }, name);
        }

        async Task RunInTransactionAsync<TResult>(Func<IRelationalTransaction, Task<TResult>> func, string name)
        {
            using var transaction = Store.BeginTransaction(name: name);
            var result = await func(transaction);
            await transaction.CommitAsync();
        }

        async Task RunInTransactionAsync(Func<IRelationalTransaction, Task> action, string name)
        {
            await RunInTransactionAsync(async transaction =>
            {
                await action(transaction);
                return true;
            }, name);
        }
    }
}
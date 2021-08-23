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
            // ChaosSqlCommand is set to retry some of the reads which breaks row versioning code because INSERTS/UPDATES,
            // even executed via SqlReader, must not be retired.
            NoMonkeyBusiness();

            var document1 = new DocumentWithRowVersion() {Name = "Name"};
            RunInTransaction(transaction => transaction.Insert(document1));

            var document2  = RunInTransaction(transaction => transaction.Load<DocumentWithRowVersion>(document1.Id));

            document2.RowVersion.Should().Equal(document1.RowVersion);

            document2.Name = "Name2";
            RunInTransaction(transaction => transaction.Update(document2));

            document2.RowVersion.Should().NotEqual(document1.RowVersion);
        }

        [Test]
        public void HandlesMultipleDocuments()
        {
            NoMonkeyBusiness();

            var insertedDocument1 = new DocumentWithRowVersion {Name = "Name1"};
            var insertedDocument2 = new DocumentWithRowVersion {Name = "Name2"};

            RunInTransaction(transaction => transaction.InsertMany(new []{insertedDocument1, insertedDocument2}));

            var document1 = RunInTransaction(transaction => transaction.Load<DocumentWithRowVersion>(insertedDocument1.Id));
            var document2 = RunInTransaction(transaction => transaction.Load<DocumentWithRowVersion>(insertedDocument2.Id));

            document1.RowVersion.Should().Equal(insertedDocument1.RowVersion);
            document2.RowVersion.Should().Equal(insertedDocument2.RowVersion);
        }

        [Test]
        public void RefreshesRowVersion()
        {
            NoMonkeyBusiness();

            var document = new DocumentWithRowVersion { Name = "Name"};
            RunInTransaction(transaction => transaction.Insert(document));

            document.RowVersion.Should().NotBeNull();

            document = RunInTransaction(transaction => transaction.Load<DocumentWithRowVersion>(document.Id));

            var previousRowVersion = document.RowVersion;
            document.Name = "Name1";
            RunInTransaction(transaction => transaction.Update(document));

            document.RowVersion.Should().NotEqual(previousRowVersion);

            previousRowVersion = document.RowVersion;
            document.Name = "Name2";
            RunInTransaction(transaction => transaction.Update(document));

            document.RowVersion.Should().NotEqual(previousRowVersion);
        }

        [Test]
        public void FailsUpdateWhenDataBecomesStale()
        {
            NoMonkeyBusiness();

            var document = new DocumentWithRowVersion {Name = "Name"};
            RunInTransaction(transaction => transaction.Insert( document));

            var document1 = RunInTransaction(transaction => transaction.Load<DocumentWithRowVersion>(document.Id));
            var document2 = RunInTransaction(transaction => transaction.Load<DocumentWithRowVersion>(document.Id));

            document1.Name = "Name1";
            RunInTransaction(transaction => transaction.Update(document1));

            document2.Name = "Name2";
            Action invalidUpdate = () => RunInTransaction(transaction => transaction.Update(document2));

            invalidUpdate.ShouldThrow<StaleDataException>();

            var document3 = RunInTransaction(transaction => transaction.Load<DocumentWithRowVersion>(document.Id));
            document3.Name.Should().Be(document1.Name);
        }

        [Test]
        public void HandlesUniqueConstraint()
        {
            NoMonkeyBusiness();

            var document1 = new DocumentWithRowVersion {Name = "Name"};
            RunInTransaction(transaction => transaction.Insert( document1));

            var document2 = new DocumentWithRowVersion {Name = "Name"};
            Action invalidUpdate = () => RunInTransaction(transaction => transaction.Insert(document2));

            invalidUpdate.ShouldThrow<UniqueConstraintViolationException>();
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

        [Test]
        public void ErrorMessageIncludesMappedTypeName()
        {
            NoMonkeyBusiness();

            var document = new DocumentWithIdentityIdAndRowVersion();
            RunInTransaction(t => t.Insert(document));

            var document1 = RunInTransaction(transaction => transaction.LoadRequired<DocumentWithIdentityIdAndRowVersion>(document.Id));
            var document2 = RunInTransaction(transaction => transaction.LoadRequired<DocumentWithIdentityIdAndRowVersion>(document.Id));

            document1.Name = "Name1";
            RunInTransaction(transaction => transaction.Update(document1));

            document2.Name = "Name2";
            Action invalidUpdate = () => RunInTransaction(transaction => transaction.Update<IId>(document2));

            invalidUpdate.ShouldThrow<StaleDataException>()
                .WithMessage("Modification failed for 'DocumentWithIdentityIdAndRowVersion' document with '1' Id because submitted data was out of date. Refresh the document and try again.");
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
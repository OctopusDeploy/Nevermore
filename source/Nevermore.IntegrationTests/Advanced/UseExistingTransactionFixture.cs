using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Nevermore.Advanced;
using Nevermore.Advanced.Queryable;
using Nevermore.IntegrationTests.Model;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced;

public class UseExistingTransactionFixture : FixtureWithRelationalStore
{
    CancellationToken cancellationToken;

    public override void SetUp()
    {
        base.SetUp();
        cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;
    }

    [Test]
    public async Task SharingSameSqlTransaction_BetweenMultipleNevermoreTransactions_WorksAsExpected()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        await sqlConnection.OpenAsync(cancellationToken);

        await using (var sqlTransaction = sqlConnection.BeginTransaction(IsolationLevel.ReadCommitted))
        {
            using (var nevermoreTransaction1 = Store.CreateWriteTransactionFromExistingConnectionAndTransaction(sqlConnection, sqlTransaction))
            {
                var alice = new Customer { FirstName = "Alice", LastName = "Apple" };
                nevermoreTransaction1.Insert(alice);
            }

            using (var nevermoreTransaction2 = Store.CreateWriteTransactionFromExistingConnectionAndTransaction(sqlConnection, sqlTransaction))
            {
                var customers = await nevermoreTransaction2.Queryable<Customer>().ToListAsync(cancellationToken);

                // We should be able to see Alice within this SQL transaction, even though it was inserted via a
                // different Nevermore transaction and was never committed
                customers.Should().BeEquivalentTo(new object[]
                {
                    new { FirstName = "Alice", LastName = "Apple" },
                });

                var bob = new Customer { FirstName = "Bob", LastName = "Banana" };
                nevermoreTransaction2.Insert(bob);
            }

            await sqlTransaction.CommitAsync(cancellationToken);
        }

        // Confirm the SQL transaction commit above committed the inserts from both Nevermore transactions
        using var readTransaction = await Store.BeginReadTransactionAsync(cancellationToken: cancellationToken);
        (await readTransaction.Queryable<Customer>().ToListAsync(cancellationToken))
            .Should().BeEquivalentTo(new object[]
            {
                new { FirstName = "Alice", LastName = "Apple" },
                new { FirstName = "Bob", LastName = "Banana" },
            });
    }

    [Test]
    public async Task OpeningReadTransaction_FromExistingConnectionAndTransaction_Throws()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        await sqlConnection.OpenAsync(cancellationToken);
        await using var sqlTransaction = sqlConnection.BeginTransaction();

        var readTransaction = (ReadTransaction) Store.CreateReadTransactionFromExistingConnectionAndTransaction(sqlConnection, sqlTransaction);

        using (new AssertionScope())
        {
            const string expectedMessage = "An existing connection and transaction were provided, they should have been opened externally";

            await new Func<Task>(async () => await readTransaction.OpenAsync(cancellationToken))
                .Should()
                .ThrowExactlyAsync<InvalidOperationException>()
                .WithMessage(expectedMessage);

            new Action(() => readTransaction.Open())
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage(expectedMessage);

            await new Func<Task>(async () => await readTransaction.OpenAsync(IsolationLevel.ReadCommitted, cancellationToken))
                .Should()
                .ThrowExactlyAsync<InvalidOperationException>()
                .WithMessage(expectedMessage);

            new Action(() => readTransaction.Open(IsolationLevel.ReadCommitted))
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage(expectedMessage);
        }
    }

    [Test]
    public async Task OpeningWriteTransaction_FromExistingConnectionAndTransaction_Throws()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        await sqlConnection.OpenAsync(cancellationToken);
        await using var sqlTransaction = sqlConnection.BeginTransaction();

        var writeTransaction = (WriteTransaction) Store.CreateWriteTransactionFromExistingConnectionAndTransaction(sqlConnection, sqlTransaction);

        using (new AssertionScope())
        {
            const string expectedMessage = "An existing connection and transaction were provided, they should have been opened externally";

            await new Func<Task>(async () => await writeTransaction.OpenAsync(cancellationToken))
                .Should()
                .ThrowExactlyAsync<InvalidOperationException>()
                .WithMessage(expectedMessage);

            new Action(() => writeTransaction.Open())
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage(expectedMessage);

            await new Func<Task>(async () => await writeTransaction.OpenAsync(IsolationLevel.ReadCommitted, cancellationToken))
                .Should()
                .ThrowExactlyAsync<InvalidOperationException>()
                .WithMessage(expectedMessage);

            new Action(() => writeTransaction.Open(IsolationLevel.ReadCommitted))
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage(expectedMessage);
        }
    }

    [Test]
    public async Task CommittingNonOwnedTransaction_Throws()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        await sqlConnection.OpenAsync(cancellationToken);
        await using var sqlTransaction = sqlConnection.BeginTransaction();

        var nonOwnedTransaction = Store.CreateWriteTransactionFromExistingConnectionAndTransaction(sqlConnection, sqlTransaction);

        using (new AssertionScope())
        {
            const string expectedMessage = $"{nameof(WriteTransaction)} cannot commit a transaction it does not own";

            await new Func<Task>(async () => await nonOwnedTransaction.CommitAsync(cancellationToken))
                .Should()
                .ThrowExactlyAsync<InvalidOperationException>()
                .WithMessage(expectedMessage);

            new Action(() => nonOwnedTransaction.Commit())
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage(expectedMessage);
        }
    }

    [Test]
    public async Task DisposingNonOwnedTransaction_DoesNotDisposeConnectionOrTransaction()
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        await sqlConnection.OpenAsync(cancellationToken);
        await using var sqlTransaction = sqlConnection.BeginTransaction();

        var nonOwnedTransaction = Store.CreateWriteTransactionFromExistingConnectionAndTransaction(sqlConnection, sqlTransaction);
        nonOwnedTransaction.Dispose();

        using (new AssertionScope())
        {
            // ReSharper disable once AccessToDisposedClosure
            new Action(() => sqlTransaction.Commit()).Should().NotThrow();
            sqlConnection.State.Should().Be(ConnectionState.Open);
        }
    }
}
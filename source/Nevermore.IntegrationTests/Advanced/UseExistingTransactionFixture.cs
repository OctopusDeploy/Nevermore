using System;
using System.Data;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Execution;
using Microsoft.Data.SqlClient;
using Nevermore.Advanced;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced;

#pragma warning disable NV0008 // Nevermore transaction is never disposed - false positives

public class UseExistingTransactionFixture : FixtureWithRelationalStore
{
    CancellationToken cancellationToken;

    public override void SetUp()
    {
        base.SetUp();
        cancellationToken = new CancellationTokenSource(TimeSpan.FromMinutes(1)).Token;
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

            await ((Func<Task>)(async () => await readTransaction.OpenAsync(cancellationToken)))
                .Should()
                .ThrowExactlyAsync<InvalidOperationException>()
                .WithMessage(expectedMessage);

            ((Action)(() => readTransaction.Open()))
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage(expectedMessage);

            await ((Func<Task>)(async () => await readTransaction.OpenAsync(IsolationLevel.ReadCommitted, cancellationToken)))
                .Should()
                .ThrowExactlyAsync<InvalidOperationException>()
                .WithMessage(expectedMessage);

            ((Action)(() => readTransaction.Open(IsolationLevel.ReadCommitted)))
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

            await ((Func<Task>)(async () => await writeTransaction.OpenAsync(cancellationToken)))
                .Should()
                .ThrowExactlyAsync<InvalidOperationException>()
                .WithMessage(expectedMessage);

            ((Action)(() => writeTransaction.Open()))
                .Should()
                .ThrowExactly<InvalidOperationException>()
                .WithMessage(expectedMessage);

            await ((Func<Task>)(async () => await writeTransaction.OpenAsync(IsolationLevel.ReadCommitted, cancellationToken)))
                .Should()
                .ThrowExactlyAsync<InvalidOperationException>()
                .WithMessage(expectedMessage);

            ((Action)(() => writeTransaction.Open(IsolationLevel.ReadCommitted)))
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

            await ((Func<Task>)(async () => await nonOwnedTransaction.CommitAsync(cancellationToken)))
                .Should()
                .ThrowExactlyAsync<InvalidOperationException>()
                .WithMessage(expectedMessage);

            ((Action)(() => nonOwnedTransaction.Commit()))
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
            ((Action)(() => sqlTransaction.Commit())).Should().NotThrow();
            sqlConnection.State.Should().Be(ConnectionState.Open);
        }
    }
}
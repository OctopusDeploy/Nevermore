using System;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
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

    class TestConnectionAndTransaction : IConnectionAndTransaction
    {
        public TestConnectionAndTransaction(DbConnection connection, DbTransaction transaction)
        {
            Connection = connection;
            Transaction = transaction;
        }

        public DbConnection Connection { get; }

        public DbTransaction Transaction { get; }

        public int CommitTransactionCalled { get; private set; }
        public int DisposeCalled { get; private set; }

        public void CommitTransaction()
        {
            ++CommitTransactionCalled;
            Transaction.Commit();
        }

        public async Task CommitTransactionAsync(CancellationToken cancellationToken)
        {
            ++CommitTransactionCalled;
            await Transaction.CommitAsync(cancellationToken);
        }

        public void Dispose()
        {
            ++DisposeCalled;
            Transaction.Dispose();
            Connection.Dispose();
        }
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public async Task OpeningReadTransaction_FromExistingTransaction_Throws(bool takeOwnershipOfExistingConnectionAndTransaction)
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        await sqlConnection.OpenAsync(cancellationToken);
        await using var sqlTransaction = sqlConnection.BeginTransaction();

        var readTransaction = (ReadTransaction) Store.CreateReadTransactionFromExistingConnectionAndTransaction(
            new TestConnectionAndTransaction(sqlConnection, sqlTransaction),
            takeOwnershipOfExistingConnectionAndTransaction);

        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await readTransaction.OpenAsync(cancellationToken));
            Assert.Throws<InvalidOperationException>(() => readTransaction.Open());
        });
    }

    [Test]
    [TestCase(false)]
    [TestCase(true)]
    public async Task OpeningWriteTransaction_FromExistingTransaction_Throws(bool takeOwnershipOfExistingConnectionAndTransaction)
    {
        await using var sqlConnection = new SqlConnection(ConnectionString);
        await sqlConnection.OpenAsync(cancellationToken);
        await using var sqlTransaction = sqlConnection.BeginTransaction();

        var writeTransaction = (WriteTransaction) Store.CreateWriteTransactionFromExistingConnectionAndTransaction(
            new TestConnectionAndTransaction(sqlConnection, sqlTransaction),
            takeOwnershipOfExistingConnectionAndTransaction);

        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await writeTransaction.OpenAsync(cancellationToken));
            Assert.Throws<InvalidOperationException>(() => writeTransaction.Open());
        });
    }

    [Test]
    public async Task CommittingNonOwnedTransaction_Throws()
    {
        var sqlConnection = new SqlConnection(ConnectionString);
        await sqlConnection.OpenAsync(cancellationToken);
        var sqlTransaction = sqlConnection.BeginTransaction();
        using var connectionAndTransaction = new TestConnectionAndTransaction(sqlConnection, sqlTransaction);

        var nonOwnedTransaction = Store.CreateWriteTransactionFromExistingConnectionAndTransaction(
            connectionAndTransaction, false);

        Assert.Multiple(() =>
        {
            Assert.ThrowsAsync<InvalidOperationException>(async () => await nonOwnedTransaction.CommitAsync(cancellationToken));
            Assert.Throws<InvalidOperationException>(() => nonOwnedTransaction.Commit());
        });
    }

    [Test]
    public async Task CommittingOwnedTransaction_Succeeds()
    {
        var sqlConnection = new SqlConnection(ConnectionString);
        await sqlConnection.OpenAsync(cancellationToken);
        var sqlTransaction = sqlConnection.BeginTransaction();
        var connectionAndTransaction = new TestConnectionAndTransaction(sqlConnection, sqlTransaction);

        using var ownedTransaction = Store.CreateWriteTransactionFromExistingConnectionAndTransaction(
            connectionAndTransaction, true);

        await ownedTransaction.CommitAsync(cancellationToken);
        Assert.AreEqual(1, connectionAndTransaction.CommitTransactionCalled);
    }

    [Test]
    public async Task DisposingNonOwnedTransaction_DoesNotDisposeConnectionOrTransaction()
    {
        var sqlConnection = new SqlConnection(ConnectionString);
        await sqlConnection.OpenAsync(cancellationToken);
        var sqlTransaction = sqlConnection.BeginTransaction();
        using var connectionAndTransaction = new TestConnectionAndTransaction(sqlConnection, sqlTransaction);

        var nonOwnedTransaction = Store.CreateWriteTransactionFromExistingConnectionAndTransaction(
            connectionAndTransaction, false);
        nonOwnedTransaction.Dispose();

        Assert.Multiple(() =>
        {
            Assert.AreEqual(0, connectionAndTransaction.DisposeCalled);
            Assert.DoesNotThrow(() => sqlTransaction.Commit());
            Assert.AreEqual(ConnectionState.Open, sqlConnection.State);
        });
    }

    [Test]
    public async Task DisposingOwnedTransaction_CallsDisposeOnGivenConnectionOrTransaction()
    {
        var sqlConnection = new SqlConnection(ConnectionString);
        await sqlConnection.OpenAsync(cancellationToken);
        var sqlTransaction = sqlConnection.BeginTransaction();
        var connectionAndTransaction = new TestConnectionAndTransaction(sqlConnection, sqlTransaction);

        var nonOwnedTransaction = Store.CreateWriteTransactionFromExistingConnectionAndTransaction(
            connectionAndTransaction, true);
        nonOwnedTransaction.Dispose();

        Assert.AreEqual(1, connectionAndTransaction.DisposeCalled);
    }
}
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Nevermore.Advanced;
using Nevermore.Transient;
using NUnit.Framework;

namespace Nevermore.Tests.RelationalStore
{
    public class ReadTransactionFixture
    {
        static readonly string FakeConnectionString = "Server=NotARealHost;Database=FakeConnectionString";

        RelationalTransactionRegistry registry;
        readonly List<FakeSqlConnection> createdConnections = new();

        DbConnection ConnectionFactory(string s)
        {
            var c = new FakeSqlConnection { ConnectionString = s };
            createdConnections.Add(c);
            return c;
        }

        [SetUp]
        public void SetUp() // NUnit doesn't create a new instance of the fixture for each test
        {
            registry = new(new SqlConnectionStringBuilder(FakeConnectionString));
            createdConnections.Clear();
        }

        [Test]
        public void OpenCanOpenConnection()
        {
            var c = new ReadTransaction(null!, registry, RetriableOperation.Select, new RelationalStoreConfiguration(FakeConnectionString), ConnectionFactory, null, false);

            c.Open();

            // this is not ideal; it'd be better to do something that proves the connection is open,
            // rather than poking around in the mock object, but it'll do for now
            createdConnections.Should().HaveCount(1);
            createdConnections[0].State.Should().Be(ConnectionState.Open);
        }

        [Test]
        public async Task OpenAsyncCanOpenConnection()
        {
            var c = new ReadTransaction(null!, registry, RetriableOperation.Select, new RelationalStoreConfiguration(FakeConnectionString), ConnectionFactory, null, false);

            await c.OpenAsync();

            createdConnections.Should().HaveCount(1);
            createdConnections[0].State.Should().Be(ConnectionState.Open);
        }

        [Test]
        public void OpenWithIsolationCanOpenConnection()
        {
            var c = new ReadTransaction(null!, registry, RetriableOperation.Select, new RelationalStoreConfiguration(FakeConnectionString), ConnectionFactory, null, false);

            c.Open(IsolationLevel.ReadCommitted);

            createdConnections.Should().HaveCount(1);
            createdConnections[0].State.Should().Be(ConnectionState.Open);
        }

        [Test]
        public async Task OpenAsyncWithIsolationCanOpenConnection()
        {
            var c = new ReadTransaction(null!, registry, RetriableOperation.Select, new RelationalStoreConfiguration(FakeConnectionString), ConnectionFactory, null, false);

            await c.OpenAsync(IsolationLevel.ReadCommitted);

            createdConnections.Should().HaveCount(1);
            createdConnections[0].State.Should().Be(ConnectionState.Open);
        }

        class FakeSqlConnectionWhichThrowsOnFirstOpen : FakeSqlConnection
        {
            public int OpenCount { get; set; }

            // note we don't need to override OpenAsync because the base DbConnection class OpenAsync just calls the Sync one here
            public override void Open()
            {
                // See SqlDatabaseTransientErrorDetectionStrategy.IsTransient
                if (OpenCount++ == 0) throw new TimeoutException("transient connection failure");
                base.Open();
            }
        }

        [Test]
        public void OpenWillRetryATransientFailure()
        {
            DbConnection ConnectionFactoryTransientFailure(string s)
            {
                var c = new FakeSqlConnectionWhichThrowsOnFirstOpen { ConnectionString = s };
                createdConnections.Add(c);
                return c;
            }

            var c = new ReadTransaction(null!, registry, RetriableOperation.Select, new RelationalStoreConfiguration(FakeConnectionString), ConnectionFactoryTransientFailure, null, false);

            c.Open();

            createdConnections.Should().HaveCount(1);
            createdConnections[0].State.Should().Be(ConnectionState.Open);

            ((FakeSqlConnectionWhichThrowsOnFirstOpen)createdConnections[0]).OpenCount.Should().Be(2); // sanity check that our mock got hit. Not important for the test but just to ensure we wired it up properly
        }

        [Test]
        public async Task OpenAsyncWillRetryATransientFailure()
        {
            DbConnection ConnectionFactoryTransientFailure(string s)
            {
                var c = new FakeSqlConnectionWhichThrowsOnFirstOpen { ConnectionString = s };
                createdConnections.Add(c);
                return c;
            }

            var c = new ReadTransaction(null!, registry, RetriableOperation.Select, new RelationalStoreConfiguration(FakeConnectionString), ConnectionFactoryTransientFailure, null, false);

            await c.OpenAsync();

            createdConnections.Should().HaveCount(1);
            createdConnections[0].State.Should().Be(ConnectionState.Open);

            ((FakeSqlConnectionWhichThrowsOnFirstOpen)createdConnections[0]).OpenCount.Should().Be(2); // sanity check that our mock got hit. Not important for the test but just to ensure we wired it up properly
        }

        // Open with an isolation level also creates a SqlTransaction with the given isolation level
        [Test]
        public void OpenWithIsolationWillRetryATransientFailure()
        {
            DbConnection ConnectionFactoryTransientFailure(string s)
            {
                var c = new FakeSqlConnectionWhichThrowsOnFirstOpen { ConnectionString = s };
                createdConnections.Add(c);
                return c;
            }

            var c = new ReadTransaction(null!, registry, RetriableOperation.Select, new RelationalStoreConfiguration(FakeConnectionString), ConnectionFactoryTransientFailure, null, false);

            c.Open(IsolationLevel.ReadCommitted);

            createdConnections.Should().HaveCount(1);
            createdConnections[0].State.Should().Be(ConnectionState.Open);

            ((FakeSqlConnectionWhichThrowsOnFirstOpen)createdConnections[0]).OpenCount.Should().Be(2); // sanity check that our mock got hit. Not important for the test but just to ensure we wired it up properly
        }

        class FakeSqlConnectionWhichThrowsOnFirstTransaction : FakeSqlConnection
        {
            public int TransactionCount { get; set; }

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            {
                // NOTE: This mimics the behaviour of a real connection, where an error kills the whole transaction and
                // it needs to be re-opened to recover
                if (TransactionCount++ == 0)
                {
                    connectionState = ConnectionState.Closed;
                    throw new TimeoutException("transient exception in BeginDbTransaction");
                }
                return base.BeginDbTransaction(isolationLevel);
            }

            public override DbTransaction BeginTransaction(IsolationLevel iso, string transactionName)
            {
                if (TransactionCount++ == 0)
                {
                    connectionState = ConnectionState.Closed;
                    throw new TimeoutException("transient exception in BeginDbTransaction");
                }
                return base.BeginTransaction(iso, transactionName);
            }
        }

        [Test]
        public void OpenWithIsolationWillRetryATransientFailureFromTransaction()
        {
            DbConnection ConnectionFactoryTransientFailure(string s)
            {
                var c = new FakeSqlConnectionWhichThrowsOnFirstTransaction { ConnectionString = s };
                createdConnections.Add(c);
                return c;
            }

            var c = new ReadTransaction(null!, registry, RetriableOperation.Select, new RelationalStoreConfiguration(FakeConnectionString), ConnectionFactoryTransientFailure, null, false);

            c.Open(IsolationLevel.ReadCommitted);

            createdConnections.Should().HaveCount(1);
            createdConnections[0].State.Should().Be(ConnectionState.Open);

            ((FakeSqlConnectionWhichThrowsOnFirstTransaction)createdConnections[0]).TransactionCount.Should().Be(2); // sanity check that our mock got hit. Not important for the test but just to ensure we wired it up properly
        }

        [Test]
        public async Task OpenAsyncWithIsolationWillRetryATransientFailureFromTransaction()
        {
            DbConnection ConnectionFactoryTransientFailure(string s)
            {
                var c = new FakeSqlConnectionWhichThrowsOnFirstTransaction { ConnectionString = s };
                createdConnections.Add(c);
                return c;
            }

            var c = new ReadTransaction(null!, registry, RetriableOperation.Select, new RelationalStoreConfiguration(FakeConnectionString), ConnectionFactoryTransientFailure, null, false);

            await c.OpenAsync(IsolationLevel.ReadCommitted);

            createdConnections.Should().HaveCount(1);
            createdConnections[0].State.Should().Be(ConnectionState.Open);

            ((FakeSqlConnectionWhichThrowsOnFirstTransaction)createdConnections[0]).TransactionCount.Should().Be(2); // sanity check that our mock got hit. Not important for the test but just to ensure we wired it up properly
        }

        class FakeSqlTransaction : DbTransaction
        {
            public FakeSqlTransaction(FakeSqlConnection connection, IsolationLevel isolationLevel, string transactionName)
            {
                DbConnection = connection;
                IsolationLevel = isolationLevel;
                Name = transactionName;
            }

            public override void Commit() => throw new NotImplementedException();

            public override void Rollback() => throw new NotImplementedException();

            protected override DbConnection DbConnection { get; }
            public override IsolationLevel IsolationLevel { get; }
            public string Name { get; }
        }

        class FakeSqlConnection : DbConnection
        {
            protected ConnectionState connectionState = ConnectionState.Closed;

            public readonly List<FakeSqlTransaction> CreatedTransactions = new();

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            {
                if(connectionState is not ConnectionState.Open) throw new InvalidOperationException($"Can't call BeginDbTransaction when state is {connectionState}");
                var tx = new FakeSqlTransaction(this, isolationLevel, "");
                CreatedTransactions.Add(tx);
                return tx;
            }

            public override void ChangeDatabase(string databaseName) => throw new NotImplementedException();

            public override void Close()
            {
                connectionState = ConnectionState.Closed;
            }

            public override void Open()
            {
                if (connectionState is not (ConnectionState.Closed or ConnectionState.Broken)) throw new InvalidOperationException($"Can't call Open when state is {connectionState}");
                connectionState = ConnectionState.Open; // do we care about the transitory Connecting state?
            }

            public virtual DbTransaction BeginTransaction(IsolationLevel iso, string transactionName)
            {
                if(connectionState is not ConnectionState.Open) throw new InvalidOperationException($"Can't call BeginTransaction when state is {connectionState}");
                var tx = new FakeSqlTransaction(this, iso, transactionName);
                CreatedTransactions.Add(tx);
                return tx;
            }

            public override string ConnectionString { get; set; }
            public override string Database { get; }
            public override ConnectionState State => connectionState;
            public override string DataSource { get; }
            public override string ServerVersion { get; }

            protected override DbCommand CreateDbCommand() => throw new NotImplementedException();
        }
    }
}
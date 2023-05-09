using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Nevermore.Advanced;
using Nevermore.Diagnostics;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.RelationalTransaction;

public class TransactionTimerFixture : FixtureWithRelationalStore
{
    class MockTransactionLogger : ITransactionLogger
    {
        public List<(long duration, string transactionName)> Entries { get; } = new();
        
        public void Write(long duration, string transactionName)
        {
            Entries.Add((duration, transactionName));
        }
    }
    
    [Test]
    public void Duration()
    {
        var mockTransactionLogger = new MockTransactionLogger();
        Store.Configuration.TransactionLogger = mockTransactionLogger;

        using (var t = Store.BeginTransaction(name: "timed transaction") as ReadTransaction)
        {
            if (t is null)
            {
                Assert.Fail($"Transaction is not {nameof(ReadTransaction)}");
            }
        
            t.ExecuteScalar<object>("WAITFOR DELAY '00:00:10'");
        }

        mockTransactionLogger.Entries.Should().ContainSingle();
        mockTransactionLogger.Entries.Single().duration.Should().BeGreaterThan(10_000);
        mockTransactionLogger.Entries.Single().transactionName.Should().Be("timed transaction");
    }
}
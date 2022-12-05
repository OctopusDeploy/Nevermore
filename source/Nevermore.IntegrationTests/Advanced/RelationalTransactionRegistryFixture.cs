using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Data.SqlClient;
using Nevermore.IntegrationTests.SetUp;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class RelationalTransactionRegistryFixture : FixtureWithDatabase
    {
        public static IEnumerable<TestCaseData> TransactionsAreRemovedFromTheRegistryWhenThePoolIsExhaustedSource()
        {

            TestCaseData CreateAsyncCase(string testName, Func<IRelationalStore, string, Task<IDisposable>> beginTransaction)
                => new TestCaseData(beginTransaction) {TestName = testName};

            TestCaseData CreateCase(string testName, Func<IRelationalStore, string, IDisposable> beginTransaction)
                => CreateAsyncCase(testName, (store, name) => Task.Run(() => beginTransaction(store, name)));

            yield return CreateCase("BeginTransaction", (store, name) => store.BeginTransaction(name: name));
            yield return CreateCase("BeginReadTransaction IsolationLevel overload", (store, name) => store.BeginReadTransaction(IsolationLevel.ReadUncommitted, name: name));
            yield return CreateCase("BeginWriteTransaction", (store, name) => store.BeginWriteTransaction(name: name));
            yield return CreateAsyncCase("BeginReadTransactionAsync IsolationLevel overload", async (store, name) => await store.BeginReadTransactionAsync(IsolationLevel.ReadUncommitted, name: name));
            yield return CreateAsyncCase("BeginWriteTransaction", async (store, name) => await store.BeginWriteTransactionAsync(name: name));

        }

        [TestCaseSource(nameof(TransactionsAreRemovedFromTheRegistryWhenThePoolIsExhaustedSource))]
        public async Task ExecuteTransactionsAreRemovedFromTheRegistryWhenThePoolIsExhausted(Func<RelationalStore, string, Task<IDisposable>> beingTransaction)
        {
            var csBuilder = new SqlConnectionStringBuilder(ConnectionString)
            {
                ConnectTimeout = 1,
                MaxPoolSize = 2
            };

            var store = new RelationalStore(new RelationalStoreConfiguration(csBuilder.ConnectionString));


            async Task<IDisposable> TryOpenConnection(int seq)
            {
                try
                {
                    var trn = await beingTransaction(store, "Transaction " + seq);
                    Console.WriteLine($"Transaction {seq}: Opened");
                    return trn;
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Transaction {seq}: Failed {e.Message}");
                    return null;
                }
            }

            var transactions = await Task.WhenAll(
                Enumerable.Range(0, 4)
                    .Select(TryOpenConnection)
            );

            foreach (var trn in transactions)
                trn?.Dispose();

            var sb = new StringBuilder();
            store.WriteCurrentTransactions(sb);
            sb.ToString().Should().BeEmpty();
        }
    }
}
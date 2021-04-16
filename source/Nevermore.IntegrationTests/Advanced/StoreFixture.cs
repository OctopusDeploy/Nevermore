using System;
using System.Linq;
using Nevermore.IntegrationTests.SetUp;
using Nevermore.Transient;
using NUnit.Framework;

namespace Nevermore.IntegrationTests.Advanced
{
    public class StoreFixture : FixtureWithRelationalStore
    {
        [Test]
        public void TransactionsCanBeOpenedInParallel()
        {
            // Clear the retry manager as one of the things this test tests
            // is a race condition setting the retry manager when RetryManager.Instance is called
            RetryManager.SetDefault(null, false);

            Enumerable.Range(0, 5)
                .AsParallel()
                .WithDegreeOfParallelism(10)
                .ForAll(
                    x =>
                    {
                        try
                        {
                            using var trn = Store.BeginTransaction(name: "Transaction " + x);
                            Console.WriteLine($"Transaction {x}: Opened");
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine($"Transaction {x}: Failed {e.Message}");
                            throw;
                        }
                    }
                );
        }
    }
}
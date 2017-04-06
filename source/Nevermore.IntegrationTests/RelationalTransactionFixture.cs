using Nevermore.Contracts;
using Nevermore;
using Nevermore.IntegrationTests.Model;
using Xunit;
using Xunit.Abstractions;

namespace Nevermore.IntegrationTests
{
    public class RelationalTransactionFixture : FixtureWithRelationalStore
    {
        public RelationalTransactionFixture(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void LoadWithSingleId()
        {
            using (var trn = Store.BeginTransaction())
            {
                trn.Load<Product>("A");
            }
        }

        [Fact]
        public void LoadWithMultipleIds()
        {
            using (var trn = Store.BeginTransaction())
            {
                trn.Load<Product>(new[] {"A", "B"});
            }
        }
    }
}
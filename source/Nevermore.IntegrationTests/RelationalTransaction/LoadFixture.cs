using System.Linq;
using Nevermore.Contracts;
using Nevermore;
using Nevermore.IntegrationTests.Model;
using Xunit;
using Xunit.Abstractions;

namespace Nevermore.IntegrationTests.RelationalTransaction
{
    public class LoadFixture : FixtureWithRelationalStore
    {
        public LoadFixture(ITestOutputHelper output) : base(output)
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
        
        [Fact]
        public void LoadWithMoreThan2100Ids()
        {
            using (var trn = Store.BeginTransaction())
            {
                trn.Load<Product>(Enumerable.Range(1, 3000).Select(n => "ID-" + n));
            }
        }
        
        [Fact]
        public void LoadStreamWithMoreThan2100Ids()
        {
            using (var trn = Store.BeginTransaction())
            {
                trn.LoadStream<Product>(Enumerable.Range(1, 3000).Select(n => "ID-" + n));
            }
        }
    }
}
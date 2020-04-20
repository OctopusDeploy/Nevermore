using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;

namespace Nevermore.Benchmarks.SetUp
{
    [Config(typeof(Config))]
    [MemoryDiagnoser]
    public abstract class BenchmarkBase
    {
        protected string ConnectionString { get; set; }

        private class Config : ManualConfig
        {
            public Config()
            {
                AddColumn(
                    StatisticColumn.P50,
                    StatisticColumn.P80,
                    StatisticColumn.P90,
                    StatisticColumn.P95);
            }
        }

        [GlobalSetup]
        public virtual void SetUp()
        {           
            var database = new IntegrationTestDatabase();
            ConnectionString = database.ConnectionString;
        }
    }
}
using BenchmarkDotNet.Attributes;

namespace Nevermore.Benchmarks.SetUp
{
    [MemoryDiagnoser]
    public abstract class BenchmarkBase
    {
        protected string ConnectionString { get; set; }

        [GlobalSetup]
        public virtual void SetUp()
        {           
            var database = new IntegrationTestDatabase();
            ConnectionString = database.ConnectionString;
        }
    }
}
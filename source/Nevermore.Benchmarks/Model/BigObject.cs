using Nevermore.Contracts;

namespace Nevermore.Benchmarks.Model
{
    public class BigObject : IId
    {
        public BigObject()
        {
        }

        public string Id { get; set; }
        public string[] LuckyNames { get; set; }
    }
}
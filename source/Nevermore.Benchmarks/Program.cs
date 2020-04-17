using BenchmarkDotNet.Running;

[assembly: BenchmarkDotNet.Attributes.HtmlExporter]
[assembly: BenchmarkDotNet.Attributes.PlainExporter]

namespace Nevermore.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
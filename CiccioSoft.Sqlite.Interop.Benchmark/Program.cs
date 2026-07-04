using BenchmarkDotNet.Running;

namespace CiccioSoft.Sqlite.Interop.Benchmark;

class Program
{
    static void Main(string[] args)
    {
        // SqliteBenchmark.Run();
        // RawComparisonBenchmark2.Run();
        // RawComparisonBenchmark.Run();
        BenchmarkRunner.Run<DotNetBenchmarkWriter>();
    }
}

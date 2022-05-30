using BenchmarkDotNet.Running;

namespace ListSerializerBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            //BenchmarkRunner.Run<ListSerializerJob>();
            BenchmarkRunner.Run<ListSerializerV2Job>();
        }
    }
}
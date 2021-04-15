using BenchmarkDotNet.Running;

namespace ListSerializerBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ListSerializerJob>();
            //var n = new ListSerializerJob();
            //n.Setup();
            //n.DeepCopy();
        }
    }
}

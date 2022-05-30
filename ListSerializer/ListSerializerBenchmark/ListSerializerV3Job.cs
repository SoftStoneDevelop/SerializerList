using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Common;
using ListSerializer;

namespace ListSerializerBenchmark
{
    [SimpleJob(RuntimeMoniker.Net60)]
    public class ListSerializerV3Job
    {
        [Params(100, 1000, 5000, 10000, 50000, 100000, 10000000)]
        public int Size;

        private ListNode _head;
        private IListSerializer _serializer;

        [IterationSetup]
        public void Setup()
        {
            _serializer = new ListSerializerV3();
            _head = ListNodeInstanceHelper.CreateRandomListNode(Size);
        }

        [Benchmark]
        public void DeepCopy()
        {
            var deepCopyTask = _serializer.DeepCopy(_head);
            deepCopyTask.Wait();
            var head = deepCopyTask.Result;
        }
    }
}
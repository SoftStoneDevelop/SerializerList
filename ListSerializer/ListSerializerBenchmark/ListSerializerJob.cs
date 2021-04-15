using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Common;
using ListSerializer;

namespace ListSerializerBenchmark
{
    [SimpleJob(RuntimeMoniker.NetCoreApp50)]
    public class ListSerializerJob
    {
        [Params(100, 1000, 10000, 100000, 1000000, 10000000)]
        public int Size;

        private ListNode _head;
        private IListSerializer _serializer;

        [IterationSetup]
        public void Setup()
        {
            _serializer = new ListSerializer.ListSerializer();
            _head = new ListNode()
            {
                Data = "Head"
            };
            var next = new ListNode()
            {
                Data = "Next", 
                Previous = _head
            };
            var next2 = new ListNode() { Data = "Next2", Previous = next, Random = next};
            next.Next = next2;
            next.Random = next2;
            _head.Next = next;
            _head.Random = next;
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
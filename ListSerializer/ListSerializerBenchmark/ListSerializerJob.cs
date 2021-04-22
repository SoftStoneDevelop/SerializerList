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
        private IListSerializer _serializerWihoutDictionary;

        [IterationSetup]
        public void Setup()
        {
            _serializer = new ListSerializer.ListSerializer();
            _serializerWihoutDictionary = new ListSerializer.ListSerializerWihoutDictionary();
            _head = ListNodeInstanceHelper.CreateRandomListNode(Size);
        }

        [Benchmark]
        public void DeepCopy()
        {
            var deepCopyTask = _serializer.DeepCopy(_head);
            deepCopyTask.Wait();
            var head = deepCopyTask.Result;
        }

        [Benchmark(Baseline = true)]
        public void DeepCopyWihoutDictionary()
        {
            var deepCopyTask = _serializerWihoutDictionary.DeepCopy(_head);
            deepCopyTask.Wait();
            var head = deepCopyTask.Result;
        }
    }
}
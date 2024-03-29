﻿using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Common;
using ListSerializer;

namespace ListSerializerBenchmark
{
    [MemoryDiagnoser]
    [SimpleJob(RuntimeMoniker.Net70)]
    [HideColumns("Error", "StdDev", "Median", "Gen0", "Gen1", "Gen2", "Alloc Ratio", "RatioSD")]
    [ThreadingDiagnoser]
    public class ListSerializerJob
    {
        [Params(1000, 2500, 5000, 10000, 50000)]
        public int Size;

        private ListNode _head;
        private IListSerializer _serializer1;
        private IListSerializer _serializer2;
        private IListSerializer _serializer3;

        [GlobalSetup]
        public void Setup()
        {
            _serializer1 = new ListSerializer.ListSerializerV1();
            _serializer2 = new ListSerializer.ListSerializerV2();
            _serializer3 = new ListSerializer.ListSerializerV3();
            _head = ListNodeInstanceHelper.CreateRandomListNode(Size);
        }

        [Benchmark(Baseline = true, Description = "V1: single thread algorithm on Dictionary")]
        public void DeepCopyV1()
        {
            var deepCopyTask = _serializer1.DeepCopy(_head);
            deepCopyTask.Wait();
            var head = deepCopyTask.Result;
        }

        [Benchmark(Description = "V2(smallest by memory): multi thread algorithm")]
        public void DeepCopyV2()
        {
            var deepCopyTask = _serializer2.DeepCopy(_head);
            deepCopyTask.Wait();
            var head = deepCopyTask.Result;
        }

        [Benchmark(Description = "V3 (compromise): multi thread algorithm)")]
        public void DeepCopyV3()
        {
            var deepCopyTask = _serializer3.DeepCopy(_head);
            deepCopyTask.Wait();
            var head = deepCopyTask.Result;
        }
    }
}
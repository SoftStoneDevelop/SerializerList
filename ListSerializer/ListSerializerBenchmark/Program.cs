using BenchmarkDotNet.Running;
using Common;
using System;
using System.Collections.Generic;

namespace ListSerializerBenchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ListSerializerJob>();
            BenchmarkRunner.Run<ListSerializerV2Job>();
            BenchmarkRunner.Run<ListSerializerV3Job>();
        }
    }
}
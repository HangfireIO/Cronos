using System;
using BenchmarkDotNet.Running;

namespace Cronos.Benchmark
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<CronBenchmarks>();
            Console.ReadLine();
        }
    }
}

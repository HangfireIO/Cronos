using System;
using BenchmarkDotNet.Running;
using Cronos.Benchmark;

namespace Cronos.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            /*var benchmarks = new CronBenchmarks();

            for (var i = 0; i < 100000000; i++)
            {
                benchmarks.IsUnreachable();
            }*/

            BenchmarkRunner.Run<CronBenchmarks>();
            Console.ReadLine();
        }
    }
}
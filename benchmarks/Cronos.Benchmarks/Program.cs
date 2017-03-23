using System;
using BenchmarkDotNet.Running;

namespace Cronos.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            /*var benchmarks = new CronBenchmarks();

            for (var i = 0; i < 100000000; i++)
            {
                benchmarks.NextUnreachableSimple();
            }*/

            BenchmarkRunner.Run<CronBenchmarks>();
            Console.ReadLine();
        }
    }
}
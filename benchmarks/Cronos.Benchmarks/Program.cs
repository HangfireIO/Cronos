using System;
using System.Linq;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Validators;

namespace Cronos.Benchmarks
{
    public class AllowNonOptimized : ManualConfig
    {
        public AllowNonOptimized()
        {
            Add(JitOptimizationsValidator.DontFailOnError); // ALLOW NON-OPTIMIZED DLLs

            Add(DefaultConfig.Instance.GetLoggers().ToArray()); // manual config has no loggers by default
            Add(DefaultConfig.Instance.GetExporters().ToArray()); // manual config has no exporters by default
            Add(DefaultConfig.Instance.GetColumnProviders().ToArray()); // manual config has no columns by default
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            /*var benchmarks = new CronBenchmarks();

            for (var i = 0; i < 100000000; i++)
            {
                benchmarks.NextUnreachableSimple();
            }*/

            BenchmarkRunner.Run<CronBenchmarks>(new AllowNonOptimized());
            Console.ReadLine();
        }
    }
}
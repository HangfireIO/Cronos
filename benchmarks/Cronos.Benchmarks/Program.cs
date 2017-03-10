using System;
using BenchmarkDotNet.Running;
using Cronos.Benchmark;

namespace Cronos.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
#if DEBUG
            var now = DateTime.UtcNow;
            var end = now.AddYears(100);
            var endTest = now.AddSeconds(20);
            var exp = CronExpression.Parse("* * * * *");
            while (endTest > DateTime.UtcNow)
            {
                exp.GetOccurrence(DateTime.UtcNow.Date, end, TimeZoneInfo.Utc);
            }
#else
            BenchmarkRunner.Run<CronBenchmarks>();
            Console.ReadLine();
#endif
        }
    }
}
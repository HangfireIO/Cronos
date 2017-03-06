using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;

namespace Cronos.Benchmark
{
    [RyuJitX64Job]
    public class CronBenchmarks
    {
        private static readonly CronExpression SimpleExpression = CronExpression.Parse("* * * * ?");
        private static readonly CronExpression ComplexExpression = CronExpression.Parse("*/10 12-20 * DEC 3");

        private static readonly DateTime DateTimeNow = DateTime.Now;

        [Benchmark]
        public unsafe string ParseBaseline()
        {
            fixed (char* pointer = "* * * * ?")
            {
                var ptr = pointer;
                while (*ptr != '\0' && *ptr != '\n' && *ptr != ' ') ptr++;

                return new string(pointer);
            }
        }

        [Benchmark]
        public CronExpression ParseStars()
        {
            return CronExpression.Parse("* * * * *");
        }

        [Benchmark]
        public CronExpression ParseNumber()
        {
            return CronExpression.Parse("20 * * * *");
        }

        [Benchmark]
        public CronExpression ParseRange()
        {
            return CronExpression.Parse("20-40 * * * *");
        }

        [Benchmark]
        public CronExpression ParseList()
        {
            return CronExpression.Parse("20,30,40,50 * * * *");
        }

        [Benchmark]
        public CronExpression ParseComplex()
        {
            return CronExpression.Parse("*/10 12-20 ? DEC 3");
        }

        [Benchmark]
        public DateTimeOffset? NextSimple()
        {
            return SimpleExpression.GetOccurrence(DateTimeNow, DateTimeNow.AddYears(100), TimeZoneInfo.Utc);
        }

        [Benchmark]
        public DateTimeOffset? NextComplex()
        {
            return ComplexExpression.GetOccurrence(DateTimeNow, DateTimeNow.AddYears(100), TimeZoneInfo.Utc);
        }
    }
}

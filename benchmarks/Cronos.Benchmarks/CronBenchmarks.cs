using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using NCrontab;

namespace Cronos.Benchmark
{
    [RyuJitX64Job]
    public class CronBenchmarks
    {
        private static readonly CronExpression SimpleExpression = CronExpression.Parse("* * * * * *", CronFormat.IncludeSeconds);
        private static readonly CronExpression ComplexExpression = CronExpression.Parse("*/10 12-20 * DEC 3");

        private static readonly CronExpression SimpleUnreachableExpression = CronExpression.Parse("* * 30 02 *");
        private static readonly CronExpression ComplexUnreachableExpression = CronExpression.Parse("* * LW * 1#1");

        private static readonly CrontabSchedule SimpleExpressionNCrontab = CrontabSchedule.Parse("* * * * *");
        private static readonly CrontabSchedule ComplexExpressionNCrontab = CrontabSchedule.Parse("*/10 12-20 * DEC 3");

        private static readonly DateTime DateTimeNow = DateTime.UtcNow;
        private static readonly DateTime DateTimeNowPlus100Years = DateTimeNow.AddYears(100);

        private static readonly DateTimeOffset DateTimeOffsetNow = DateTimeOffset.UtcNow;
        private static readonly DateTimeOffset DateTimeOffsetNowPlus100Years = DateTimeOffsetNow.AddYears(100);

        private static readonly TimeZoneInfo UtcTimeZone = TimeZoneInfo.Utc;
        private static readonly TimeZoneInfo PacificTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");

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
        public DateTime? NextSimpleDateTime()
        {
            return SimpleExpression.GetOccurrence(DateTimeNow, DateTimeNowPlus100Years, UtcTimeZone);
        }

        [Benchmark]
        public DateTime? NextComplexDateTime()
        {
            return ComplexExpression.GetOccurrence(DateTimeNow, DateTimeNowPlus100Years, UtcTimeZone);
        }

        [Benchmark]
        public DateTimeOffset? NextSimpleDateTimeOffset()
        {
            return SimpleExpression.GetOccurrence(DateTimeOffsetNow, DateTimeOffsetNowPlus100Years, UtcTimeZone);
        }

        [Benchmark]
        public DateTimeOffset? NextComplexDateTimeOffset()
        {
            return ComplexExpression.GetOccurrence(DateTimeOffsetNow, DateTimeOffsetNowPlus100Years, UtcTimeZone);
        }

        [Benchmark]
        public DateTime? NextSimpleWithTimeZone()
        {
            return SimpleExpression.GetOccurrence(DateTimeNow, DateTimeNowPlus100Years, PacificTimeZone);
        }

        [Benchmark]
        public DateTime? NextComplexWithTimeZone()
        {
            return ComplexExpression.GetOccurrence(DateTimeNow, DateTimeNowPlus100Years, PacificTimeZone);
        }

        [Benchmark]
        public void NextUnreachableSimple()
        {
            var result = SimpleUnreachableExpression
                .GetOccurrence(DateTime.UtcNow, DateTime.UtcNow.AddYears(100), UtcTimeZone);

            if (result != null) throw new InvalidOperationException();
        }

        [Benchmark]
        public void NextUnreachableComplex()
        {
            var result = ComplexUnreachableExpression
                .GetOccurrence(DateTime.UtcNow, DateTime.UtcNow.AddYears(100), UtcTimeZone);

            if (result != null) throw new InvalidOperationException();
        }

        [Benchmark]
        public CrontabSchedule ParseStarsNCrontab()
        {
            return CrontabSchedule.Parse("* * * * *");
        }

        [Benchmark]
        public CrontabSchedule ParseComplexNCrontab()
        {
            return CrontabSchedule.Parse("*/10 12-20 * DEC 3");
        }

        [Benchmark]
        public DateTime NextSimpleNCrontab()
        {
            return SimpleExpressionNCrontab.GetNextOccurrence(DateTimeNow, DateTimeNowPlus100Years);
        }

        [Benchmark]
        public DateTime NextComplexNCrontab()
        {
            return ComplexExpressionNCrontab.GetNextOccurrence(DateTimeNow, DateTimeNowPlus100Years);
        }
    }
}

using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using NCrontab;

namespace Cronos.Benchmark
{
    [RyuJitX64Job]
    public class CronBenchmarks
    {
        private static readonly CronExpression MinutlyExpression = CronExpression.Parse("* * * * *");
        private static readonly CronExpression HourlyExpression = CronExpression.Parse("0 * * * *");
        private static readonly CronExpression DaylyExpression = CronExpression.Parse("0 3 * * *");
        private static readonly CronExpression MonthlyExpression = CronExpression.Parse("0 6 1 * *");

        private static readonly CrontabSchedule NCrontabSimpleExpression = NCrontab.CrontabSchedule.Parse("* * * * *", new CrontabSchedule.ParseOptions{IncludingSeconds = false});
        private static readonly CronExpression ComplexExpression = CronExpression.Parse("*/10 12-20 * DEC 3");

        private static readonly DateTimeOffset DateTimeNow = DateTimeOffset.UtcNow.Date;
        private static readonly DateTime DateTimeNow1 = DateTime.UtcNow.Date;
        private static readonly DateTimeOffset DateTimeNow2 = DateTimeOffset.UtcNow.AddMinutes(3);
        private static readonly DateTimeOffset DateTimeNow3 = DateTimeOffset.UtcNow.AddMinutes(7);
        private static readonly DateTimeOffset EndDateTime  = DateTimeNow.AddYears(100);
        private static readonly DateTime EndDateTime1 = DateTimeNow1.AddYears(100);

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
        public DateTimeOffset? GetOccurrenceSimpleDateTime()
        {
            return MinutlyExpression.GetOccurrence(DateTimeNow1, EndDateTime1, TimeZoneInfo.Utc);
        }

        [Benchmark]
        public DateTimeOffset? GetOccurrenceComplexDateTime()
        {
            return ComplexExpression.GetOccurrence(DateTimeNow, EndDateTime, TimeZoneInfo.Utc);
        }

        [Benchmark]
        public DateTimeOffset? GetOccurrenceSimpleDateTimeOffset()
        {
            return MinutlyExpression.GetOccurrence(DateTimeNow1, EndDateTime1, TimeZoneInfo.Utc);
        }

        [Benchmark]
        public DateTimeOffset? GetOccurrenceComplexDateTimeOffset()
        {
            return ComplexExpression.GetOccurrence(DateTimeNow, EndDateTime, TimeZoneInfo.Utc);
        }

        //[Benchmark]
        public DateTimeOffset? NCrontabSimple()
        {
            return NCrontabSimpleExpression.GetNextOccurrence(DateTimeNow1);
        }
        
        [Benchmark]
        public DateTimeOffset? GetOccurenceMinutelyOffset()
        {
            return HourlyExpression.GetOccurrence(DateTimeNow, EndDateTime, TimeZoneInfo.Utc);
        }

        //[Benchmark]
        //public DateTimeOffset? GetOccurenceDayly()
        //{
        //    return DaylyExpression.GetOccurrence(DateTimeNow3, EndDateTime, TimeZoneInfo.Utc);
        //}

        //[Benchmark]
        //public DateTimeOffset? GetOccurenceMonthly()
        //{
        //    return MonthlyExpression.GetOccurrence(DateTimeNow3, EndDateTime, TimeZoneInfo.Utc);
        //}
    }
}

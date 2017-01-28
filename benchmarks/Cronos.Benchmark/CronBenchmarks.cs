using BenchmarkDotNet.Attributes;
using NodaTime;

namespace Cronos.Benchmark
{
    public class CronBenchmarks
    {
        private static readonly CronExpression SimpleExpression = CronExpression.Parse("* * * * * ?");
        private static readonly CronExpression ComplexExpression = CronExpression.Parse("* */10 12-20 ? DEC 3");

        private static readonly Instant NowInstant = SystemClock.Instance.Now;
        private static readonly ZonedDateTime NowUtc = NowInstant.InUtc();
        private static readonly LocalDateTime Now = NowUtc.LocalDateTime;
        private static readonly int Second = Now.Second;
        private static readonly int Minute = Now.Minute;
        private static readonly int Hour = Now.Hour;
        private static readonly int Day = Now.Day;
        private static readonly int Month = Now.Month;
        private static readonly int DayOfWeek = Now.DayOfWeek;
        private static readonly int Year = Now.Year;

        [Benchmark]
        public unsafe string ParseBaseline()
        {
            fixed (char* pointer = "* * * * * ?")
            {
                var ptr = pointer;
                while (*ptr != '\0' && *ptr != '\n' && *ptr != ' ') ptr++;

                return new string(pointer);
            }
        }

        [Benchmark]
        public CronExpression ParseSimple()
        {
            return CronExpression.Parse("* * * * * ?");
        }

        [Benchmark]
        public CronExpression ParseComplex()
        {
            return CronExpression.Parse("* */10 12-20 ? DEC 3");
        }

        [Benchmark]
        public bool IsMatchNumbers()
        {
            return ComplexExpression.IsMatch(Second, Minute, Hour, Day, Month, DayOfWeek, Year);
        }

        [Benchmark]
        public bool IsMatchDateTime()
        {
            return ComplexExpression.IsMatch(Now);
        }

        [Benchmark]
        public ZonedDateTime? NextSimple()
        {
            return SimpleExpression.Next(NowUtc);
        }

        [Benchmark]
        public ZonedDateTime? NextComplex()
        {
            return ComplexExpression.Next(NowUtc);
        }

    }
}

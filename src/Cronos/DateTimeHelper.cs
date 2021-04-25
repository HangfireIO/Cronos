using System;

namespace Cronos
{
    internal static class DateTimeHelper
    {
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        public static DateTimeOffset FloorToSeconds(DateTimeOffset dateTimeOffset) => dateTimeOffset.AddTicks(-GetExtraTicks(dateTimeOffset.Ticks));

        public static bool IsRound(DateTimeOffset dateTimeOffset) => GetExtraTicks(dateTimeOffset.Ticks) == 0;

        private static long GetExtraTicks(long ticks) => ticks % OneSecond.Ticks;
    }
}
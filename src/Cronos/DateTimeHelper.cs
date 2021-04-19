using System;

namespace Cronos
{
    internal static class DateTimeHelper
    {
        private static readonly TimeSpan OneSecond = TimeSpan.FromSeconds(1);

        public static DateTimeOffset FloorToSeconds(DateTimeOffset dateTimeOffset)
        {
            return dateTimeOffset.AddTicks(-GetExtraTicks(dateTimeOffset.Ticks));
        }

        public static DateTime FloorToSeconds(DateTime dateTime)
        {
            return dateTime.AddTicks(-GetExtraTicks(dateTime.Ticks));
        }
        
        private static long GetExtraTicks(long ticks)
        {
            return ticks % OneSecond.Ticks;
        }
}
}
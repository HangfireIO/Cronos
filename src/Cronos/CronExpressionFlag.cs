using System;

namespace Cronos
{
    [Flags]
    internal enum CronExpressionFlag : byte
    {
        None = 0b0,
        DayOfMonthLast = 0b1,
        DayOfWeekLast = 0b10,
        Interval = 0b100,
        NearestWeekday = 0b1000,
        NthDayOfWeek = 0b10000
    }
}

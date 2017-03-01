using System;

namespace Cronos
{
    [Flags]
    internal enum CronExpressionFlag
    {
        None = 0x0,
        DayOfMonthQuestion = 0x8,
        DayOfMonthLast = 0x10,
        DayOfWeekLast = 0x20,
        Interval = 0x40,
        NearestWeekday = 0x80
    }
}

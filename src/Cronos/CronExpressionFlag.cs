using System;

namespace Cronos
{
    [Flags]
    internal enum CronExpressionFlag
    {
        None = 0x0,
        SecondStar = 0x1,
        MinuteStar = 0x2,
        HourStar = 0x4,
        DayOfMonthQuestion = 0x8,
        DayOfMonthLast = 0x10,
        DayOfWeekLast = 0x20
    }
}

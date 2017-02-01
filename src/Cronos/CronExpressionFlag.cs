using System;

namespace Cronos
{
    [Flags]
    public enum CronExpressionFlag
    {
        None = 0x0,
        DayOfMonthStar = 0x1,
        DayOfWeekStar = 0x2,
        MinuteStar = 0x4,
        HourStar = 0x8,
        SecondStar = 0x10,
        DayOfMonthLast = 0x20,
        DayOfWeekLast = 0x40,
    }
}

using System;

namespace Cronos
{
    [Flags]
    public enum CronExpressionFlag
    {
        None = 0x0,
        SecondStar = 0x1,
        MinuteStar = 0x2,
        HourStar = 0x4,
        DayOfMonthStar = 0x8,
        DayOfMonthQuestion = 0x10,
        DayOfMonthLast = 0x20,
        DayOfWeekLast = 0x40
    }
}

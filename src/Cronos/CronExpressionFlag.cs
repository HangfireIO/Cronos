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
        DayOfWeekStar = 0x10, // TODO Remove.
        DayOfMonthQuestion = 0x40,
        DayOfWeekQuestion = 0x80, // TODO Remove.
        DayOfMonthLast = 0x100,
        DayOfWeekLast = 0x200
    }
}

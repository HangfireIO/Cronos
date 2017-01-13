using System;

namespace Cronos
{
    [Flags]
    public enum CronExpressionFlag
    {
        None = 0x0,
        DayOfMonthStar = 0x1,
        DayOfWeekStar = 0x2,
        WhenReboot = 0x4, // TODO: Remove this
        MinuteStar = 0x8,
        HourStar = 0x10,
        SecondStar = 0x20,
        DayOfMonthLast = 0x40,
        DayOfWeekLast = 0x80,
    }
}

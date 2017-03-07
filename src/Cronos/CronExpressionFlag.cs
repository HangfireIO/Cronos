using System;
using System.Runtime.CompilerServices;

namespace Cronos
{
    [Flags]
    internal enum CronExpressionFlag
    {
        None = 0b0,
        DayOfMonthQuestion = 0b1,
        DayOfMonthLast = 0b10,
        DayOfWeekLast = 0b100,
        Interval = 0b1000,
        NearestWeekday = 0b10000,
        NthDayOfWeek = 0b100000,

        LongPath = DayOfMonthLast | DayOfWeekLast | NearestWeekday | NthDayOfWeek
    }
}

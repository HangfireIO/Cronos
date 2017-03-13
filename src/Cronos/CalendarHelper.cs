using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Cronos
{
    internal static class CalendarHelper
    {
        private const long TicksPerMillisecond = 10000;
        private const long TicksPerSecond = TicksPerMillisecond * 1000;
        private const long TicksPerMinute = TicksPerSecond * 60;
        private const long TicksPerHour = TicksPerMinute * 60;
        private const long TicksPerDay = TicksPerHour * 24;

        private static readonly int[] DaysToMonth365 =
        {
            0, 31, 59, 90, 120, 151, 181, 212, 243, 273, 304, 334, 365
        };

        private static readonly int[] DaysToMonth366 =
        {
            0, 31, 60, 91, 121, 152, 182, 213, 244, 274, 305, 335, 366
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DayOfWeek GetDayOfWeek(int year, int month, int day)
        {
            var isLeapYear = year % 4 == 0 && (year % 100 != 0 || year % 400 == 0);
            int[] days = isLeapYear ? DaysToMonth366 : DaysToMonth365;
            int y = year - 1;
            int n = y * 365 + y / 4 - y / 100 + y / 400 + days[month - 1] + day - 1;
            var ticks = n * TicksPerDay;

            return ((DayOfWeek)((int)(ticks / TicksPerDay + 1) % 7));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLessThan(
            int year1, int month1, int day1, int hour1, int minute1, int second1,
            int year2, int month2, int day2, int hour2, int minute2, int second2)
        {
            if (year1 != year2) return year1 < year2;
            if (month1 != month2) return month1 < month2;
            if (day1 != day2) return day1 < day2;
            if (hour1 != hour2) return hour1 < hour2;
            if (minute1 != minute2) return minute1 < minute2;
            if (second1 != second2) return second1 < second2;

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetDaysInMonth(int year, int month)
        {
            int[] days = ((year % 4 == 0 && (year % 100 != 0 || year % 400 == 0)) ? DaysToMonth366 : DaysToMonth365);
            return (days[month] - days[month - 1]);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MoveToNearestWeekDay(ref int day, ref DayOfWeek dayOfWeek, int lastDayOfMonth)
        {
            if (dayOfWeek == DayOfWeek.Sunday)
            {
                if (day == lastDayOfMonth)
                {
                    day = day - 2;
                    dayOfWeek = DayOfWeek.Friday;
                    return -2;
                }
                day++;
                dayOfWeek = DayOfWeek.Monday;
                return 1;
            }
            if (dayOfWeek == DayOfWeek.Saturday)
            {
                if (day == Constants.DaysOfMonth.First)
                {
                    day = day + 2;
                    dayOfWeek = DayOfWeek.Monday;
                    return 2;
                }
                day--;
                dayOfWeek = DayOfWeek.Friday;
                return -1;
            }
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsNthDayOfWeek(int day, int n)
        {
            return day - Constants.DaysPerWeekCount * n < Constants.DaysOfMonth.First &&
                   day - Constants.DaysPerWeekCount * (n - 1) >= Constants.DaysOfMonth.First;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLastDayOfWeek(int day, int lastDayOfMonth)
        {
            return day + Constants.DaysPerWeekCount > lastDayOfMonth;
        }
    }
}
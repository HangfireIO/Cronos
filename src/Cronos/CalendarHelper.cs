using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Cronos
{
    internal static class CalendarHelper
    {
        private static readonly Calendar Calendar = CultureInfo.InvariantCulture.Calendar;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DayOfWeek GetDayOfWeek(DateTime dateTime)
        {
            return Calendar.GetDayOfWeek(dateTime);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetDaysInMonth(int year, int month)
        {
            return Calendar.GetDaysInMonth(year, month);
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
                if (day == Constants.FirstDayOfMonth)
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
            return day - Constants.DaysPerWeekCount * n < Constants.FirstDayOfMonth &&
                   day - Constants.DaysPerWeekCount * (n - 1) >= Constants.FirstDayOfMonth;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLastDayOfWeek(int day, int lastDayOfMonth)
        {
            return day + Constants.DaysPerWeekCount > lastDayOfMonth;
        }
    }
}
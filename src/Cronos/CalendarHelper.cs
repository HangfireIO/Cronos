using System;
using System.Globalization;

namespace Cronos
{
    internal static class CalendarHelper
    {
        private static readonly Calendar Calendar = CultureInfo.InvariantCulture.Calendar;

        public static DayOfWeek GetDayOfWeek(DateTime dateTime)
        {
            return Calendar.GetDayOfWeek(dateTime);
        }

        public static int GetDaysInMonth(int year, int month)
        {
            return Calendar.GetDaysInMonth(year, month);
        }

        public static int GetNearestWeekDay(int day, DayOfWeek dayOfWeek, int lastDayOfMonth)
        {
            if (dayOfWeek == DayOfWeek.Sunday)
            {
                if (day == lastDayOfMonth)
                {
                    return day - 2;
                }
                return day + 1;
            }
            if (dayOfWeek == DayOfWeek.Saturday)
            {
                if (day == Constants.FirstDayOfMonth)
                {
                    return day + 2;
                }
                return day - 1;
            }
            return day;
        }

        public static bool IsNthDayOfWeek(int day, int n)
        {
            return day - Constants.DaysPerWeekCount * n < Constants.FirstDayOfMonth &&
                   day - Constants.DaysPerWeekCount * (n - 1) >= Constants.FirstDayOfMonth;
        }

        public static bool IsLastDayOfWeek(int day, int lastDayOfMonth)
        {
            return day + Constants.DaysPerWeekCount > lastDayOfMonth;
        }
    }
}
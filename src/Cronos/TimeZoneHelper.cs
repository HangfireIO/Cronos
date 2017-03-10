// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;

namespace Cronos
{
    internal static class TimeZoneHelper
    {
        // This method is here because .NET Framework, .NET Core and Mono works different when transition from standard time (ST) to
        // daylight saving time (DST) happens.
        // When DST ends you set the clocks backward. If you are in USA it happens on first sunday of November at 2:00 am. 
        // So duration from 1:00 am to 2:00 am repeats twice.
        // .NET Framework and .NET Core consider backward DST transition as [1:00 DST ->> 2:00 DST)--[1:00 ST --> 2:00 ST]. So 2:00 is not ambiguous, but 1:00 is ambiguous.
        // Mono consider backward DST transition as [1:00 DST ->> 2:00 DST]--(1:00 ST --> 2:00 ST]. So 2:00 is ambiguous, but 1:00 is not ambiguous.
        // We have to add 1 tick to amiguousTime to have the same behavior for all frameworks. Thus 1:00 is ambiguous and 2:00 is not ambiguous. 
        public static bool IsAmbiguousTime(TimeZoneInfo zone, DateTime ambiguousTime)
        {
            return zone.IsAmbiguousTime(ambiguousTime.AddTicks(1));
        }

        public static TimeSpan[] GetAmbiguousOffsets(TimeZoneInfo zone, DateTime ambiguousTime)
        {
            return zone.GetAmbiguousTimeOffsets(ambiguousTime.AddTicks(1));
        }

        public static TimeSpan GetDstOffset(DateTime ambiguousDateTime, TimeZoneInfo zone)
        {
            var offsets = TimeZoneHelper.GetAmbiguousOffsets(zone, ambiguousDateTime);

            var baseOffset = zone.BaseUtcOffset;

            for (var i = 0; i < offsets.Length; i++)
            {
                if (offsets[i] != baseOffset) return offsets[i];
            }

            throw new InvalidOperationException();
        }

        public static DateTimeOffset GetDstTransitionEndDateTime(TimeZoneInfo zone, DateTime ambiguousDateTime, TimeSpan dstOffset)
        {
#if NETSTANDARD1_0
            var dstTransitionDateTime = ambiguousDateTime;

            while (zone.IsAmbiguousTime(dstTransitionDateTime))
            {
                dstTransitionDateTime = dstTransitionDateTime.AddMinutes(1);
            }

            while (!zone.IsAmbiguousTime(dstTransitionDateTime))
            {
                dstTransitionDateTime = dstTransitionDateTime.AddSeconds(-1);
            }

            while (zone.IsAmbiguousTime(dstTransitionDateTime))
            {
                dstTransitionDateTime = dstTransitionDateTime.AddMilliseconds(1);
            }

            return new DateTimeOffset(
                dstTransitionDateTime.Year,
                dstTransitionDateTime.Month,
                dstTransitionDateTime.Day,
                dstTransitionDateTime.Hour,
                dstTransitionDateTime.Minute,
                dstTransitionDateTime.Second,
                dstTransitionDateTime.Millisecond,
                dstOffset);
#else
            var adjustmentRule = TimeZoneHelper.GetAdjustmentRuleForTime(zone, ambiguousDateTime);

            var dstTransitionDateTime = TimeZoneHelper.TransitionTimeToDateTime(ambiguousDateTime.Year, adjustmentRule.DaylightTransitionEnd);

            return new DateTimeOffset(dstTransitionDateTime, dstOffset);
#endif
        }

        public static DateTimeOffset GetDstStartDateTime(TimeZoneInfo zone, DateTime invalidDateTime, TimeSpan baseOffset)
        {
#if NETSTANDARD1_0
            var dstTransitionDateTime = invalidDateTime;

            while (zone.IsInvalidTime(dstTransitionDateTime))
            {
                dstTransitionDateTime = dstTransitionDateTime.AddMinutes(1);
            }

            while (!zone.IsInvalidTime(dstTransitionDateTime))
            {
                dstTransitionDateTime = dstTransitionDateTime.AddSeconds(-1);
            }

            while (zone.IsInvalidTime(dstTransitionDateTime))
            {
                dstTransitionDateTime = dstTransitionDateTime.AddMilliseconds(1);
            }

            var dstOffset = zone.GetUtcOffset(dstTransitionDateTime);

            return new DateTimeOffset(dstTransitionDateTime, dstOffset);
#else
            var adjustmentRule = TimeZoneHelper.GetAdjustmentRuleForTime(zone, invalidDateTime);

            var dstTransitionDateTime = TimeZoneHelper.TransitionTimeToDateTime(invalidDateTime.Year, adjustmentRule.DaylightTransitionStart);

            var dstOffset = baseOffset.Add(adjustmentRule.DaylightDelta);

            return new DateTimeOffset(dstTransitionDateTime, baseOffset).ToOffset(dstOffset);
#endif
        }

#if !NETSTANDARD1_0
        public static TimeZoneInfo.AdjustmentRule GetAdjustmentRuleForTime(TimeZoneInfo zone, DateTime dateTime)
        {
            var date = dateTime.Date;
            var rules = zone.GetAdjustmentRules();
            for (var i = 0; i < rules.Length; i++)
            {
                if (rules[i].DateStart <= date && rules[i].DateEnd >= date) return rules[i];
            }
            return null;
        }

        public static DateTime TransitionTimeToDateTime(Int32 year, TimeZoneInfo.TransitionTime transitionTime)
        {
            DateTime value;
            DateTime timeOfDay = transitionTime.TimeOfDay;

            if (transitionTime.IsFixedDateRule)
            {
                // create a DateTime from the passed in year and the properties on the transitionTime

                // if the day is out of range for the month then use the last day of the month
                Int32 day = DateTime.DaysInMonth(year, transitionTime.Month);

                value = new DateTime(year, transitionTime.Month, (day < transitionTime.Day) ? day : transitionTime.Day,
                    timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);
            }
            else
            {
                if (transitionTime.Week <= 4)
                {
                    //
                    // Get the (transitionTime.Week)th Sunday.
                    //
                    value = new DateTime(year, transitionTime.Month, 1,
                        timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);

                    int dayOfWeek = (int)value.DayOfWeek;
                    int delta = (int)transitionTime.DayOfWeek - dayOfWeek;
                    if (delta < 0)
                    {
                        delta += 7;
                    }
                    delta += 7 * (transitionTime.Week - 1);

                    if (delta > 0)
                    {
                        value = value.AddDays(delta);
                    }
                }
                else
                {
                    //
                    // If TransitionWeek is greater than 4, we will get the last week.
                    //
                    Int32 daysInMonth = DateTime.DaysInMonth(year, transitionTime.Month);
                    value = new DateTime(year, transitionTime.Month, daysInMonth,
                        timeOfDay.Hour, timeOfDay.Minute, timeOfDay.Second, timeOfDay.Millisecond);

                    // This is the day of week for the last day of the month.
                    int dayOfWeek = (int)value.DayOfWeek;
                    int delta = dayOfWeek - (int)transitionTime.DayOfWeek;
                    if (delta < 0)
                    {
                        delta += 7;
                    }

                    if (delta > 0)
                    {
                        value = value.AddDays(-delta);
                    }
                }
            }
            return value;
        }
#endif
    }
}

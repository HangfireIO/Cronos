using System;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Cronos
{
    /// <summary>
    /// Provides a parser and scheduler for cron expressions.
    /// </summary>
    public sealed class CronExpression
    {
        private long _second;     // 60 bits -> from 0 bit to 59 bit in Int64
        private long _minute;     // 60 bits -> from 0 bit to 59 bit in Int64
        private long _hour;       // 24 bits -> from 0 bit to 23 bit in Int64
        private long _dayOfMonth; // 31 bits -> from 1 bit to 31 bit in Int64
        private long _month;      // 12 bits -> from 1 bit to 12 bit in Int64
        private long _dayOfWeek;  // 8 bits  -> from 0 bit to 7  bit in Int64

        private int _nthdayOfWeek;
        private bool _nearestWeekday;

        private CronExpressionFlag _flags;

        private CronExpression()
        {
        }

        private static Calendar Calendar => CultureInfo.InvariantCulture.Calendar;

        ///<summary>
        /// Constructs a new <see cref="CronExpression"/> based on the specified
        /// cron expression. It's suppoted expressions consisting of 5 or 6 fields:
        /// second (optional), minute, hour, day of month, month, day of week. 
        /// See more: <a href="https://github.com/HangfireIO/Cronos">https://github.com/HangfireIO/Cronos</a>
        /// </summary>
        public static CronExpression Parse(string cronExpression)
        {
            if (string.IsNullOrEmpty(cronExpression)) throw new ArgumentNullException(nameof(cronExpression));

            var expression = new CronExpression();

            unsafe
            {
                fixed (char* value = cronExpression)
                {
                    var fieldsCount = CountFields(value);

                    var pointer = value;

                    SkipWhiteSpaces(ref pointer);

                    // Second.

                    if (fieldsCount == Constants.CronWithSecondsFieldsCount)
                    {
                        if (*pointer == '*')
                        {
                            expression._flags |= CronExpressionFlag.SecondStar;
                        }

                        pointer = GetList(ref expression._second, Constants.FirstSecond, Constants.LastSecond, null, pointer, CronFieldType.Second);
                    }
                    else if(fieldsCount != Constants.CronWithoutSecondsFieldsCount)
                    {
                        throw new FormatException($@"'{cronExpression}'  '* * * *' is an invalid cron expression. 
It must contain 5 of 6 fields in the sequence of seconds (optional), minutes, hours, day of month, months and day of week.");
                    }
                    else
                    {
                        SetAllBits(out expression._second);
                    }

                    // Minute.

                    if (*pointer == '*')
                    {
                        expression._flags |= CronExpressionFlag.MinuteStar;
                    }

                    pointer = GetList(ref expression._minute, Constants.FirstMinute, Constants.LastMinute, null, pointer, CronFieldType.Minute);

                    // Hour.

                    if (*pointer == '*')
                    {
                        expression._flags |= CronExpressionFlag.HourStar;
                    }

                    pointer = GetList(ref expression._hour, Constants.FirstHour, Constants.LastHour, null, pointer, CronFieldType.Hour);

                    // Day of month.

                    if (*pointer == '?')
                    {
                        expression._flags |= CronExpressionFlag.DayOfMonthQuestion;
                    }
                    else if (*pointer == 'L')
                    {
                        expression._flags |= CronExpressionFlag.DayOfMonthLast;
                    }

                    pointer = GetList(ref expression._dayOfMonth, Constants.FirstDayOfMonth, Constants.LastDayOfMonth, null, pointer, CronFieldType.DayOfMonth);

                    if (*pointer == 'W')
                    {
                        expression._nearestWeekday = true;
                        pointer++;

                        SkipWhiteSpaces(ref pointer);
                    }

                    // Month.

                    pointer = GetList(ref expression._month, Constants.FirstMonth, Constants.LastMonth, Constants.MonthNamesArray, pointer, CronFieldType.Month);

                    // Day of week.

                    if (*pointer == '?' && expression.HasFlag(CronExpressionFlag.DayOfMonthQuestion))
                    {
                        throw new FormatException($"'{CronFieldType.DayOfWeek}': '?' is not supported.");
                    }

                    pointer = GetList(ref expression._dayOfWeek, Constants.FirstDayOfWeek, Constants.LastDayOfWeek, Constants.DayOfWeekNamesArray, pointer, CronFieldType.DayOfWeek);

                    if (*pointer == 'L')
                    {
                        expression._flags |= CronExpressionFlag.DayOfWeekLast;
                        pointer++;
                    }

                    if (*pointer == '#')
                    {
                        pointer++;
                        pointer = GetNumber(out expression._nthdayOfWeek, Constants.MinNthDayOfWeek, null, pointer);

                        if (pointer == null || expression._nthdayOfWeek < Constants.MinNthDayOfWeek || expression._nthdayOfWeek > Constants.MaxNthDayOfWeek)
                        {
                            throw new FormatException($"'#' must be followed by a number between {Constants.MinNthDayOfWeek} and {Constants.MaxNthDayOfWeek}.");
                        }
                    }

                    // Make sundays equivalent.
                    if (GetBit(expression._dayOfWeek, 0) || GetBit(expression._dayOfWeek, 7))
                    {
                        SetBit(ref expression._dayOfWeek, 0);
                        SetBit(ref expression._dayOfWeek, 7);
                    }

                    return expression;
                }
            }
        }

        /// <summary>
        /// Calculate next execution starting with a <paramref name="startDateTimeOffset"/> and 
        /// up to <paramref name="startDateTimeOffset"/> (all inclusive) in given <paramref name="timeZone"/>.
        /// </summary>
        public DateTimeOffset? Next(DateTimeOffset startDateTimeOffset, DateTimeOffset endDateTimeOffset, TimeZoneInfo timeZone)
        {
            if (timeZone.Equals(TimeZoneInfo.Utc))
            {
                var found = Next(startDateTimeOffset.DateTime, endDateTimeOffset.DateTime);

                return found != null
                    ? new DateTimeOffset(found.Value, TimeSpan.Zero)
                    : (DateTimeOffset?)null;
            }

            var startLocalDateTime = startDateTimeOffset.DateTime;
            var endLocalDateTime = endDateTimeOffset.DateTime;

            var currentOffset = startDateTimeOffset.Offset;

            if (IsMatch(startLocalDateTime))
            {
                if (timeZone.IsInvalidTime(startLocalDateTime))
                {
                    var nextValidTime = GetDstTransitionStartDateTime(timeZone, startLocalDateTime, timeZone.BaseUtcOffset);

                    return nextValidTime;
                }
                if (timeZone.IsAmbiguousTime(startLocalDateTime))
                {
                    // Ambiguous.

                    // Interval jobs should be fired in both offsets.
                    if (HasFlag(CronExpressionFlag.SecondStar | CronExpressionFlag.MinuteStar | CronExpressionFlag.HourStar))
                    {
                        return new DateTimeOffset(startLocalDateTime, currentOffset);
                    }

                    TimeSpan lateOffset = timeZone.BaseUtcOffset;

                    // Strict jobs should be fired in lowest offset only.
                    if (currentOffset != lateOffset)
                    {
                        return new DateTimeOffset(startLocalDateTime, currentOffset);
                    }
                }
                else
                {
                    // Strict
                    return new DateTimeOffset(startLocalDateTime, timeZone.GetUtcOffset(startLocalDateTime));
                }
            }

            if (timeZone.IsAmbiguousTime(startLocalDateTime))
            {
                TimeSpan lateOffset = timeZone.BaseUtcOffset;

                TimeSpan earlyOffset = GetDstOffset(startLocalDateTime, timeZone);

                if (earlyOffset == currentOffset)
                {
                    var dstTransitionEndDateTimeOffset = GetDstTransitionEndDateTime(timeZone, startLocalDateTime, earlyOffset);

                    var earlyIntervalLocalEnd = dstTransitionEndDateTimeOffset.AddSeconds(-1).DateTime;

                     // Current period, try to find anything here.
                    var found = Next(startLocalDateTime, earlyIntervalLocalEnd);

                    if (found.HasValue)
                    {
                        return Next(new DateTimeOffset(found.Value, currentOffset), endDateTimeOffset, timeZone);
                    }

                    var lateIntervalLocalStart = dstTransitionEndDateTimeOffset.ToOffset(lateOffset).DateTime;

                    //Try to find anything starting from late offset.
                    found = Next(lateIntervalLocalStart, endLocalDateTime);

                    if (found.HasValue)
                    {
                        return Next(new DateTimeOffset(found.Value, lateOffset), endDateTimeOffset, timeZone);
                    }
                }
            }

            // Does not match, find next.
            var nextFound = Next(startLocalDateTime.AddSeconds(1), endLocalDateTime);

            if (nextFound == null) return null;

            return Next(new DateTimeOffset(nextFound.Value, currentOffset), endDateTimeOffset, timeZone);
        }

        private DateTime? Next(DateTime baseTime, DateTime endTime)
        {
            var baseYear = baseTime.Year;
            var baseMonth = baseTime.Month;
            var baseDay = baseTime.Day;
            var baseHour = baseTime.Hour;
            var baseMinute = baseTime.Minute;
            var baseSecond = baseTime.Second;

            var year = baseYear;
            var month = baseMonth;
            var day = baseDay;
            var hour = baseHour;
            var minute = baseMinute;
            var second = baseSecond;

            var minSecond = FindFirstSet(_second, Constants.FirstSecond, Constants.LastSecond);
            var minMinute = FindFirstSet(_minute, Constants.FirstMinute, Constants.LastMinute);
            var minHour = FindFirstSet(_hour, Constants.FirstHour, Constants.LastHour);
            var minMonth = FindFirstSet(_month, Constants.FirstMonth, Constants.LastMonth);

            //
            // Second.
            //

            var nextSecond = FindFirstSet(_second, second, Constants.LastSecond);

            if (nextSecond != -1)
            {
                second = nextSecond;
            }
            else
            {
                second = minSecond;
                minute++;
            }

            //
            // Minute.
            //

            var nextMinute = FindFirstSet(_minute, minute, Constants.LastMinute);

            if (nextMinute != -1)
            {
                minute = nextMinute;

                if (minute > baseMinute)
                {
                    second = minSecond;
                }
            }
            else
            {
                second = minSecond;
                minute = minMinute;
                hour++;
            }

            //
            // Hour.
            //

            var nextHour = FindFirstSet(_hour, hour, Constants.LastHour);

            if (nextHour != -1)
            {
                hour = nextHour;

                if (hour > baseHour)
                {
                    second = minSecond;
                    minute = minMinute;
                }
            }
            else
            {
                second = minSecond;
                minute = minMinute;
                hour = minHour;
                day++;
            }

            //
            // Day of month.
            //         

            RetryDayMonth:

            day = GetNextDayOfMonth(year, month, day);

            if (day < Constants.FirstDayOfMonth || day > Constants.LastDayOfMonth)
            {
                month++;
                day = GetNextDayOfMonth(year, month, Constants.FirstDayOfMonth);
                second = minSecond;
                minute = minMinute;
                hour = minHour;
            }
            else if (day > baseDay)
            {
                second = minSecond;
                minute = minMinute;
                hour = minHour;
            }

            //
            // Month.
            //

            var nextMonth = FindFirstSet(_month, month, Constants.LastMonth);

            if (nextMonth != -1)
            {
                if (nextMonth > month)
                {
                    second = minSecond;
                    minute = minMinute;
                    hour = minHour;
                    day = GetNextDayOfMonth(year, nextMonth, Constants.FirstDayOfMonth);
                }

                month = nextMonth;
            }
            else
            {
                second = minSecond;
                minute = minMinute;
                hour = minHour;
                month = minMonth;
                year++;
                day = GetNextDayOfMonth(year, month, Constants.FirstDayOfMonth);
            }

            if (day < Constants.FirstDayOfMonth || day > Constants.LastDayOfMonth)
            {
                if (new DateTime(year, month, Constants.FirstDayOfMonth, hour, minute, second) > endTime) return null;

                day = -1;
                goto RetryDayMonth;
            }

            var dayOfWeek = Calendar.GetDayOfWeek(new DateTime(year, month, day));
            var lastDayOfMonth = Calendar.GetDaysInMonth(year, month);

            // W character.

            if (_nearestWeekday)
            {
                var nearestWeekDay = GetNearestWeekDay(day, dayOfWeek, lastDayOfMonth);

                if (nearestWeekDay > day)
                {
                    // Day was shifted from Saturday or Sunday to Monday.
                    hour = minHour;
                    minute = minMinute;
                    second = minSecond;
                    dayOfWeek = DayOfWeek.Monday;
                }
                else if (nearestWeekDay < day)
                {
                    // Day was shifted from Saturday or Sunday to Friday.
                    dayOfWeek = DayOfWeek.Friday;

                    if (month == baseMonth && year == baseYear)
                    {
                        if (nearestWeekDay < baseDay || nearestWeekDay == baseDay && nextHour == -1)
                        {
                            day = -1;
                        }
                        else if (nearestWeekDay == baseDay)
                        {
                            // Recover hour, minute and second matched for baseDay.
                            hour = nextHour;
                            minute = nextHour > baseHour ? minMinute : nextMinute;
                            second = nextMinute > baseMinute ? minSecond : nextSecond;
                        }
                    }
                }

                if (new DateTime(year, month, nearestWeekDay, hour, minute, second) > endTime)
                    return null;

                if (((_dayOfWeek >> (int)dayOfWeek) & 1) == 0) day = -1;

                if (HasFlag(CronExpressionFlag.DayOfWeekLast) && !IsLastDayOfWeek(nearestWeekDay, lastDayOfMonth))
                {
                    day = -1;
                }

                if (_nthdayOfWeek != 0 && !IsNthDayOfWeek(nearestWeekDay, _nthdayOfWeek))
                {
                    day = -1;
                }

                if (day == -1) goto RetryDayMonth;

                day = nearestWeekDay;
            }

            if (new DateTime(year, month, day, hour, minute, second) > endTime)
            {
                return null;
            }

            //
            // Day of week.
            //

            if (((_dayOfWeek >> (int)dayOfWeek) & 1) == 0)
            {
                second = minSecond;
                minute = minMinute;
                hour = minHour;
                day++;

                goto RetryDayMonth;
            }

            // L character in day of week.

            if (HasFlag(CronExpressionFlag.DayOfWeekLast) && !IsLastDayOfWeek(day, lastDayOfMonth))
            {
                second = minSecond;
                minute = minMinute;
                hour = minHour;
                day++;

                goto RetryDayMonth;
            }

            // # character.

            if (_nthdayOfWeek != 0 && !IsNthDayOfWeek(day, _nthdayOfWeek))
            {
                second = minSecond;
                minute = minMinute;
                hour = minHour;
                day++;

                goto RetryDayMonth;
            }

            return new DateTime(year, month, day, hour, minute, second);
        }

        private DateTimeOffset GetDstTransitionEndDateTime(TimeZoneInfo zone, DateTime ambiguousDateTime, TimeSpan dstOffset)
        {
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
        }

        private DateTimeOffset GetDstTransitionStartDateTime(TimeZoneInfo zone, DateTime invalidDateTime, TimeSpan baseOffset)
        {
            var dstTransitionDateTime = invalidDateTime;

            while (zone.IsInvalidTime(dstTransitionDateTime))
            {
                dstTransitionDateTime = dstTransitionDateTime.AddMinutes(-1);
            }

            while (!zone.IsInvalidTime(dstTransitionDateTime))
            {
                dstTransitionDateTime = dstTransitionDateTime.AddSeconds(1);
            }

            while (zone.IsInvalidTime(dstTransitionDateTime))
            {
                dstTransitionDateTime = dstTransitionDateTime.AddMilliseconds(-1);
            }

            dstTransitionDateTime = dstTransitionDateTime.AddMilliseconds(1);

            return new DateTimeOffset(
                dstTransitionDateTime.Year,
                dstTransitionDateTime.Month,
                dstTransitionDateTime.Day,
                dstTransitionDateTime.Hour,
                dstTransitionDateTime.Minute,
                dstTransitionDateTime.Second,
                dstTransitionDateTime.Millisecond,
                baseOffset);
        }

        private TimeSpan GetDstOffset(DateTime ambiguousDateTime, TimeZoneInfo zone)
        {
            var offsets = zone.GetAmbiguousTimeOffsets(ambiguousDateTime);

            var baseOffset = zone.BaseUtcOffset;

            for (var i = 0; i < offsets.Length; i++)
            {
                if (offsets[i] != baseOffset) return offsets[i];
            }

            throw new InvalidOperationException();
        }

        private static bool IsNthDayOfWeek(int day, int n)
        {
            return day - Constants.DaysPerWeekCount * n < Constants.FirstDayOfMonth &&
                   day - Constants.DaysPerWeekCount * (n - 1) >= Constants.FirstDayOfMonth;
        }

        private static bool IsLastDayOfWeek(int day, int lastDayOfMonth)
        {
            return day + Constants.DaysPerWeekCount > lastDayOfMonth;
        }

        private static int FindFirstSet(long value, int startBit, int endBit)
        {
            return DeBruijin.FindFirstSet(value, startBit, endBit);
        }

        private int GetNearestWeekDay(int day, DayOfWeek dayOfWeek, int lastDayOfMonth)
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

        private int GetNextDayOfMonth(int year, int month, int startDay)
        {
            if (month < Constants.FirstMonth || month > Constants.LastMonth) return -1;

            if (startDay == -1) return -1;

            var daysInMonth = Calendar.GetDaysInMonth(year, month);

            var dayOfMonthField = HasFlag(CronExpressionFlag.DayOfMonthLast)
                   ? _dayOfMonth >> (Constants.LastDayOfMonth - daysInMonth)
                   : _dayOfMonth;

            var nextDay = FindFirstSet(dayOfMonthField, startDay, daysInMonth);

            if (nextDay == -1) return -1;

            return nextDay;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasFlag(CronExpressionFlag flag)
        {
            return (_flags & flag) != 0;
        }

        private bool IsMatch(int second, int minute, int hour, int dayOfMonth, int month, int dayOfWeek, int year)
        {
            var daysInMonth = Calendar.GetDaysInMonth(year, month);

            var dayOfMonthField = HasFlag(CronExpressionFlag.DayOfMonthLast)
                    ? _dayOfMonth >> (Constants.LastDayOfMonth - daysInMonth)
                    : _dayOfMonth;

            if (HasFlag(CronExpressionFlag.DayOfMonthLast) && !_nearestWeekday)
            {
                if (!GetBit(dayOfMonthField, dayOfMonth)) return false;
            }
            else if (HasFlag(CronExpressionFlag.DayOfWeekLast))
            {
                if (!IsLastDayOfWeek(dayOfMonth, daysInMonth)) return false;
            }
            else if (_nthdayOfWeek != 0)
            {
                if(!IsNthDayOfWeek(dayOfMonth, _nthdayOfWeek)) return false;
            }
            else if (_nearestWeekday)
            {
                var isDayMatched = GetBit(dayOfMonthField, dayOfMonth) && dayOfWeek > 0 && dayOfWeek < 6 ||
                                   GetBit(dayOfMonthField, dayOfMonth - 1) && dayOfWeek == 1 ||
                                   GetBit(dayOfMonthField, dayOfMonth + 1) && dayOfWeek == 5 ||
                                   GetBit(dayOfMonthField, 1) && dayOfWeek == 1 && (dayOfMonth == 2 || dayOfMonth == 3) ||
                                   GetBit(dayOfMonthField, dayOfMonth + 2) && dayOfMonth == daysInMonth - 2 && dayOfWeek == 5;

                if (!isDayMatched) return false;
            }

            // Make 0-based values out of these so we can use them as indicies
            // minute -= Constants.FirstMinute;
            //  hour -= Constants.FirstHour;
            // dayOfMonth -= Constants.FirstDayOfMonth;
            //  month -= Constants.FirstMonth;
            // dayOfWeek -= Constants.FirstDayOfWeek;

            // The dom/dow situation is:  
            //     "* * 1,15 * Sun" will run on the first and fifteenth *only* on Sundays; 
            //     "* * * * Sun" will run *only* on Sundays; 
            //     "* * 1,15 * *" will run *only* the 1st and 15th.
            // this is why we keep DayOfMonthStar and DayOfWeekStar.
            return GetBit(_second, second) &&
                   GetBit(_minute, minute) &&
                   GetBit(_hour, hour) &&
                   GetBit(_month, month) &&
                   GetBit(_dayOfWeek, dayOfWeek) &&
                   (_nearestWeekday || GetBit(dayOfMonthField, dayOfMonth));
        }

        private bool IsMatch(DateTime dateTime)
        {
            return IsMatch(
                dateTime.Second,
                dateTime.Minute,
                dateTime.Hour,
                dateTime.Day,
                dateTime.Month,
                (int)dateTime.DayOfWeek,
                dateTime.Year);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetAllBits(out long bits)
        {
            bits = ~0L;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void SkipWhiteSpaces(ref char* pointer)
        {
            while (*pointer == '\t' || *pointer == ' ')
            {
                pointer++;
            }
        }

        private static unsafe char* GetList(
          ref long bits, /* one bit per flag, default=FALSE */
          int low, int high, /* bounds, impl. offset for bitstr */
          int[] names, /* NULL or *[] of names for these elements */
          char* pointer,
          CronFieldType cronFieldType)
        {
            var singleValue = true;
            while (true)
            {
                pointer = GetRange(ref bits, low, high, names, pointer, cronFieldType);

                if (*pointer == ',')
                {
                    singleValue = false;
                    pointer++;
                }
                else
                {
                    break;
                }
            }

            if (*pointer == 'W' && !singleValue)
            {
                throw new FormatException($"'{cronFieldType}': using some numbers with 'W' is not supported.");
            }

            SkipWhiteSpaces(ref pointer);

            return pointer;
        }

        private static unsafe char* GetRange(
            ref long bits, 
            int low, 
            int high,
            int[] names,
            char* pointer,
            CronFieldType cronFieldType)
        {
            int num1, num2, num3;
            
            if (*pointer == '*')
            {
                // '*' means "first-last" but can still be modified by /step.
                num1 = low;
                num2 = high;

                pointer++;

                if (*pointer != '/')
                {
                    SetAllBits(out bits);
                    return pointer;
                }
            }
            else if(*pointer == '?')
            {
                if (cronFieldType != CronFieldType.DayOfMonth && cronFieldType != CronFieldType.DayOfWeek)
                {
                    throw new FormatException($@"'?' is not supported for the '{cronFieldType}' field.");
                }

                pointer++;

                if (*pointer == '/') throw new FormatException($@"'{cronFieldType}': '/' is not allowed after '?'.");

                SetAllBits(out bits);
                return pointer;
            }
            else if(*pointer == 'L')
            {
                if (cronFieldType != CronFieldType.DayOfMonth) throw new FormatException($@"'L' is not supported for the '{cronFieldType}' field.");

                pointer++;

                SetBit(ref bits, Constants.LastDayOfMonth);

                if (*pointer == '-')
                {
                    // Eat the dash.
                    pointer++;

                    // Get the number following the dash.
                    if ((pointer = GetNumber(out int lastMonthOffset, 0, null, pointer)) == null || lastMonthOffset < 0 || lastMonthOffset >= high)
                    {
                        throw new FormatException($"Last month offset in '{cronFieldType}' field must be a number between {0} and {high} (all inclusive).");
                    }

                    bits = bits >> lastMonthOffset;
                }
                return pointer;
            }
            else
            {
                if ((pointer = GetNumber(out num1, low, names, pointer)) == null || num1 < low || num1 > high)
                {
                    throw new FormatException($"Value of '{cronFieldType}' field must be a number between {low} and {high} (all inclusive).");
                }

                if (*pointer == '-')
                {
                    // Eat the dash.
                    pointer++;

                    // Get the number following the dash.
                    if ((pointer = GetNumber(out num2, low, names, pointer)) == null || num2 < low || num2 > high)
                    {
                        throw new FormatException($"Range in '{cronFieldType}' field must contain numbers between {low} and {high} (all inclusive).");
                    }

                    if (*pointer == 'W') throw new FormatException($"'{cronFieldType}': 'W' is not allowed after '-'.");
                }
                else if (*pointer == '/')
                {
                    num2 = high;
                }
                else
                {
                    SetBit(ref bits, num1);

                    return pointer;
                }
            }

            // Check for step size.
            if (*pointer == '/')
            {
                // Eat the slash.
                pointer++;

                // Get the step size -- note: we don't pass the
                // names here, because the number is not an
                // element id, it's a step size.  'low' is
                // sent as a 0 since there is no offset either.
                if ((pointer = GetNumber(out num3, 0, null, pointer)) == null || num3 <= 0 || num3 > high)
                {
                    throw new FormatException($"Step in '{cronFieldType}' field must be a number between {1} and {high} (all inclusive).");
                }
                if (*pointer == 'W')
                {
                    throw new FormatException($"'{cronFieldType}': 'W' is not allowed after '/'.");
                }
            }
            else
            {
                // No step.  Default==1.
                num3 = 1;
            }

            // If upper bound less than bottom one, e.g. range 55-10 specified
            // we'll set bits from 0 to 15 then we shift it right by 5 bits.
            int shift = 0;
            if (num2 < num1)
            {
                // Skip one of sundays.
                if (cronFieldType == CronFieldType.DayOfWeek) high--;

                shift = high - num1 + 1;
                num2 = num2 + shift;
                num1 = low;
            }

            // Range. set all elements from num1 to num2, stepping
            // by num3.
            for (var i = num1; i <= num2; i += num3)
            {
                SetBit(ref bits, i);
            }

            // If we have range like 55-10 or 11-1, so num2 > num1 we have to shift bits right.
            bits = shift == 0 
                ? bits 
                : bits >> shift | bits << (high - low - shift + 1);

            return pointer;
        }

        private static unsafe char* GetNumber(
            out int num, /* where does the result go? */
            int low, /* offset applied to result if symbolic enum used */
            int[] names, /* symbolic names, if any, for enums */
            char* pointer)
        {
            num = 0;

            if (IsDigit(*pointer))
            {
                num = GetNumeric(*pointer++);

                if (!IsDigit(*pointer)) return pointer;

                num = num * 10 + GetNumeric(*pointer++);

                if (!IsDigit(*pointer)) return pointer;

                return null;
            }

            if (names == null) return null;

            if (!IsLetter(*pointer)) return null;
            var buffer = ToUpper(*pointer++);

            if (!IsLetter(*pointer)) return null;
            buffer |= ToUpper(*pointer++) << 8;

            if (!IsLetter(*pointer)) return null;
            buffer |= ToUpper(*pointer++) << 16;

            if (IsLetter(*pointer)) return null;

            var length = names.Length;

            for (var i = 0; i < length; i++)
            {
                if (buffer == names[i])
                {
                    num = i + low;
                    return pointer;
                }
            }

            return null;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool GetBit(long value, int index)
        {
            return (value & (1L << index)) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SetBit(ref long value, int index)
        {
            value |= 1L << index;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsDigit(int code)
        {
            return code >= 48 && code <= 57;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsLetter(int code)
        {
            return (code >= 65 && code <= 90) || (code >= 97 && code <= 122);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int GetNumeric(int code)
        {
            return code - 48;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ToUpper(int code)
        {
            if (code >= 97 && code <= 122)
            {
                return code - 32;
            }

            return code;
        }

        private static unsafe int CountFields(char* pointer)
        {
            int length = 0;
            CheckWhiteSpace:

            if (*pointer == '\t' ||  *pointer == ' ')
            {
                pointer++;
                goto CheckWhiteSpace;
            }

            if (*pointer == '\0') return length;

            CheckNotWhiteSpace:

            if (*pointer != '\t' && *pointer != ' ' && *pointer != '\0')
            {
                pointer++;
                goto CheckNotWhiteSpace;
            }

            length++;

            if (*pointer == '\0') return length;

            goto CheckWhiteSpace;
        }
    }
}
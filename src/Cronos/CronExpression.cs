using System;
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

        private CronExpressionFlag _flags;

        private CronExpression()
        {
        }

        ///<summary>
        /// Constructs a new <see cref="CronExpression"/> based on the specified
        /// cron expression. It's supported expressions consisting of 5 fields:
        /// minute, hour, day of month, month, day of week. 
        /// If you want to parse non-standard cron expresions use <see cref="Parse(string, CronFields)"/> with specified CronFields argument.
        /// See more: <a href="https://github.com/HangfireIO/Cronos">https://github.com/HangfireIO/Cronos</a>
        /// </summary>
        public static CronExpression Parse(string expression)
        {
            return Parse(expression, CronFields.Standard);
        }

        ///<summary>
        /// Constructs a new <see cref="CronExpression"/> based on the specified
        /// cron expression. It's supported expressions consisting of 5 or 6 fields:
        /// second (optional), minute, hour, day of month, month, day of week. 
        /// See more: <a href="https://github.com/HangfireIO/Cronos">https://github.com/HangfireIO/Cronos</a>
        /// </summary>
        public static CronExpression Parse(string expression, CronFields fields)
        {
            if (string.IsNullOrEmpty(expression)) throw new ArgumentNullException(nameof(expression));

            var cronExpression = new CronExpression();

            unsafe
            {
                fixed (char* value = expression)
                {
                    var pointer = value;

                    SkipWhiteSpaces(ref pointer);

                    // Second.

                    if ((fields & CronFields.IncludeSeconds) != 0)
                    {
                        ParseField(CronField.Second, ref pointer, cronExpression, ref cronExpression._second);
                    }
                    else
                    {
                        SetBit(ref cronExpression._second, 0);
                    }

                    // Minute.
                    ParseField(CronField.Minute, ref pointer, cronExpression, ref cronExpression._minute);

                    // Hour.
                    ParseField(CronField.Hour, ref pointer, cronExpression, ref cronExpression._hour);

                    // Day of month.
                    ParseField(CronField.DayOfMonth, ref pointer, cronExpression, ref cronExpression._dayOfMonth);

                    // Month.
                    ParseField(CronField.Month, ref pointer, cronExpression, ref cronExpression._month);

                    // Day of week.
                    if (*pointer == '?' && cronExpression.HasFlag(CronExpressionFlag.DayOfMonthQuestion))
                    {
                        ThrowFormatException("'{0}': '?' is not supported.", CronField.DayOfWeek);
                    }

                    ParseField(CronField.DayOfWeek, ref pointer, cronExpression, ref cronExpression._dayOfWeek);

                    if (*pointer != '\0')
                    {
                        ThrowFormatException("Unexpected character '{0}' on position {1}, end of string expected. Please use the 'fields' argument to specify non-standard CRON fields.", *pointer, pointer - value);
                    }
                    
                    // Make sundays equivalent.
                    if ((cronExpression._dayOfWeek & Constants.SundayBits) != 0)
                    {
                        cronExpression._dayOfWeek |= Constants.SundayBits;
                    }

                    return cronExpression;
                }
            }
        }

        /// <summary>
        /// Calculates the first occurrence starting with <paramref name="startInclusive"/> and 
        /// up to <paramref name="endInclusive"/> in given <paramref name="zone"/>.
        /// </summary>
        public DateTimeOffset? GetOccurrence(DateTimeOffset startInclusive, DateTimeOffset endInclusive, TimeZoneInfo zone)
        {
            // TODO: DateTime kind
            if (zone.Equals(TimeZoneInfo.Utc))
            {
                var found = GetOccurrence(startInclusive.UtcDateTime, endInclusive.UtcDateTime);

                return found != null
                    ? new DateTimeOffset(found.Value, TimeSpan.Zero)
                    : (DateTimeOffset?) null;
            }

            var zonedStart = TimeZoneInfo.ConvertTime(startInclusive, zone);
            var zonedEnd = TimeZoneInfo.ConvertTime(endInclusive, zone);

            return GetOccurenceByZonedTimes(zonedStart, zonedEnd, zone);
        }

        /// <summary>
        /// Calculates the first occurrence starting with <paramref name="utcStartInclusive"/> and 
        /// up to <paramref name="utcEndInclusive"/> in given <paramref name="zone"/>.
        /// </summary>
        /// <exception cref="ArgumentException">The <see cref="DateTime.Kind"/> property of <paramref name="utcStartInclusive"/> or <paramref name="utcEndInclusive"/> 
        /// is not <see cref="DateTimeKind.Utc"/>.
        /// </exception>
        public DateTimeOffset? GetOccurrence(DateTime utcStartInclusive, DateTime utcEndInclusive, TimeZoneInfo zone)
        {
            void CheckDateTimeArgument(string argName, DateTime dateTime, DateTimeKind expectedKind)
            {
                if (dateTime.Kind != expectedKind)
                {
                    throw new ArgumentException($@"The supplied DateTime must have the Kind property set to {expectedKind}", argName);
                }
            }

            CheckDateTimeArgument(nameof(utcStartInclusive), utcStartInclusive, DateTimeKind.Utc);
            CheckDateTimeArgument(nameof(utcEndInclusive), utcEndInclusive, DateTimeKind.Utc);

            if (zone.Equals(TimeZoneInfo.Utc))
            {
                var found = GetOccurrence(utcStartInclusive, utcEndInclusive);

                return found != null
                    ? new DateTimeOffset(found.Value, TimeSpan.Zero)
                    : (DateTimeOffset?)null;
            }

            var zonedStart = TimeZoneInfo.ConvertTime((DateTimeOffset)utcStartInclusive, zone);
            var zonedEnd = TimeZoneInfo.ConvertTime((DateTimeOffset)utcEndInclusive, zone);

            return GetOccurenceByZonedTimes(zonedStart, zonedEnd, zone);
        }

        private DateTimeOffset? GetOccurenceByZonedTimes(DateTimeOffset zonedStartInclusive, DateTimeOffset zonedEndInclusive, TimeZoneInfo timeZone)
        {
            var startLocalDateTime = zonedStartInclusive.DateTime;
            var endLocalDateTime = zonedEndInclusive.DateTime;

            var currentOffset = zonedStartInclusive.Offset;

            if (IsMatch(startLocalDateTime))
            {
                if (timeZone.IsInvalidTime(startLocalDateTime))
                {
                    var nextValidTime = TimeZoneHelper.GetDstStartDateTime(timeZone, startLocalDateTime, timeZone.BaseUtcOffset);

                    return nextValidTime;
                }
                if (TimeZoneHelper.IsAmbiguousTime(timeZone, startLocalDateTime))
                {
                    // Ambiguous.

                    // Interval jobs should be fired in both offsets.
                    // TODO: Will "15/10" fire in both offsets?
                    if (HasFlag(CronExpressionFlag.Interval))
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
                    return zonedStartInclusive;
                }
            }

            if (TimeZoneHelper.IsAmbiguousTime(timeZone, startLocalDateTime))
            {
                TimeSpan lateOffset = timeZone.BaseUtcOffset;

                TimeSpan earlyOffset = TimeZoneHelper.GetDstOffset(startLocalDateTime, timeZone);

                if (earlyOffset == currentOffset)
                {
                    var dstTransitionEndDateTimeOffset = TimeZoneHelper.GetDstTransitionEndDateTime(timeZone, startLocalDateTime, earlyOffset);

                    var earlyIntervalLocalEnd = dstTransitionEndDateTimeOffset.AddSeconds(-1).DateTime;

                    // Current period, try to find anything here.
                    var found = GetOccurrence(startLocalDateTime, earlyIntervalLocalEnd);

                    if (found.HasValue)
                    {
                        return GetOccurenceByZonedTimes(new DateTimeOffset(found.Value, currentOffset), zonedEndInclusive, timeZone);
                    }

                    var lateIntervalLocalStart = dstTransitionEndDateTimeOffset.ToOffset(lateOffset).DateTime;

                    //Try to find anything starting from late offset.
                    found = GetOccurrence(lateIntervalLocalStart, endLocalDateTime);

                    if (found.HasValue)
                    {
                        return GetOccurenceByZonedTimes(new DateTimeOffset(found.Value, lateOffset), zonedEndInclusive, timeZone);
                    }
                }
            }

            // Does not match, find next.
            var nextFound = GetOccurrence(startLocalDateTime.AddSeconds(1), endLocalDateTime);

            if (nextFound == null) return null;

            var zoneOffset = timeZone.GetUtcOffset(nextFound.Value);

            return GetOccurenceByZonedTimes(new DateTimeOffset(nextFound.Value, zoneOffset), zonedEndInclusive, timeZone);
        }

        private DateTime? GetOccurrence(DateTime baseTime, DateTime endTime)
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

            var dayOfWeek = CalendarHelper.GetDayOfWeek(new DateTime(year, month, day));
            var lastDayOfMonth = CalendarHelper.GetDaysInMonth(year, month);

            // W character.

            if (HasFlag(CronExpressionFlag.NearestWeekday))
            {
                var nearestWeekDay = CalendarHelper.GetNearestWeekDay(day, dayOfWeek, lastDayOfMonth);

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

                if (HasFlag(CronExpressionFlag.DayOfWeekLast) && !CalendarHelper.IsLastDayOfWeek(nearestWeekDay, lastDayOfMonth))
                {
                    day = -1;
                }

                if (_nthdayOfWeek != 0 && !CalendarHelper.IsNthDayOfWeek(nearestWeekDay, _nthdayOfWeek))
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

            if (HasFlag(CronExpressionFlag.DayOfWeekLast) && !CalendarHelper.IsLastDayOfWeek(day, lastDayOfMonth))
            {
                second = minSecond;
                minute = minMinute;
                hour = minHour;
                day++;

                goto RetryDayMonth;
            }

            // # character.

            if (_nthdayOfWeek != 0 && !CalendarHelper.IsNthDayOfWeek(day, _nthdayOfWeek))
            {
                second = minSecond;
                minute = minMinute;
                hour = minHour;
                day++;

                goto RetryDayMonth;
            }

            return new DateTime(year, month, day, hour, minute, second);
        }

        private static int FindFirstSet(long value, int startBit, int endBit)
        {
            return DeBruijin.FindFirstSet(value, startBit, endBit);
        }

        private int GetNextDayOfMonth(int year, int month, int startDay)
        {
            if (month < Constants.FirstMonth || month > Constants.LastMonth) return -1;

            if (startDay == -1) return -1;

            var daysInMonth = CalendarHelper.GetDaysInMonth(year, month);

            var dayOfMonthField = HasFlag(CronExpressionFlag.DayOfMonthLast)
                   ? _dayOfMonth >> (Constants.LastDayOfMonth - daysInMonth)
                   : _dayOfMonth;

            var nextDay = FindFirstSet(dayOfMonthField, startDay, daysInMonth);

            if (nextDay == -1) return -1;

            return nextDay;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasFlag(CronExpressionFlag value)
        {
            return (_flags & value) != 0;
        }

        private bool IsMatch(int millisecond, int second, int minute, int hour, int dayOfMonth, int month, int dayOfWeek, int year)
        {
            if (millisecond != 0) return false;

            var daysInMonth = CalendarHelper.GetDaysInMonth(year, month);

            var dayOfMonthField = HasFlag(CronExpressionFlag.DayOfMonthLast)
                    ? _dayOfMonth >> (Constants.LastDayOfMonth - daysInMonth)
                    : _dayOfMonth;

            if (HasFlag(CronExpressionFlag.DayOfMonthLast) && !HasFlag(CronExpressionFlag.NearestWeekday))
            {
                if (!GetBit(dayOfMonthField, dayOfMonth)) return false;
            }
            else if (HasFlag(CronExpressionFlag.DayOfWeekLast))
            {
                if (!CalendarHelper.IsLastDayOfWeek(dayOfMonth, daysInMonth)) return false;
            }
            else if (_nthdayOfWeek != 0)
            {
                if(!CalendarHelper.IsNthDayOfWeek(dayOfMonth, _nthdayOfWeek)) return false;
            }
            else if (HasFlag(CronExpressionFlag.NearestWeekday))
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
                   (HasFlag(CronExpressionFlag.NearestWeekday) || GetBit(dayOfMonthField, dayOfMonth));
        }

        private bool IsMatch(DateTime dateTime)
        {
            return IsMatch(
                dateTime.Millisecond,
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

        private static unsafe void ParseField(CronField field, ref char* pointer, CronExpression expression, ref long bits)
        {
            if (*pointer == '*')
            {
                pointer++;

                if (field == CronField.Second || field == CronField.Minute || field == CronField.Hour)
                {
                    expression._flags |= CronExpressionFlag.Interval;
                }

                if (*pointer != '/')
                {
                    SetAllBits(out bits);
                }
                else
                {
                    ParseRange(field, ref pointer, expression, ref bits, true);
                }
            }
            else
            {
                ParseList(field, ref pointer, expression, ref bits);
            }

            if (field == CronField.DayOfMonth)
            {
                if (*pointer == 'W')
                {
                    pointer++;
                    expression._flags |= CronExpressionFlag.NearestWeekday;
                }
            }
            else if (field == CronField.DayOfWeek)
            {
                if (*pointer == 'L')
                {
                    pointer++;
                    expression._flags |= CronExpressionFlag.DayOfWeekLast;
                }

                if (*pointer == '#')
                {
                    pointer++;
                    pointer = GetNumber(out expression._nthdayOfWeek, Constants.MinNthDayOfWeek, null, pointer);

                    if (pointer == null || expression._nthdayOfWeek < Constants.MinNthDayOfWeek || expression._nthdayOfWeek > Constants.MaxNthDayOfWeek)
                    {
                        ThrowFormatException("'#' must be followed by a number between {0} and {1}.", Constants.MinNthDayOfWeek, Constants.MaxNthDayOfWeek);
                    }
                }
            }

            SkipWhiteSpaces(ref pointer);
        }

        private static unsafe void ParseList(CronField field, ref char* pointer, CronExpression expression, ref long bits)
        {
            var singleValue = true;
            while (true)
            {
                ParseRange(field, ref pointer, expression, ref bits, false);

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
                ThrowFormatException("'{0}': using some numbers with 'W' is not supported.", field);
            }
        }

        private static unsafe void ParseRange(CronField field, ref char* pointer, CronExpression expression, ref long bits, bool star)
        {
            int num1, num2, num3;

            var low = Constants.FirstValues[(int) field];
            var high = Constants.LastValues[(int) field];
            
            if (star)
            {
                num1 = low;
                num2 = high;
            }
            else if(*pointer == '?')
            {
                if (field != CronField.DayOfMonth && field != CronField.DayOfWeek)
                {
                    ThrowFormatException("'?' is not supported for the '{0}' field.", field);
                }

                pointer++;

                if (field == CronField.DayOfMonth)
                {
                    expression._flags |= CronExpressionFlag.DayOfMonthQuestion;
                }

                if (*pointer == '/')
                {
                    ThrowFormatException("'{0}': '/' is not allowed after '?'.", field);
                }

                SetAllBits(out bits);
                return;
            }
            else if(*pointer == 'L')
            {
                if (field != CronField.DayOfMonth)
                {
                    ThrowFormatException("'L' is not supported for the '{0}' field.", field);
                }

                pointer++;

                SetBit(ref bits, Constants.LastDayOfMonth);

                expression._flags |= CronExpressionFlag.DayOfMonthLast;

                if (*pointer == '-')
                {
                    // Eat the dash.
                    pointer++;

                    // Get the number following the dash.
                    if ((pointer = GetNumber(out int lastMonthOffset, 0, null, pointer)) == null || lastMonthOffset < 0 || lastMonthOffset >= high)
                    {
                        ThrowFormatException("Last month offset in '{0}' field must be a number between {1} and {2} (all inclusive).", field, low, high);
                    }

                    bits = bits >> lastMonthOffset;
                }
                return;
            }
            else
            {
                var names = Constants.NameArrays[(int) field];

                if ((pointer = GetNumber(out num1, low, names, pointer)) == null || num1 < low || num1 > high)
                {
                    ThrowFormatException("Value of '{0}' field must be a number between {1} and {2} (all inclusive).", field, low, high);
                }

                if (*pointer == '-')
                {
                    // Eat the dash.
                    pointer++;

                    // Get the number following the dash.
                    if ((pointer = GetNumber(out num2, low, names, pointer)) == null || num2 < low || num2 > high)
                    {
                        ThrowFormatException("Range in '{0}' field must contain numbers between {1} and {2} (all inclusive).", field, low, high);
                    }

                    if (*pointer == 'W')
                    {
                        ThrowFormatException("'{0}': 'W' is not allowed after '-'.", field);
                    }
                }
                else if (*pointer == '/')
                {
                    // TODO: Why?
                    num2 = high;
                }
                else
                {
                    SetBit(ref bits, num1);
                    return;
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
                    ThrowFormatException("Step in '{0}' field must be a number between 1 and {1} (all inclusive).", field, high);
                }
                if (*pointer == 'W')
                {
                    ThrowFormatException("'{0}': 'W' is not allowed after '/'.", field);
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
                if (field == CronField.DayOfWeek) high--;

                shift = high - num1 + 1;
                num2 = num2 + shift;
                num1 = low;
            }

            // Range. set all elements from num1 to num2, stepping
            // by num3.
            if (num3 == 1 && num1 < num2 + 1)
            {
                // Fast path, to set all the required bits at once.
                bits |= (1L << (num2 + 1)) - (1L << num1);
            }
            else
            {
                for (var i = num1; i <= num2; i += num3)
                {
                    SetBit(ref bits, i);
                }
            }

            // If we have range like 55-10 or 11-1, so num2 > num1 we have to shift bits right.
            bits = shift == 0 
                ? bits 
                : bits >> shift | bits << (high - low - shift + 1);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowFormatException(string format, params object[] args)
        {
            throw new FormatException(String.Format(format, args));
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
    }
}
using System;
using System.Runtime.CompilerServices;

namespace Cronos
{
    /// <summary>
    /// Provides a parser and scheduler for cron expressions.
    /// </summary>
    public sealed class CronExpression
    {
        private static readonly TimeZoneInfo UtcTimeZone = TimeZoneInfo.Utc;

        private long _second;     // 60 bits -> from 0 bit to 59 bit in Int64
        private long _minute;     // 60 bits -> from 0 bit to 59 bit in Int64
        private long _hour;       // 24 bits -> from 0 bit to 23 bit in Int64
        private long _dayOfMonth; // 31 bits -> from 1 bit to 31 bit in Int64
        private long _month;      // 12 bits -> from 1 bit to 12 bit in Int64
        private long _dayOfWeek;  // 8 bits  -> from 0 bit to 7  bit in Int64

        private int _nthdayOfWeek;
        private int _lastMonthOffset;

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
            if (zone == UtcTimeZone)
            {
                if (IsMatch(startInclusive.UtcDateTime)) return startInclusive.ToOffset(TimeSpan.Zero);

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
            CheckDateTimeArgument(nameof(utcStartInclusive), utcStartInclusive, DateTimeKind.Utc);
            CheckDateTimeArgument(nameof(utcEndInclusive), utcEndInclusive, DateTimeKind.Utc);

            if (zone == UtcTimeZone)
            {
                if (IsMatch(utcStartInclusive)) return utcStartInclusive;

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
            DateTimeHelper.FillDateTimeParts(
                baseTime, 
                out int baseSecond, 
                out int baseMinute, 
                out int baseHour, 
                out int baseDay, 
                out int baseMonth, 
                out int baseYear);

            var year = baseYear;
            var month = baseMonth;
            var day = baseDay;
            var hour = baseHour;
            var minute = baseMinute;
            var second = baseSecond;

            var minSecond = FindFirstSet(CronField.Second, _second, Constants.FirstSecond, Constants.LastSecond);
            var minMinute = FindFirstSet(CronField.Minute,_minute, Constants.FirstMinute, Constants.LastMinute);
            var minHour = FindFirstSet(CronField.Hour, _hour, Constants.FirstHour, Constants.LastHour);
            var minDay = FindFirstSet(CronField.DayOfMonth, _dayOfMonth, Constants.FirstDayOfMonth, Constants.LastDayOfMonth);
            var minMonth = FindFirstSet(CronField.Month, _month, Constants.FirstMonth, Constants.LastMonth);

            void Rollover(CronField field, bool increment = true)
            {
                if (field == CronField.Hour)
                {
                    second = minSecond;
                    minute = minMinute;
                    hour = minHour;
                    if(increment) day++;
                }
                else if (field == CronField.Second)
                {
                    second = minSecond;
                    if (increment) minute++;
                }
                else if (field == CronField.Minute)
                {
                    second = minSecond;
                    minute = minMinute;
                    if (increment) hour++;
                }
                else if (field == CronField.DayOfMonth)
                {
                    second = minSecond;
                    minute = minMinute;
                    hour = minHour;
                    day = minDay;
                    if (increment) month++;
                }
                else if (field == CronField.Month)
                {
                    second = minSecond;
                    minute = minMinute;
                    hour = minHour;
                    day = minDay;
                    month = minMonth;
                    if (increment) year++;
                }
            }

            void MoveToNextValue(CronField field, long fieldBits, ref int value)
            {
                var nextValue = FindFirstSet(field, fieldBits, value, Constants.LastValues[(int) field]);

                if (nextValue == value) return;

                if (nextValue == -1)
                {
                    Rollover(field);
                    return;
                }

                Rollover(field - 1, false);
                value = nextValue;
            }

            MoveToNextValue(CronField.Second, _second, ref second);
            MoveToNextValue(CronField.Minute, _minute, ref minute);
            MoveToNextValue(CronField.Hour, _hour, ref hour);

            RetryDayOfMonth:

            MoveToNextValue(CronField.DayOfMonth, _dayOfMonth, ref day);

            RetryMonth:

            MoveToNextValue(CronField.Month, _month, ref month);

            var lastDayOfMonth = CalendarHelper.GetDaysInMonth(year, month);

            if (day > lastDayOfMonth)
            {
                if (new DateTime(year, month, lastDayOfMonth, hour, minute, second) > endTime) return null;

                Rollover(CronField.DayOfMonth);
                goto RetryDayOfMonth;
            }

            if (new DateTime(year, month, day, hour, minute, second) > endTime) return null;

            if (!HasFlag(CronExpressionFlag.LongPath))
            {
                if (IsDayOfWeekMatch(year, month, day))
                {
                    return new DateTime(year, month, day, hour, minute, second);
                }

                Rollover(CronField.Hour);
                goto RetryDayOfMonth;
            }

            if (HasFlag(CronExpressionFlag.DayOfMonthLast))
            {
                var lastDayMonthWithOffset = lastDayOfMonth - _lastMonthOffset;

                if (lastDayMonthWithOffset > day)
                {
                    Rollover(CronField.Hour, false);
                    day = lastDayMonthWithOffset;
                }
                else if(lastDayMonthWithOffset < day)
                {
                    Rollover(CronField.DayOfMonth);
                    goto RetryDayOfMonth;
                }

                if (!IsDayOfWeekMatch(year, month, day))
                {
                    Rollover(CronField.Hour);
                    goto RetryDayOfMonth;
                }

                if (new DateTime(year, month, day, hour, minute, second) > endTime) return null;
            }

            // W character.

            if (HasFlag(CronExpressionFlag.NearestWeekday))
            {
                var dayOfWeek = CalendarHelper.GetDayOfWeek(new DateTime(year, month, day));

                var shift = CalendarHelper.MoveToNearestWeekDay(ref day, ref dayOfWeek, lastDayOfMonth);

                if (shift > 0)
                {
                    Rollover(CronField.Hour, false);
                }
                else if (shift < 0)
                {
                    if (new DateTime(year, month, day) < new DateTime(baseYear, baseMonth, baseDay))
                    {
                        Rollover(CronField.DayOfMonth);
                        goto RetryMonth;
                    }

                    if (new DateTime(year, month, day) == new DateTime(baseYear, baseMonth, baseDay))
                    {
                        hour = baseHour;
                        minute = baseMinute;
                        second = baseSecond;

                        MoveToNextValue(CronField.Second, _second, ref second);
                        MoveToNextValue(CronField.Minute, _minute, ref minute);
                        MoveToNextValue(CronField.Hour, _hour, ref hour);

                        if (day == -1 || day != baseDay)
                        {
                            Rollover(CronField.DayOfMonth);
                            goto RetryMonth;
                        }
                    }
                }

                if (!IsDayOfWeekMatch(dayOfWeek) ||
                    HasFlag(CronExpressionFlag.DayOfWeekLast) && !CalendarHelper.IsLastDayOfWeek(day, lastDayOfMonth) ||
                    HasFlag(CronExpressionFlag.NthDayOfWeek) && !CalendarHelper.IsNthDayOfWeek(day, _nthdayOfWeek))
                {
                    Rollover(CronField.DayOfMonth);

                    goto RetryMonth;
                }

                if (new DateTime(year, month, day, hour, minute, second) > endTime) return null;
            }

            // L and # characters in day of week.

            if (!IsDayOfWeekMatch(year, month, day) ||
                HasFlag(CronExpressionFlag.DayOfWeekLast) && !CalendarHelper.IsLastDayOfWeek(day, lastDayOfMonth) ||
                HasFlag(CronExpressionFlag.NthDayOfWeek) && !CalendarHelper.IsNthDayOfWeek(day, _nthdayOfWeek))
            {
                Rollover(CronField.Hour);

                goto RetryDayOfMonth;
            }

            return new DateTime(year, month, day, hour, minute, second);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsDayOfWeekMatch(int year, int month, int day)
        {
            if (_dayOfWeek == Constants.AllBits[(int)CronField.DayOfWeek]) return true;

            var dayOfWeek = CalendarHelper.GetDayOfWeek(new DateTime(year, month, day));
            return ((_dayOfWeek >> (int)dayOfWeek) & 1) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsDayOfWeekMatch(DayOfWeek dayOfWeek)
        {
            return ((_dayOfWeek >> (int)dayOfWeek) & 1) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindFirstSet(CronField field, long value, int startBit, int endBit)
        {
            if (GetBit(value, startBit)) return startBit;

            return DeBruijin.FindFirstSet(value, startBit, endBit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasFlag(CronExpressionFlag value)
        {
            return (_flags & value) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsMatch(int millisecond, int second, int minute, int hour, int dayOfMonth, int month, int dayOfWeek, int year)
        {
            if (millisecond != 0) return false;

            var daysInMonth = CalendarHelper.GetDaysInMonth(year, month);

            if (!HasFlag(CronExpressionFlag.LongPath))
            {
                return GetBit(_second, second) &&
                       GetBit(_minute, minute) &&
                       GetBit(_hour, hour) &&
                       GetBit(_month, month) &&
                       GetBit(_dayOfWeek, dayOfWeek) &&
                       GetBit(_dayOfMonth, dayOfMonth);
            }

            var dayOfMonthField = HasFlag(CronExpressionFlag.DayOfMonthLast)
                    ? 1L << (daysInMonth - _lastMonthOffset)
                    : _dayOfMonth;

            if (HasFlag(CronExpressionFlag.DayOfMonthLast) && !HasFlag(CronExpressionFlag.NearestWeekday))
            {
                if (!GetBit(dayOfMonthField, dayOfMonth)) return false;
            }
            else if (HasFlag(CronExpressionFlag.DayOfWeekLast))
            {
                if (!CalendarHelper.IsLastDayOfWeek(dayOfMonth, daysInMonth)) return false;
            }
            else if (HasFlag(CronExpressionFlag.NthDayOfWeek))
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
            return GetBit(_second, second) &&
                   GetBit(_minute, minute) &&
                   GetBit(_hour, hour) &&
                   GetBit(_month, month) &&
                   GetBit(_dayOfWeek, dayOfWeek) &&
                   (HasFlag(CronExpressionFlag.NearestWeekday) || GetBit(dayOfMonthField, dayOfMonth));
        }

        private bool IsMatch(DateTime dateTime)
        {
            var millisecond = dateTime.Millisecond;

            if (millisecond != 0) return false;

            DateTimeHelper.FillDateTimeParts(dateTime, out int second, out int minute, out int hour, out int dayOfMonth, out int month, out int year);

            var dayOfWeek = (int)dateTime.DayOfWeek;

            if (!HasFlag(CronExpressionFlag.LongPath))
            {
                return GetBit(_second, second) &&
                       GetBit(_minute, minute) &&
                       GetBit(_hour, hour) &&
                       GetBit(_month, month) &&
                       GetBit(_dayOfWeek, dayOfWeek) &&
                       GetBit(_dayOfMonth, dayOfMonth);
            }

            var daysInMonth = CalendarHelper.GetDaysInMonth(year, month);

            var dayOfMonthField = HasFlag(CronExpressionFlag.DayOfMonthLast)
                ? 1L << (daysInMonth - _lastMonthOffset)
                : _dayOfMonth;

            if (HasFlag(CronExpressionFlag.DayOfMonthLast) && !HasFlag(CronExpressionFlag.NearestWeekday))
            {
                if (!GetBit(dayOfMonthField, dayOfMonth)) return false;
            }
            else if (HasFlag(CronExpressionFlag.DayOfWeekLast))
            {
                if (!CalendarHelper.IsLastDayOfWeek(dayOfMonth, daysInMonth)) return false;
            }
            else if (HasFlag(CronExpressionFlag.NthDayOfWeek))
            {
                if (!CalendarHelper.IsNthDayOfWeek(dayOfMonth, _nthdayOfWeek)) return false;
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
                    SetAllBits(field, out bits);

                    SkipWhiteSpaces(ref pointer);

                    return;
                }

                ParseRange(field, ref pointer, expression, ref bits, true);
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
                    expression._flags |= CronExpressionFlag.NthDayOfWeek;
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

                SetAllBits(field, out bits);

                return;
            }
            else if(*pointer == 'L')
            {
                if (field != CronField.DayOfMonth)
                {
                    ThrowFormatException("'L' is not supported for the '{0}' field.", field);
                }

                pointer++;

                bits = 0b1111 << Constants.MinDaysInMonth;

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
                    expression._lastMonthOffset = lastMonthOffset;
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
        private static void CheckDateTimeArgument(string paramName, DateTime dateTime, DateTimeKind expectedKind)
        {
            if (dateTime.Kind != expectedKind)
            {
                ThrowArgumentException(paramName, "The supplied DateTime must have the Kind property set to {0}", expectedKind);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowFormatException(string format, params object[] args)
        {
            throw new FormatException(String.Format(format, args));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowArgumentException(string paramName, string format, params object[] args)
        {
            throw new ArgumentException(String.Format(format, args), paramName);
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
        private static void SetAllBits(CronField field, out long bits)
        {
            bits = Constants.AllBits[(int)field];
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
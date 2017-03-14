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
        private const int MinDaysInMonth = 28;
        private const int MinNthDayOfWeek = 1;
        private const int MaxNthDayOfWeek = 5;
        private const int SundayBits = 0b1000_0001;

        private static readonly int[] DeBruijnPositions =
        {
            0, 1, 2, 53, 3, 7, 54, 27,
            4, 38, 41, 8, 34, 55, 48, 28,
            62, 5, 39, 46, 44, 42, 22, 9,
            24, 35, 59, 56, 49, 18, 29, 11,
            63, 52, 6, 26, 37, 40, 33, 47,
            61, 45, 43, 21, 23, 58, 17, 10,
            51, 25, 36, 32, 60, 20, 57, 16,
            50, 31, 19, 15, 30, 14, 13, 12
        };

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
        /// If you want to parse non-standard cron expresions use <see cref="Parse(string, CronFormat)"/> with specified CronFields argument.
        /// See more: <a href="https://github.com/HangfireIO/Cronos">https://github.com/HangfireIO/Cronos</a>
        /// </summary>
        public static CronExpression Parse(string expression)
        {
            return Parse(expression, CronFormat.Standard);
        }

        ///<summary>
        /// Constructs a new <see cref="CronExpression"/> based on the specified
        /// cron expression. It's supported expressions consisting of 5 or 6 fields:
        /// second (optional), minute, hour, day of month, month, day of week. 
        /// See more: <a href="https://github.com/HangfireIO/Cronos">https://github.com/HangfireIO/Cronos</a>
        /// </summary>
        public static CronExpression Parse(string expression, CronFormat format)
        {
            if (string.IsNullOrEmpty(expression)) throw new ArgumentNullException(nameof(expression));

            var cronExpression = new CronExpression();

            unsafe
            {
                fixed (char* value = expression)
                {
                    var pointer = value;

                    SkipWhiteSpaces(ref pointer);

                    if ((format & CronFormat.IncludeSeconds) != 0)
                    {
                        ParseField(CronField.Seconds, ref pointer, cronExpression, ref cronExpression._second);
                    }
                    else
                    {
                        SetBit(ref cronExpression._second, 0);
                    }

                    ParseField(CronField.Minutes, ref pointer, cronExpression, ref cronExpression._minute);

                    ParseField(CronField.Hours, ref pointer, cronExpression, ref cronExpression._hour);

                    ParseField(CronField.DaysOfMonth, ref pointer, cronExpression, ref cronExpression._dayOfMonth);

                    ParseField(CronField.Months, ref pointer, cronExpression, ref cronExpression._month);

                    if (*pointer == '?' && cronExpression.HasFlag(CronExpressionFlag.DayOfMonthQuestion))
                    {
                        ThrowFormatException("'{0}': '?' is not supported.", CronField.DaysOfWeek.ToString());
                    }

                    ParseField(CronField.DaysOfWeek, ref pointer, cronExpression, ref cronExpression._dayOfWeek);

                    if (*pointer != '\0')
                    {
                        ThrowFormatException("Unexpected character '{0}' on position {1}, end of string expected. Please use the 'fields' argument to specify non-standard CRON fields.", *pointer, pointer - value);
                    }
                    
                    // Make sundays equivalent.
                    if ((cronExpression._dayOfWeek & SundayBits) != 0)
                    {
                        cronExpression._dayOfWeek |= SundayBits;
                    }

                    return cronExpression;
                }
            }
        }

        /// <summary>
        /// Calculates the first occurrence starting with <paramref name="utcStartInclusive"/> and 
        /// up to <paramref name="utcEndInclusive"/> in given <paramref name="zone"/>.
        /// </summary>
        /// <exception cref="ArgumentException">The <see cref="DateTime.Kind"/> property of <paramref name="utcStartInclusive"/> or <paramref name="utcEndInclusive"/> 
        /// is not <see cref="DateTimeKind.Utc"/>.
        /// </exception>
        public DateTime? GetOccurrence(DateTime utcStartInclusive, DateTime utcEndInclusive, TimeZoneInfo zone)
        {
            if (utcStartInclusive.Kind != DateTimeKind.Utc) ThrowWrongDateTimeKindException(nameof(utcStartInclusive));
            if (utcEndInclusive.Kind != DateTimeKind.Utc) ThrowWrongDateTimeKindException(nameof(utcEndInclusive));

            if (zone == UtcTimeZone)
            {
                var found = GetOccurrence(utcStartInclusive, utcEndInclusive);
                if (found == null) return null;

                return DateTime.SpecifyKind(found.Value, DateTimeKind.Utc);
            }

            var zonedStart = TimeZoneInfo.ConvertTime(utcStartInclusive, zone);
            var zonedEnd = TimeZoneInfo.ConvertTime(utcEndInclusive, zone);

            var occurrence = GetOccurenceByZonedTimes(zonedStart, zonedEnd, zone);
            return occurrence?.UtcDateTime;
        }

        /// <summary>
        /// Calculates the first occurrence starting with <paramref name="startInclusive"/> and 
        /// up to <paramref name="endInclusive"/> in given <paramref name="zone"/>.
        /// </summary>
        public DateTimeOffset? GetOccurrence(DateTimeOffset startInclusive, DateTimeOffset endInclusive, TimeZoneInfo zone)
        {
            if (zone == UtcTimeZone)
            {
                var found = GetOccurrence(startInclusive.UtcDateTime, endInclusive.UtcDateTime);

                if (found == null) return null;

                return new DateTimeOffset(found.Value, TimeSpan.Zero);
            }

            var zonedStart = TimeZoneInfo.ConvertTime(startInclusive, zone);
            var zonedEnd = TimeZoneInfo.ConvertTime(endInclusive, zone);

            return GetOccurenceByZonedTimes(zonedStart, zonedEnd, zone);
        }

        private DateTimeOffset? GetOccurenceByZonedTimes(DateTimeOffset zonedStartInclusive, DateTimeOffset zonedEndInclusive, TimeZoneInfo zone)
        {
            var startLocalDateTime = zonedStartInclusive.DateTime;
            var endLocalDateTime = zonedEndInclusive.DateTime;

            if (TimeZoneHelper.IsAmbiguousTime(zone, startLocalDateTime))
            {
                var currentOffset = zonedStartInclusive.Offset;
                var lateOffset = zone.BaseUtcOffset;
               
                if (lateOffset != currentOffset)
                {
                    var earlyOffset = TimeZoneHelper.GetDstOffset(startLocalDateTime, zone);
                    var earlyIntervalLocalEnd = TimeZoneHelper.GetDstEnd(zone, startLocalDateTime, earlyOffset);

                    if (earlyIntervalLocalEnd > zonedEndInclusive) earlyIntervalLocalEnd = zonedEndInclusive;

                    // Early period, try to find anything here.
                    var found = GetOccurrence(startLocalDateTime, earlyIntervalLocalEnd.DateTime);
                    if (found.HasValue) return new DateTimeOffset(found.Value, earlyOffset);

                    startLocalDateTime = TimeZoneHelper.GetStandartTimeStart(zone, startLocalDateTime, earlyOffset).DateTime;
                }

                // Skip late ambiguous interval.
                var ambiguousTimeEnd = TimeZoneHelper.GetAmbiguousTimeEnd(zone, startLocalDateTime);

                var abmiguousTimeLastInstant = ambiguousTimeEnd <= zonedEndInclusive
                    ? ambiguousTimeEnd.DateTime.AddTicks(-1)
                    : zonedEndInclusive.DateTime;

                var foundInLateInterval = GetOccurrence(startLocalDateTime, abmiguousTimeLastInstant);

                if (foundInLateInterval.HasValue && HasFlag(CronExpressionFlag.Interval))
                    return new DateTimeOffset(foundInLateInterval.Value, lateOffset);

                startLocalDateTime = ambiguousTimeEnd.DateTime;
            }

            if (endLocalDateTime != DateTime.MaxValue && TimeZoneHelper.IsAmbiguousTime(zone, endLocalDateTime))
            {
                // When endLocalDateTime falls on ambiguous period we set endLocalDateTime to end of ambiguous period.
                // If occurrence fall on that ambiguous period we'll check if it less than zonedEndInclusive.
                var ambiguousTimeEnd = TimeZoneHelper.GetAmbiguousTimeEnd(zone, endLocalDateTime);
                endLocalDateTime = ambiguousTimeEnd.DateTime.AddTicks(-1);
            }

            var occurrence = GetOccurrence(startLocalDateTime, endLocalDateTime);
            if (occurrence == null) return null;

            if (zone.IsInvalidTime(occurrence.Value))
            {
                var nextValidTime = TimeZoneHelper.GetDstStart(zone, occurrence.Value, zone.BaseUtcOffset);
                return nextValidTime;
            }

            if (TimeZoneHelper.IsAmbiguousTime(zone, occurrence.Value))
            {
                var earlyOffset = TimeZoneHelper.GetDstOffset(occurrence.Value, zone);
                var result = new DateTimeOffset(occurrence.Value, earlyOffset);

                return result <= zonedEndInclusive ? result : (DateTimeOffset?)null;
            }

            return new DateTimeOffset(occurrence.Value, zone.GetUtcOffset(occurrence.Value));
        }

        private DateTime? GetOccurrence(DateTime baseTime, DateTime endTime)
        {
            CalendarHelper.FillDateTimeParts(
                baseTime, 
                out int baseSecond, 
                out int baseMinute, 
                out int baseHour, 
                out int baseDay, 
                out int baseMonth, 
                out int baseYear);

            CalendarHelper.FillDateTimeParts(
                endTime,
                out int endSecond,
                out int endMinute,
                out int endHour,
                out int endDay,
                out int endMonth,
                out int endYear);

            var year = baseYear;
            var month = baseMonth;
            var day = baseDay;
            var hour = baseHour;
            var minute = baseMinute;
            var second = baseSecond;

            var minSecond = FindFirstSet(_second, CronField.Seconds.First, CronField.Seconds.Last);
            var minMinute = FindFirstSet(_minute, CronField.Minutes.First, CronField.Minutes.Last);
            var minHour = FindFirstSet(_hour, CronField.Hours.First, CronField.Hours.Last);
            var minDay = FindFirstSet(_dayOfMonth, CronField.DaysOfMonth.First, CronField.DaysOfMonth.Last);
            var minMonth = FindFirstSet(_month, CronField.Months.First, CronField.Months.Last);

            void Rollover(CronField field, bool increment = true)
            {
                if (field == CronField.Seconds)
                {
                    second = minSecond;
                    if (increment) minute++;
                }
                else if (field == CronField.Minutes)
                {
                    second = minSecond;
                    minute = minMinute;
                    if (increment) hour++;
                }
                else if (field == CronField.Hours)
                {
                    second = minSecond;
                    minute = minMinute;
                    hour = minHour;
                    if (increment) day++;
                }
                else if (field == CronField.DaysOfMonth)
                {
                    second = minSecond;
                    minute = minMinute;
                    hour = minHour;
                    day = minDay;
                    if (increment) month++;
                }
                else if (field == CronField.Months)
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
                var nextValue = FindFirstSet(fieldBits, value, field.Last);
                if (nextValue == value) return;

                if (nextValue == -1)
                {
                    Rollover(field);
                    return;
                }

                Rollover(field.Previous, false);
                value = nextValue;
            }

            bool IsBeyondEndDate()
            {
                return CalendarHelper.IsLessThan(
                    endYear, endMonth, endDay, endHour, endMinute, endSecond, 
                    year, month, day, hour, minute, second);
            }

            MoveToNextValue(CronField.Seconds, _second, ref second);
            MoveToNextValue(CronField.Minutes, _minute, ref minute);
            MoveToNextValue(CronField.Hours, _hour, ref hour);

            RetryDayOfMonth:

            MoveToNextValue(CronField.DaysOfMonth, _dayOfMonth, ref day);

            RetryMonth:

            MoveToNextValue(CronField.Months, _month, ref month);

            var lastDayOfMonth = CalendarHelper.GetDaysInMonth(year, month);

            if (day > lastDayOfMonth)
            {
                day = lastDayOfMonth;

                if (IsBeyondEndDate()) return null;

                Rollover(CronField.DaysOfMonth);
                goto RetryDayOfMonth;
            }

            if (HasFlag(CronExpressionFlag.DayOfMonthLast))
            {
                var lastDayMonthWithOffset = lastDayOfMonth - _lastMonthOffset;

                if (lastDayMonthWithOffset > day)
                {
                    Rollover(CronField.Hours, false);
                    day = lastDayMonthWithOffset;
                }
                else if(lastDayMonthWithOffset < day)
                {
                    if (IsBeyondEndDate()) return null;

                    Rollover(CronField.DaysOfMonth);
                    goto RetryMonth;
                }

                if (!IsDayOfWeekMatch(year, month, day))
                {
                    if (IsBeyondEndDate()) return null;

                    Rollover(CronField.Hours);
                    goto RetryDayOfMonth;
                }
            }

            // W character.

            if (HasFlag(CronExpressionFlag.NearestWeekday))
            {
                var dayOfWeek = CalendarHelper.GetDayOfWeek(year, month, day);
                var shift = CalendarHelper.MoveToNearestWeekDay(ref day, ref dayOfWeek, lastDayOfMonth);

                if (IsBeyondEndDate()) return null;

                if (shift > 0)
                {
                    Rollover(CronField.Hours, false);
                }
                else if (shift < 0)
                {
                    if (CalendarHelper.IsLessThan(year, month, day, 0, 0, 0, baseYear, baseMonth, baseDay, 0, 0, 0))
                    {
                        Rollover(CronField.DaysOfMonth);
                        goto RetryMonth;
                    }

                    if (year == baseYear && month == baseMonth && day == baseDay)
                    {
                        hour = baseHour;
                        minute = baseMinute;
                        second = baseSecond;

                        MoveToNextValue(CronField.Seconds, _second, ref second);
                        MoveToNextValue(CronField.Minutes, _minute, ref minute);
                        MoveToNextValue(CronField.Hours, _hour, ref hour);

                        if (day == -1 || day != baseDay)
                        {
                            Rollover(CronField.DaysOfMonth);
                            goto RetryMonth;
                        }
                    }
                }

                if (IsBeyondEndDate()) return null;

                if (!IsDayOfWeekMatch(dayOfWeek) ||
                    HasFlag(CronExpressionFlag.DayOfWeekLast) && !CalendarHelper.IsLastDayOfWeek(day, lastDayOfMonth) ||
                    HasFlag(CronExpressionFlag.NthDayOfWeek) && !CalendarHelper.IsNthDayOfWeek(day, _nthdayOfWeek))
                {
                    Rollover(CronField.DaysOfMonth);
                    goto RetryMonth;
                }
            }

            if (IsBeyondEndDate()) return null;

            // L and # characters in day of week.

            if (!IsDayOfWeekMatch(year, month, day) ||
                HasFlag(CronExpressionFlag.DayOfWeekLast) && !CalendarHelper.IsLastDayOfWeek(day, lastDayOfMonth) ||
                HasFlag(CronExpressionFlag.NthDayOfWeek) && !CalendarHelper.IsNthDayOfWeek(day, _nthdayOfWeek))
            {
                Rollover(CronField.Hours);
                goto RetryDayOfMonth;
            }

            return new DateTime(CalendarHelper.DateTimeToTicks(year, month, day, hour, minute, second));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsDayOfWeekMatch(int year, int month, int day)
        {
            if (_dayOfWeek == -1L) return true;

            var dayOfWeek = CalendarHelper.GetDayOfWeek(year, month, day);
            return ((_dayOfWeek >> (int)dayOfWeek) & 1) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsDayOfWeekMatch(DayOfWeek dayOfWeek)
        {
            return ((_dayOfWeek >> (int)dayOfWeek) & 1) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int FindFirstSet(long value, int startBit, int endBit)
        {
            if (startBit <= endBit && GetBit(value, startBit)) return startBit;

            // TODO: Add description and source

            value = value >> startBit;
            if (value == 0) return -1;

            ulong res = unchecked((ulong)(value & -value) * 0x022fdd63cc95386d) >> 58;

            var result = DeBruijnPositions[res] + startBit;
            if (result > endBit) return -1;

            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasFlag(CronExpressionFlag value)
        {
            return (_flags & value) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe void SkipWhiteSpaces(ref char* pointer)
        {
            while (*pointer == '\t' || *pointer == ' ')
            {
                pointer++;
            }
        }

        private static unsafe void ParseField(
            CronField field,
            ref char* pointer, 
            CronExpression expression, 
            ref long bits)
        {
            if (*pointer == '*')
            {
                pointer++;

                if (field == CronField.Seconds || field == CronField.Minutes || field == CronField.Hours)
                {
                    expression._flags |= CronExpressionFlag.Interval;
                }

                if (*pointer != '/')
                {
                    SetAllBits(out bits);

                    SkipWhiteSpaces(ref pointer);

                    return;
                }

                ParseRange(field, ref pointer, expression, ref bits, true);
            }
            else
            {
                ParseList(field, ref pointer, expression, ref bits);
            }

            if (field == CronField.DaysOfMonth)
            {
                if (*pointer == 'W')
                {
                    pointer++;
                    expression._flags |= CronExpressionFlag.NearestWeekday;
                }
            }
            else if (field == CronField.DaysOfWeek)
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
                    pointer = GetNumber(out expression._nthdayOfWeek, MinNthDayOfWeek, null, pointer);

                    if (pointer == null || expression._nthdayOfWeek < MinNthDayOfWeek || expression._nthdayOfWeek > MaxNthDayOfWeek)
                    {
                        ThrowFormatException("'#' must be followed by a number between {0} and {1}.", MinNthDayOfWeek, MaxNthDayOfWeek);
                    }
                }
            }

            SkipWhiteSpaces(ref pointer);
        }

        private static unsafe void ParseList(
            CronField field, 
            ref char* pointer, 
            CronExpression expression, 
            ref long bits)
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
                ThrowFormatException("'{0}': using some numbers with 'W' is not supported.", field.ToString());
            }
        }

        private static unsafe void ParseRange(
            CronField field, 
            ref char* pointer, 
            CronExpression expression, 
            ref long bits,
            bool star)
        {
            int num1, num2, num3;

            var low = field.First;
            var high = field.Last;

            if (star)
            {
                num1 = low;
                num2 = high;
            }
            else if(*pointer == '?')
            {
                if (field != CronField.DaysOfMonth && field != CronField.DaysOfWeek)
                {
                    ThrowFormatException("'?' is not supported for the '{0}' field.", field);
                }

                pointer++;

                if (field == CronField.DaysOfMonth)
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
                if (field != CronField.DaysOfMonth)
                {
                    ThrowFormatException("'L' is not supported for the '{0}' field.", field);
                }

                pointer++;

                bits = 0b1111 << MinDaysInMonth; // TODO: Replace with a constant

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
                var names = field.Names;

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
                // No step. Default == 1.
                num3 = 1;
            }

            // If upper bound less than bottom one, e.g. range 55-10 specified
            // we'll set bits from 0 to 15 then we shift it right by 5 bits.
            int shift = 0;
            if (num2 < num1)
            {
                // Skip one of sundays.
                if (field == CronField.DaysOfWeek) high--;

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

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowFormatException(string format, params object[] args)
        {
            throw new FormatException(String.Format(format, args));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowWrongDateTimeKindException(string paramName)
        {
            throw new ArgumentException("The supplied DateTime must have the Kind property set to DateTimeKind.Utc", paramName);
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
        private static void SetAllBits(out long bits)
        {
            bits = -1L;
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
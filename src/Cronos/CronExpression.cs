using System;
using System.Runtime.CompilerServices;

namespace Cronos
{
    /// <summary>
    /// Provides a parser and scheduler for cron expressions.
    /// </summary>
    public sealed class CronExpression
    {
        private const long NotFound = 0;

        private const int MinNthDayOfWeek = 1;
        private const int MaxNthDayOfWeek = 5;
        private const int SundayBits = 0b1000_0001;

        private const int MaxYear = 2099;

        private static readonly TimeZoneInfo UtcTimeZone = TimeZoneInfo.Utc;

        private static readonly CronExpression Yearly = Parse("0 0 1 1 *");
        private static readonly CronExpression Weekly = Parse("0 0 * * 0");
        private static readonly CronExpression Monthly = Parse("0 0 1 * *");
        private static readonly CronExpression Daily = Parse("0 0 * * *");
        private static readonly CronExpression Hourly = Parse("0 * * * *");
        private static readonly CronExpression Minutely = Parse("* * * * *");
        private static readonly CronExpression Secondly = Parse("* * * * * *", CronFormat.IncludeSeconds);

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

        private long  _second;     // 60 bits -> from 0 bit to 59 bit
        private long  _minute;     // 60 bits -> from 0 bit to 59 bit
        private int   _hour;       // 24 bits -> from 0 bit to 23 bit
        private int   _dayOfMonth; // 31 bits -> from 1 bit to 31 bit
        private short _month;      // 12 bits -> from 1 bit to 12 bit
        private byte  _dayOfWeek;  // 8 bits  -> from 0 bit to 7 bit

        private byte  _nthdayOfWeek;
        private byte  _lastMonthOffset;

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

            unsafe
            {
                fixed (char* value = expression)
                {
                    var pointer = value;

                    SkipWhiteSpaces(ref pointer);

                    if (*pointer == '@')
                    {
                        var macroExpression = ParseMacro(ref pointer);
                        if (macroExpression == null) ThrowFormatException("Unexpected character '{0}' on position {1}.", *pointer, pointer - value);

                        pointer++;

                        SkipWhiteSpaces(ref pointer);

                        if (!IsEndOfString(*pointer)) ThrowFormatException("Unexpected character '{0}' on position {1}, end of string expected.", *pointer, pointer - value);
                        return macroExpression;
                    }

                    var cronExpression = new CronExpression();

                    if ((format & CronFormat.IncludeSeconds) != 0)
                    {
                        cronExpression._second = ParseField(CronField.Seconds, ref pointer, cronExpression);
                    }
                    else
                    {
                        SetBit(ref cronExpression._second, 0);
                    }

                    cronExpression._minute = ParseField(CronField.Minutes, ref pointer, cronExpression);
                    cronExpression._hour = (int)ParseField(CronField.Hours, ref pointer, cronExpression);
                    cronExpression._dayOfMonth = (int)ParseField(CronField.DaysOfMonth, ref pointer, cronExpression);
                    cronExpression._month = (short)ParseField(CronField.Months, ref pointer, cronExpression);
                    cronExpression._dayOfWeek = (byte)ParseField(CronField.DaysOfWeek, ref pointer, cronExpression);

                    if (!IsEndOfString(*pointer))
                    {
                        ThrowFormatException("Unexpected character '{0}' on position {1}, end of string expected. Please use the '{2}' argument to specify non-standard CRON fields.", *pointer, pointer - value, nameof(format));
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
        /// Calculates next occurrence starting with <paramref name="fromUtc"/> (optionally <paramref name="inclusive"/>) in UTC time zone.
        /// </summary>
        public DateTime? GetNextOccurrence(DateTime fromUtc, bool inclusive = false)
        {
            if (fromUtc.Kind != DateTimeKind.Utc) ThrowWrongDateTimeKindException(nameof(fromUtc));

            var found = FindOccurence(fromUtc.Ticks, inclusive);
            if (found == NotFound) return null;

            return new DateTime(found, DateTimeKind.Utc);
        }

        /// <summary>
        /// Calculates next occurrence starting with <paramref name="fromUtc"/> (optionally <paramref name="inclusive"/>) in given <paramref name="zone"/>
        /// </summary>
        public DateTime? GetNextOccurrence(DateTime fromUtc, TimeZoneInfo zone, bool inclusive = false)
        {
            if (fromUtc.Kind != DateTimeKind.Utc) ThrowWrongDateTimeKindException(nameof(fromUtc));

            if (ReferenceEquals(zone, UtcTimeZone))
            {
                var found = FindOccurence(fromUtc.Ticks, inclusive);
                if (found == NotFound) return null;

                return new DateTime(found, DateTimeKind.Utc);
            }

            var zonedStart = TimeZoneInfo.ConvertTime(fromUtc, zone);
            var occurrence = GetOccurenceByZonedTimes(zonedStart, zone, inclusive);

            return occurrence?.UtcDateTime;
        }

        /// <summary>
        /// Calculates next occurrence starting with <paramref name="from"/> (optionally <paramref name="inclusive"/>) in given <paramref name="zone"/>
        /// </summary>
        public DateTimeOffset? GetNextOccurrence(DateTimeOffset from, TimeZoneInfo zone, bool inclusive = false)
        {
            if (ReferenceEquals(zone, UtcTimeZone))
            {
                var found = FindOccurence(from.UtcTicks, inclusive);
                if (found == NotFound) return null;

                return new DateTimeOffset(found, TimeSpan.Zero);
            }

            var zonedStart = TimeZoneInfo.ConvertTime(from, zone);
            return GetOccurenceByZonedTimes(zonedStart, zone, inclusive);
        }

        private DateTimeOffset? GetOccurenceByZonedTimes(DateTimeOffset from, TimeZoneInfo zone, bool inclusive)
        {
            var fromLocal = from.DateTime;

            if (TimeZoneHelper.IsAmbiguousTime(zone, fromLocal))
            {
                var currentOffset = from.Offset;
                var standardOffset = zone.BaseUtcOffset;
               
                if (standardOffset != currentOffset)
                {
                    var daylightOffset = TimeZoneHelper.GetDaylightOffset(zone, fromLocal);
                    var daylightTimeLocalEnd = TimeZoneHelper.GetDaylightTimeEnd(zone, fromLocal, daylightOffset).DateTime;

                    // Early period, try to find anything here.
                    var foundInDaylightOffset = FindOccurence(fromLocal.Ticks, daylightTimeLocalEnd.Ticks, inclusive);
                    if (foundInDaylightOffset != NotFound) return new DateTimeOffset(foundInDaylightOffset, daylightOffset);

                    fromLocal = TimeZoneHelper.GetStandartTimeStart(zone, fromLocal, daylightOffset).DateTime;
                    inclusive = true;
                }

                // Skip late ambiguous interval.
                var ambiguousIntervalLocalEnd = TimeZoneHelper.GetAmbiguousIntervalEnd(zone, fromLocal).DateTime;

                if (HasFlag(CronExpressionFlag.Interval))
                {
                    var foundInStandardOffset = FindOccurence(fromLocal.Ticks, ambiguousIntervalLocalEnd.Ticks - 1, inclusive);
                    if (foundInStandardOffset != NotFound) return new DateTimeOffset(foundInStandardOffset, standardOffset);
                }

                fromLocal = ambiguousIntervalLocalEnd;
            }

            var occurrenceTicks = FindOccurence(fromLocal.Ticks, inclusive);
            if (occurrenceTicks == NotFound) return null;

            var occurrence = new DateTime(occurrenceTicks);

            if (zone.IsInvalidTime(occurrence))
            {
                var nextValidTime = TimeZoneHelper.GetDaylightTimeStart(zone, occurrence);
                return nextValidTime;
            }

            if (TimeZoneHelper.IsAmbiguousTime(zone, occurrence))
            {
                var daylightOffset = TimeZoneHelper.GetDaylightOffset(zone, occurrence);
                return new DateTimeOffset(occurrence, daylightOffset);
            }

            return new DateTimeOffset(occurrence, zone.GetUtcOffset(occurrence));
        }

        private long FindOccurence(long startTimeTicks, long endTimeTicks, bool startInclusive)
        {
            var found = FindOccurence(startTimeTicks, startInclusive);

            if (found == NotFound || found > endTimeTicks) return NotFound;
            return found;
        }

        private long FindOccurence(long ticks, bool startInclusive)
        {
            if (!startInclusive) ticks = CalendarHelper.AddMillisecond(ticks);

            CalendarHelper.FillDateTimeParts(
                ticks,
                out int startSecond,
                out int startMinute,
                out int startHour,
                out int startDay,
                out int startMonth,
                out int startYear);

            var minMatchedDay = GetFirstSet(_dayOfMonth);

            var second = startSecond;
            var minute = startMinute;
            var hour = startHour;
            var day = startDay;
            var month = startMonth;
            var year = startYear;

            int lastCheckedDay;

            if (!GetBit(_second, second) && !Move(_second, ref second)) minute++;
            if (!GetBit(_minute, minute) && !Move(_minute, ref minute)) hour++;
            if (!GetBit(_hour, hour) && !Move(_hour, ref hour)) day++;

            // If NearestWeekday flag is set it's possible forward shift.
            if (HasFlag(CronExpressionFlag.NearestWeekday)) day = CronField.DaysOfMonth.First;

            if (!GetBit(_dayOfMonth, day) && !Move(_dayOfMonth, ref day)) goto RetryMonth;
            if (!GetBit(_month, month)) goto RetryMonth;

            Retry:

            if (day > GetLastDayOfMonth(year, month)) goto RetryMonth;

            if (HasFlag(CronExpressionFlag.DayOfMonthLast)) day = GetLastDayOfMonth(year, month);
            lastCheckedDay = day;

            if (HasFlag(CronExpressionFlag.NearestWeekday)) day = CalendarHelper.MoveToNearestWeekDay(year, month, day);

            if (IsDayOfWeekMatch(year, month, day))
            {
                if (CalendarHelper.IsGreaterThan(year, month, day, startYear, startMonth, startDay)) goto RolloverDay;
                if (hour > startHour) goto RolloverHour;
                if (minute > startMinute) goto RolloverMinute;
                goto ReturnResult;

                RolloverDay: hour = GetFirstSet(_hour);
                RolloverHour: minute = GetFirstSet(_minute);
                RolloverMinute: second = GetFirstSet(_second);

                ReturnResult:

                var found = CalendarHelper.DateTimeToTicks(year, month, day, hour, minute, second);
                if (found >= ticks) return found;
            }

            day = lastCheckedDay;
            if (Move(_dayOfMonth, ref day)) goto Retry;

            RetryMonth:

            if (!Move(_month, ref month) && ++year >= MaxYear) return NotFound;
            day = minMatchedDay;

            goto Retry;
        }

        private static bool Move(long fieldBits, ref int fieldValue)
        {
            if (fieldBits >> ++fieldValue == 0)
            {
                fieldValue = GetFirstSet(fieldBits);
                return false;
            }

            fieldValue += GetFirstSet(fieldBits >> fieldValue);
            return true;
        }

        private int GetLastDayOfMonth(int year, int month)
        {
            return CalendarHelper.GetDaysInMonth(year, month) - _lastMonthOffset;
        }

        private bool IsDayOfWeekMatch(int year, int month, int day)
        {
            if (HasFlag(CronExpressionFlag.DayOfWeekLast) && !CalendarHelper.IsLastDayOfWeek(year, month, day) ||
                HasFlag(CronExpressionFlag.NthDayOfWeek) && !CalendarHelper.IsNthDayOfWeek(day, _nthdayOfWeek))
            {
                return false;
            }

            if (_dayOfWeek == CronField.DaysOfWeek.AllBits) return true;

            var dayOfWeek = CalendarHelper.GetDayOfWeek(year, month, day);

            return ((_dayOfWeek >> (int)dayOfWeek) & 1) != 0;
        }

#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static int GetFirstSet(long value)
        {
            // TODO: Add description and source
            ulong res = unchecked((ulong)(value & -value) * 0x022fdd63cc95386d) >> 58;
            return DeBruijnPositions[res];
        }

        private bool HasFlag(CronExpressionFlag value)
        {
            return (_flags & value) != 0;
        }

        private static unsafe void SkipWhiteSpaces(ref char* pointer)
        {
            while (IsWhiteSpace(*pointer)) pointer++; 
        }

        private static unsafe CronExpression ParseMacro(ref char* pointer)
        {
            pointer++;

            switch (ToUpper(*pointer))
            {
                case 'A':
                    if (ToUpper(*++pointer) == 'N' &&
                        ToUpper(*++pointer) == 'N' &&
                        ToUpper(*++pointer) == 'U' &&
                        ToUpper(*++pointer) == 'A' &&
                        ToUpper(*++pointer) == 'L' &&
                        ToUpper(*++pointer) == 'L' &&
                        ToUpper(*++pointer) == 'Y')
                        return Yearly;
                    return null;
                case 'D':
                    if (ToUpper(*++pointer) == 'A' &&
                        ToUpper(*++pointer) == 'I' &&
                        ToUpper(*++pointer) == 'L' &&
                        ToUpper(*++pointer) == 'Y')
                        return Daily;
                    return null;
                case 'E':
                    if (ToUpper(*++pointer) == 'V' &&
                        ToUpper(*++pointer) == 'E' &&
                        ToUpper(*++pointer) == 'R' &&
                        ToUpper(*++pointer) == 'Y' &&
                        ToUpper(*++pointer) == '_')
                    {
                        pointer++;
                        if (ToUpper(*pointer) == 'M' &&
                            ToUpper(*++pointer) == 'I' &&
                            ToUpper(*++pointer) == 'N' &&
                            ToUpper(*++pointer) == 'U' &&
                            ToUpper(*++pointer) == 'T' &&
                            ToUpper(*++pointer) == 'E')
                            return Minutely;

                        if (*(pointer - 1) != '_') return null;

                        if (*(pointer - 1) == '_' &&
                            ToUpper(*pointer) == 'S' &&
                            ToUpper(*++pointer) == 'E' &&
                            ToUpper(*++pointer) == 'C' &&
                            ToUpper(*++pointer) == 'O' &&
                            ToUpper(*++pointer) == 'N' &&
                            ToUpper(*++pointer) == 'D')
                            return Secondly;
                    }

                    return null;
                case 'H':
                    if (ToUpper(*++pointer) == 'O' &&
                        ToUpper(*++pointer) == 'U' &&
                        ToUpper(*++pointer) == 'R' &&
                        ToUpper(*++pointer) == 'L' &&
                        ToUpper(*++pointer) == 'Y')
                        return Hourly;
                    return null;
                case 'M':
                    pointer++;
                    if (ToUpper(*pointer) == 'O' &&
                        ToUpper(*++pointer) == 'N' &&
                        ToUpper(*++pointer) == 'T' &&
                        ToUpper(*++pointer) == 'H' &&
                        ToUpper(*++pointer) == 'L' &&
                        ToUpper(*++pointer) == 'Y')
                        return Monthly;

                    if (ToUpper(*(pointer - 1)) == 'M' &&
                        ToUpper(*pointer) == 'I' &&
                        ToUpper(*++pointer) == 'D' &&
                        ToUpper(*++pointer) == 'N' &&
                        ToUpper(*++pointer) == 'I' &&
                        ToUpper(*++pointer) == 'G' &&
                        ToUpper(*++pointer) == 'H' &&
                        ToUpper(*++pointer) == 'T')
                        return Daily;

                    return null;
                case 'W':
                    if (ToUpper(*++pointer) == 'E' &&
                        ToUpper(*++pointer) == 'E' &&
                        ToUpper(*++pointer) == 'K' &&
                        ToUpper(*++pointer) == 'L' &&
                        ToUpper(*++pointer) == 'Y')
                        return Weekly;
                    return null;
                case 'Y':
                    if (ToUpper(*++pointer) == 'E' &&
                        ToUpper(*++pointer) == 'A' &&
                        ToUpper(*++pointer) == 'R' &&
                        ToUpper(*++pointer) == 'L' &&
                        ToUpper(*++pointer) == 'Y')
                        return Yearly;
                    return null;
                default:
                    return null;
            }
        }

        private static unsafe long ParseField(CronField field, ref char* pointer, CronExpression expression)
        {
            var bits = 0L;
            if (*pointer == '*' || *pointer == '?')
            {
                pointer++;

                if (field.CanDefineInterval) expression._flags |= CronExpressionFlag.Interval;

                if (*pointer != '/')
                {
                    bits = field.AllBits;

                    if (!IsWhiteSpace(*pointer) && !IsEndOfString(*pointer)) ThrowFormatException(field, "'{0}' is not supported after '{1}'.", *pointer, *(pointer - 1));

                    SkipWhiteSpaces(ref pointer);
                    return bits;
                }

                bits |= ParseRange(field, ref pointer, expression, true);
            }
            else
            {
                bits |= ParseList(field, ref pointer, expression);
            }

            if (field == CronField.DaysOfMonth)
            {
                if (ToUpper(*pointer) == 'W')
                {
                    pointer++;
                    expression._flags |= CronExpressionFlag.NearestWeekday;
                }
            }
            else if (field == CronField.DaysOfWeek)
            {
                if (ToUpper(*pointer) == 'L')
                {
                    pointer++;
                    expression._flags |= CronExpressionFlag.DayOfWeekLast;
                }
                else if (*pointer == '#')
                {
                    pointer++;
                    expression._flags |= CronExpressionFlag.NthDayOfWeek;
                    var nthdayOfWeek = GetNumber(ref pointer, null);

                    if (nthdayOfWeek == -1 || nthdayOfWeek < MinNthDayOfWeek || nthdayOfWeek > MaxNthDayOfWeek)
                    {
                        ThrowFormatException(field, "'#' must be followed by a number between {0} and {1}.", MinNthDayOfWeek, MaxNthDayOfWeek);
                    }

                    expression._nthdayOfWeek = (byte)nthdayOfWeek;
                }
            }

            if (!IsWhiteSpace(*pointer) && !IsEndOfString(*pointer)) ThrowFormatException(field, "Unexpected character '{0}'.", *pointer);

            SkipWhiteSpaces(ref pointer);
            return bits;
        }

        private static unsafe long ParseList(CronField field, ref char* pointer, CronExpression expression)
        {
            var bits = 0L;
            var singleValue = true;
            while (true)
            {
                bits |= ParseRange(field, ref pointer, expression, false);

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

            if (ToUpper(*pointer) == 'W' && !singleValue)
            {
                ThrowFormatException(field, "Using some numbers with 'W' is not supported.");
            }

            return bits;
        }

        private static unsafe long ParseRange(CronField field, ref char* pointer, CronExpression expression, bool star)
        {
            var bits = 0L;
            int num1, num2, num3;

            var low = field.First;
            var high = field.Last;

            if (star)
            {
                num1 = low;
                num2 = high;
            }
            else if(ToUpper(*pointer) == 'L')
            {
                if (field != CronField.DaysOfMonth)
                {
                    ThrowFormatException(field, "'L' is not supported.");
                }

                pointer++;

                expression._flags |= CronExpressionFlag.DayOfMonthLast;

                if (*pointer == '-')
                {
                    // Eat the dash.
                    pointer++;

                    int lastMonthOffset;
                    // Get the number following the dash.
                    if ((lastMonthOffset = GetNumber(ref pointer, null)) == -1 || lastMonthOffset < 0 || lastMonthOffset >= high)
                    {
                        ThrowFormatException(field, "Last month offset must be a number between {0} and {1} (all inclusive).", low, high);
                    }

                    expression._lastMonthOffset = (byte)lastMonthOffset;
                }
                return field.AllBits;
            }
            else
            {
                var names = field.Names;

                if ((num1 = GetNumber(ref pointer, names)) == -1 || num1 < low || num1 > high)
                {
                    ThrowFormatException(field, "Value must be a number between {0} and {1} (all inclusive).", field, low, high);
                }

                if (*pointer == '-')
                {
                    if (field.CanDefineInterval) expression._flags |= CronExpressionFlag.Interval;

                    // Eat the dash.
                    pointer++;

                    // Get the number following the dash.
                    if ((num2 = GetNumber(ref pointer, names)) == -1 || num2 < low || num2 > high)
                    {
                        ThrowFormatException(field, "Range must contain numbers between {0} and {1} (all inclusive).", low, high);
                    }

                    if (ToUpper(*pointer) == 'W')
                    {
                        ThrowFormatException(field, "'W' is not allowed after '-'.");
                    }
                }
                else if (*pointer == '/')
                {
                    if (field.CanDefineInterval) expression._flags |= CronExpressionFlag.Interval;

                    // If case of slash upper bound is high. E.g. '10/2` means 'every value from 10 to high with step size = 2'.
                    num2 = high;
                }
                else
                {
                    return 1L << num1;
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
                if ((num3 = GetNumber(ref pointer, null)) == -1 || num3 <= 0 || num3 > high)
                {
                    ThrowFormatException(field, "Step must be a number between 1 and {0} (all inclusive).", high);
                }
                if (ToUpper(*pointer) == 'W')
                {
                    ThrowFormatException(field, "'W' is not allowed after '/'.");
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

            return bits;
        }

        private static unsafe int GetNumber(ref char* pointer, int[] names)
        {
            if (IsDigit(*pointer))
            {
                var num = GetNumeric(*pointer++);

                if (!IsDigit(*pointer)) return num;

                num = num * 10 + GetNumeric(*pointer++);

                if (!IsDigit(*pointer)) return num;
                return -1;
            }

            if (names == null) return -1;

            if (!IsLetter(*pointer)) return -1;
            var buffer = ToUpper(*pointer++);

            if (!IsLetter(*pointer)) return -1;
            buffer |= ToUpper(*pointer++) << 8;

            if (!IsLetter(*pointer)) return -1;
            buffer |= ToUpper(*pointer++) << 16;

            if (IsLetter(*pointer)) return -1;

            var length = names.Length;

            for (var i = 0; i < length; i++)
            {
                if (buffer == names[i])
                {
                    return i;
                }
            }

            return -1;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowFormatException(CronField field, string format, params object[] args)
        {
            throw new CronFormatException(field, String.Format(format, args));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowFormatException(string format, params object[] args)
        {
            throw new CronFormatException(String.Format(format, args));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowWrongDateTimeKindException(string paramName)
        {
            throw new ArgumentException("The supplied DateTime must have the Kind property set to Utc", paramName);
        }

#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static bool GetBit(long value, int index)
        {
            return (value & (1L << index)) != 0;
        }

#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        internal static void SetBit(ref long value, int index)
        {
            value |= 1L << index;
        }

#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static bool IsEndOfString(int code)
        {
            return code == '\0';
        }

#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static bool IsWhiteSpace(int code)
        {
            return code == '\t' || code == ' ';
        }

#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static bool IsDigit(int code)
        {
            return code >= 48 && code <= 57;
        }

#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static bool IsLetter(int code)
        {
            return (code >= 65 && code <= 90) || (code >= 97 && code <= 122);
        }

#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private static int GetNumeric(int code)
        {
            return code - 48;
        }

#if !NET40
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
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
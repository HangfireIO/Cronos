using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using NodaTime;

namespace Cronos
{
    public class CronExpression
    {
        private long _second; // 60 bits -> 64 bits in Int64
        private long _minute; // 60 bits -> 64 bits in Int64
        private long _hour; // 24 bits -> 32 bits in Int32
        private long _dayOfMonth; // 31 bits -> 32 bits ini Int32
        private long _month; // 12 bits -> 16 bits in Int16
        private long _dayOfWeek; // 8 bits -> 8 bits in byte

        private int _nthdayOfWeek;
        private bool _nearestWeekday;

        private CronExpression()
        {
        }

        public CronExpressionFlag Flags { get; private set; }

        private static Calendar Calendar => CultureInfo.InvariantCulture.Calendar;

        public static CronExpression Parse(string cronExpression)
        {
            if (string.IsNullOrEmpty(cronExpression)) throw new ArgumentNullException(nameof(cronExpression));

            var expression = new CronExpression();

            unsafe
            {
                fixed (char* value = cronExpression)
                {
                    // Second.

                    var pointer = value;

                    pointer = SkipWhiteSpaces(pointer);

                    if (*pointer == '*')
                    {
                        expression.Flags |= CronExpressionFlag.SecondStar;
                    }

                    if ((pointer = GetList(ref expression._second, Constants.FirstSecond, Constants.LastSecond, null, pointer, CronFieldType.Second)) == null)
                    {
                        throw new ArgumentException($"second '{cronExpression}'", nameof(cronExpression));
                    }

                    // Minute.

                    if (*pointer == '*')
                    {
                        expression.Flags |= CronExpressionFlag.MinuteStar;
                    }

                    if ((pointer = GetList(ref expression._minute, Constants.FirstMinute, Constants.LastMinute, null, pointer, CronFieldType.Minute)) == null)
                    {
                        throw new ArgumentException($"minute '{cronExpression}'", nameof(cronExpression));
                    }

                    // Hour.

                    if (*pointer == '*')
                    {
                        expression.Flags |= CronExpressionFlag.HourStar;
                    }

                    if ((pointer = GetList(ref expression._hour, Constants.FirstHour, Constants.LastHour, null, pointer, CronFieldType.Hour)) == null)
                    {
                        throw new ArgumentException("hour", nameof(cronExpression));
                    }

                    // Day of month.

                    if (*pointer == '?')
                    {
                        expression.Flags |= CronExpressionFlag.DayOfMonthQuestion;
                    }
                    else if (*pointer == 'L')
                    {
                        expression.Flags |= CronExpressionFlag.DayOfMonthLast;
                    }

                    if ((pointer = GetList(ref expression._dayOfMonth, Constants.FirstDayOfMonth, Constants.LastDayOfMonth, null, pointer, CronFieldType.DayOfMonth)) == null)
                    {
                        throw new ArgumentException("day of month", nameof(cronExpression));
                    }

                    if (*pointer == 'W')
                    {
                        expression._nearestWeekday = true;
                        pointer++;

                        pointer = SkipWhiteSpaces(pointer);
                    }

                    // Month.

                    if ((pointer = GetList(ref expression._month, Constants.FirstMonth, Constants.LastMonth, Constants.MonthNamesArray, pointer, CronFieldType.Month)) == null)
                    {
                        throw new ArgumentException("month", nameof(cronExpression));
                    }

                    // If Month field doesn't contain months with 31 days and Day of month contains only 31 or
                    // Month field doesn't contain months with 30 or 31 days Day of month contains only 30 or 31
                    // it means that date is unreachable.

                    if (((expression._month & Constants.MonthsWith31Days) == 0 && (expression._dayOfMonth | Constants.The31ThDayOfMonth) == Constants.The31ThDayOfMonth) ||
                        ((expression._month & Constants.MonthsWith30Or31Days) == 0 && (expression._dayOfMonth | Constants.The30ThOr31ThDayOfMonth) == Constants.The30ThOr31ThDayOfMonth))
                    {
                        throw new ArgumentException("month", nameof(cronExpression));
                    }

                    // Day of week.

                    if (*pointer == '?' && expression.HasFlag(CronExpressionFlag.DayOfMonthQuestion))
                    {
                        throw new ArgumentException("day of week", nameof(cronExpression));
                    }

                    if ((pointer = GetList(ref expression._dayOfWeek, Constants.FirstDayOfWeek, Constants.LastDayOfWeek, Constants.DayOfWeekNamesArray, pointer, CronFieldType.DayOfWeek)) == null)
                    {
                        throw new ArgumentException("day of week", nameof(cronExpression));
                    }

                    if (*pointer == 'L')
                    {
                        expression.Flags |= CronExpressionFlag.DayOfWeekLast;
                        pointer++;
                    }

                    if (*pointer == '#')
                    {
                        pointer++;
                        pointer = GetNumber(out expression._nthdayOfWeek, Constants.MinNthDayOfWeek, null, pointer);

                        if (expression._nthdayOfWeek < Constants.MinNthDayOfWeek || expression._nthdayOfWeek > Constants.MaxNthDayOfWeek)
                        {
                            throw new ArgumentException("day of week", nameof(cronExpression));
                        }

                        pointer = SkipWhiteSpaces(pointer);
                    }

                    if (*pointer != '\0')
                    {
                        throw new ArgumentException("invalid cron", nameof(cronExpression));
                    }

                    // Make sundays equivilent.
                    if (GetBit(expression._dayOfWeek, 0) || GetBit(expression._dayOfWeek, 7))
                    {
                        SetBit(ref expression._dayOfWeek, 0);
                        SetBit(ref expression._dayOfWeek, 7);
                    }

                    return expression;
                }
            }
        }

        public ZonedDateTime? Next(ZonedDateTime now)
        {
            return Next(now.LocalDateTime, now.Offset, now.Zone);
        }

        public ZonedDateTime? Next(LocalDateTime now, Offset currentOffset, DateTimeZone zone)
        {
            if (zone.Equals(DateTimeZone.Utc))
            {
                return Next(now, LocalDateTime.FromDateTime(DateTime.MaxValue))?.InUtc();
            }

            var mapping = zone.MapLocal(now);

            if (IsMatch(now))
            {
                switch (mapping.Count)
                {
                    case 0:
                        // Strict jobs should be shifted to next valid time.
                        return now.InZoneLeniently(zone);
                    case 1:
                        // Strict
                        return now.InZoneStrictly(zone);
                    case 2:
                        // Ambiguous.

                        // Interval jobs should be fired in both offsets.
                        if (HasFlag(CronExpressionFlag.SecondStar | CronExpressionFlag.MinuteStar | CronExpressionFlag.HourStar))
                        {
                            return new ZonedDateTime(now, zone, currentOffset);
                        }

                        // Strict jobs should be fired in lowest offset only.
                        if (currentOffset == mapping.EarlyInterval.WallOffset)
                        {
                            return new ZonedDateTime(now, zone, currentOffset);
                        }

                        break;
                    default:
                        // TODO: or what?
                        throw new InvalidOperationException();
                }
            }

            if (mapping.Count == 2)
            {
                var early = mapping.EarlyInterval;
                var late = mapping.LateInterval;

                if (early.WallOffset == currentOffset)
                {
                    // Current period, try to find anything here.
                    var found = Next(now, early.IsoLocalEnd.PlusMinutes(-1));
                    if (found.HasValue)
                    {
                        return Next(found.Value, currentOffset, zone);
                    }

                    // Try to find anything starting from late offset.
                    found = Next(late.IsoLocalStart, LocalDateTime.FromDateTime(DateTime.MaxValue));
                    if (found.HasValue)
                    {
                        return Next(found.Value, late.WallOffset, zone);
                    }
                }
            }

            // Does not match, find next.
            var nextFound = Next(now.PlusSeconds(1), LocalDateTime.FromDateTime(DateTime.MaxValue));
            if (nextFound == null) return null;

            return Next(nextFound.Value, currentOffset, zone);
        }

        private LocalDateTime? Next(LocalDateTime baseTime, LocalDateTime endTime)
        {
            var baseYear = baseTime.Year;
            var baseMonth = baseTime.Month;
            var baseDay = baseTime.Day;
            var baseHour = baseTime.Hour;
            var baseMinute = baseTime.Minute;
            var baseSecond = baseTime.Second;

            var endYear = endTime.Year;
            var endMonth = endTime.Month;
            var endDay = endTime.Day;

            var year = baseYear;
            var month = baseMonth;
            var day = baseDay;
            var hour = baseHour;
            var minute = baseMinute;
            var second = baseSecond;

            var minSecond = FindFirstSet(_second, Constants.FirstSecond, Constants.LastSecond);
            var minMinute = FindFirstSet(_minute, Constants.FirstMinute, Constants.LastMinute);
            var minHour = FindFirstSet(_hour, Constants.FirstHour, Constants.LastHour);
            var minDayOfMonth = FindFirstSet(_dayOfMonth, Constants.FirstDayOfMonth, Constants.LastDayOfMonth);
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
                if (day > Constants.LastDayOfMonth)
                {
                    day = minDayOfMonth;
                    month++;
                }
            }

            //
            // Day of month.
            //

            day = FindFirstSet(_dayOfMonth, day, Constants.LastDayOfMonth);

            RetryDayMonth:

            if (day == -1)
            {
                minute = minMinute;
                hour = minHour;
                day = minDayOfMonth;
                month++;
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
                month = nextMonth;

                if (month > baseMonth)
                {
                    second = minSecond;
                    minute = minMinute;
                    hour = minHour;
                    day = minDayOfMonth;
                }
            }
            else
            {
                second = minSecond;
                minute = minMinute;
                hour = minHour;
                day = minDayOfMonth;
                month = minMonth;
                year++;
            }

            //
            // The day field in a cron expression spans the entire range of days
            // in a month, which is from 1 to 31. However, the number of days in
            // a month tend to be variable depending on the month (and the year
            // in case of February). So a check is needed here to see if the
            // date is a border case. If the day happens to be beyond 28
            // (meaning that we're dealing with the suspicious range of 29-31)
            // and the date part has changed then we need to determine whether
            // the day still makes sense for the given year and month. If the
            // day is beyond the last possible value, then the day/month part
            // for the schedule is re-evaluated. So an expression like "0 0
            // 15,31 * *" will yield the following sequence starting on midnight
            // of Jan 1, 2000:
            //
            //  Jan 15, Jan 31, Feb 15, Mar 15, Apr 15, Apr 31, ...
            //

            var dateChanged = day != baseDay || month != baseMonth || year != baseYear;

            if (day > 28 && dateChanged && day > Calendar.GetDaysInMonth(year, month))
            {
                if (year >= endYear && month >= endMonth && day >= endDay)
                    return endTime;

                day = -1;
                goto RetryDayMonth;
            }

            // W character.

            if (_nearestWeekday)
            {
                var lastDayOfMonth = Calendar.GetDaysInMonth(year, month);

                if (lastDayOfMonth < day)
                {
                    day = -1;
                }
                else
                {
                    var dayOfWeek = Calendar.GetDayOfWeek(new DateTime(year, month, day));

                    if (dayOfWeek == DayOfWeek.Sunday)
                    {
                        day = day == lastDayOfMonth
                            ? day - 2
                            : day + 1;
                    }
                    else if (dayOfWeek == DayOfWeek.Saturday)
                    {
                        day = day == Constants.FirstDayOfMonth
                            ? day + 2
                            : day - 1;
                    }
                    if (month == baseMonth && year == baseYear)
                    {
                        if (day < baseDay)
                        {
                            day = -1;
                            goto RetryDayMonth;
                        }
                        if (day == baseDay)
                        {
                            // There is not matched time in that day after base time.
                            if (nextHour == -1)
                            {
                                day = -1;
                                goto RetryDayMonth;
                            }

                            // Recover hour, minute and second matched for baseDay.
                            hour = nextHour;
                            minute = nextMinute == -1 ? minMinute : nextMinute;
                            second = nextSecond == -1 ? minSecond : nextSecond;

                            if (new LocalDateTime(year, month, day, hour, minute, second, 0) < baseTime)
                            {
                                day = -1;
                                goto RetryDayMonth;
                            }
                        }
                        else
                        {
                            hour = minHour;
                            minute = minMinute;
                            second = minSecond;
                        }
                    }
                }
            }

            var nextTime = new LocalDateTime(year, month, day, hour, minute, second, 0);

            if (nextTime > endTime)
                return null;

            //
            // L character.
            //

            if ((Flags & CronExpressionFlag.DayOfMonthLast) != 0)
            {
                var lastDayOfMonth = Calendar.GetDaysInMonth(nextTime.Year, nextTime.Month);
                if (nextTime.Day == lastDayOfMonth)
                    return nextTime;

                return Next(new LocalDateTime(year, month, lastDayOfMonth, 0, 0, 0, 0), endTime);
            }
            if ((Flags & CronExpressionFlag.DayOfWeekLast) != 0)
            {
                if (((_dayOfWeek >> nextTime.DayOfWeek) & 1) != 0)
                {
                    if (month != nextTime.PlusWeeks(1).Month)
                        return nextTime;
                }

                return Next(new LocalDateTime(year, month, day, 0, 0, 0, 0).PlusDays(1), endTime);
            }
            if (_nthdayOfWeek != 0)
            {
                if (((_dayOfWeek >> nextTime.DayOfWeek) & 1) != 0)
                {
                    if (month != nextTime.PlusWeeks(-1 * _nthdayOfWeek).Month &&
                        month == nextTime.PlusWeeks(-1 * (_nthdayOfWeek - 1)).Month)
                    {
                        return nextTime;
                    }
                }

                return Next(new LocalDateTime(year, month, day, 0, 0, 0, 0).PlusDays(1), endTime);
            }

            //
            // Day of week.
            //

            if (((_dayOfWeek >> nextTime.DayOfWeek) & 1) != 0)
                return nextTime;

            return Next(new LocalDateTime(year, month, day, 23, 59, 59, 0).PlusSeconds(1), endTime);
        }

        private static int FindFirstSet(long value, int startBit, int endBit)
        {
            return DeBruijin.FindFirstSet(value, startBit, endBit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool HasFlag(CronExpressionFlag flag)
        {
            return (Flags & flag) != 0;
        }

        private bool IsMatch(int second, int minute, int hour, int dayOfMonth, int month, int dayOfWeek, int year)
        {
            if ((Flags & CronExpressionFlag.DayOfMonthLast) != 0)
            {
                if (dayOfMonth != Calendar.GetDaysInMonth(year, month)) return false;
            }
            else if ((Flags & CronExpressionFlag.DayOfWeekLast) != 0)
            {
                if (dayOfMonth + Constants.DaysPerWeekCount <= Calendar.GetDaysInMonth(year, month)) return false;
            }
            else if (_nthdayOfWeek != 0)
            {
                if ((dayOfMonth - (_nthdayOfWeek - 1) * Constants.DaysPerWeekCount <= 0) || (dayOfMonth - _nthdayOfWeek * Constants.DaysPerWeekCount) > 0)
                {
                    return false;
                }
            }
            else if (_nearestWeekday)
            {
                var daysInMonth = Calendar.GetDaysInMonth(year, month);
                var isDayMatched = GetBit(_dayOfMonth, dayOfMonth) && dayOfWeek > 0 && dayOfWeek < 6 ||
                                   GetBit(_dayOfMonth, dayOfMonth - 1) && dayOfWeek == 1 ||
                                   GetBit(_dayOfMonth, dayOfMonth + 1) && dayOfWeek == 5 ||
                                   GetBit(_dayOfMonth, 1) && dayOfWeek == 1 && (dayOfMonth == 2 || dayOfMonth == 3) ||
                                   GetBit(_dayOfMonth, dayOfMonth + 2) && dayOfMonth == daysInMonth - 2 && dayOfWeek == 5;

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
                   (_nearestWeekday || GetBit(_dayOfMonth, dayOfMonth));
        }

        private bool IsMatch(LocalDateTime dateTime)
        {
            return IsMatch(
                dateTime.Second,
                dateTime.Minute,
                dateTime.Hour,
                dateTime.Day,
                dateTime.Month,
                dateTime.DayOfWeek,
                dateTime.Year);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static unsafe char* SkipWhiteSpaces(char* pointer)
        {
            while (*pointer == '\t' || *pointer == ' ')
            {
                pointer++;
            }

            return pointer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                if ((pointer = GetRange(ref bits, low, high, names, pointer, cronFieldType)) == null)
                {
                    return null;
                }

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
                return null;
            }

            // exiting.  skip to some blanks, then skip over the blanks.
            /*while (*pointer != '\t' && *pointer != ' ' && *pointer != '\n' && *pointer != '\0')
            {
                pointer++;
            }*/

            pointer = SkipWhiteSpaces(pointer);

            return pointer;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
                // '*' means "first-last" but can still be modified by /step
                num1 = low;
                num2 = high;

                pointer++;

                if (*pointer != '/')
                {
                    bits = ~0L;
                    return pointer;
                }
            }
            else if(*pointer == '?')
            {
                if (cronFieldType != CronFieldType.DayOfMonth && cronFieldType != CronFieldType.DayOfWeek)
                {
                    return null;
                }

                pointer++;

                if (*pointer == '/') return null;

                bits = ~0L;
                return pointer;
            }
            else if(*pointer == 'L')
            {
                if (cronFieldType != CronFieldType.DayOfMonth) return null;

                pointer++;

                bits = ~0L;
                return pointer;
            }
            else
            {
                if ((pointer = GetNumber(out num1, low, names, pointer)) == null)
                {
                    return null;
                }

                // Explicitly check for sane values. Certain combinations of ranges and
                // steps which should return EOF don't get picked up by the code below,
                // eg:
                //     5-64/30 * * * *
                //
                // Code adapted from set_elements() where this error was probably intended
                // to be catched.
                if (num1 < low || num1 > high)
                {
                    return null;
                }

                if (*pointer == '-')
                {
                    // eat the dash
                    pointer++;

                    // get the number following the dash
                    if ((pointer = GetNumber(out num2, low, names, pointer)) == null)
                    {
                        return null;
                    }

                    // Explicitly check for sane values. Certain combinations of ranges and
                    // steps which should return EOF don't get picked up by the code below,
                    // eg:
                    //     5-64/30 * * * *
                    //
                    // Code adapted from set_elements() where this error was probably intended
                    // to be catched.
                    if (num2 < low || num2 > high)
                    {
                        return null;
                    }
                    if (*pointer == 'W')
                    {
                        return null;
                    }
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

            // check for step size
            if (*pointer == '/')
            {
                // eat the slash
                pointer++;

                // Get the step size -- note: we don't pass the
                // names here, because the number is not an
                // element id, it's a step size.  'low' is
                // sent as a 0 since there is no offset either.
                if ((pointer = GetNumber(out num3, 0, null, pointer)) == null || num3 <= 0 || num3 > high)
                {
                    return null;
                }
                if (*pointer == 'W')
                {
                    return null;
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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

                num = (num << 3) + (num << 1) + GetNumeric(*pointer++);

                if (!IsDigit(*pointer)) return pointer;

                return null;

                /*do
                {
                    num = (num << 3) + (num << 1) + GetNumeric(*pointer++);
                } while (IsDigit(*pointer));

                return pointer;*/
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
using System;
using System.Collections.Generic;
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

        // May be packed into 135 bits / 17 bytes, or 19 bytes unpacked + 4(2?) for Flags
        // Regular CRON string is 9 + 1(\0) bytes minimum
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
                    // Seconds

                    var pointer = value;

                    if (*pointer == '*')
                    {
                        expression.Flags |= CronExpressionFlag.SecondStar;
                    }

                    if ((pointer = GetList(ref expression._second, Constants.FirstSecond, Constants.LastSecond, null, pointer, CronFieldType.Second)) == null)
                    {
                        throw new ArgumentException($"second '{cronExpression}'", nameof(cronExpression));
                    }

                    // Minutes

                    if (*pointer == '*')
                    {
                        expression.Flags |= CronExpressionFlag.MinuteStar;
                    }

                    if ((pointer = GetList(ref expression._minute, Constants.FirstMinute, Constants.LastMinute, null, pointer, CronFieldType.Minute)) == null)
                    {
                        throw new ArgumentException($"minute '{cronExpression}'", nameof(cronExpression));
                    }

                    // Hours

                    if (*pointer == '*')
                    {
                        expression.Flags |= CronExpressionFlag.HourStar;
                    }

                    if ((pointer = GetList(ref expression._hour, Constants.FirstHour, Constants.LastHour, null, pointer, CronFieldType.Hour)) == null)
                    {
                        throw new ArgumentException("hour", nameof(cronExpression));
                    }

                    // Days of month

                    if (*pointer == '*')
                    {
                        expression.Flags |= CronExpressionFlag.DayOfMonthStar;
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

                        // TODO: Consider way when cronExpression contains '\t' symbols.
                        // eat space
                        pointer++;
                    }

                    // Months

                    if ((pointer = GetList(ref expression._month, Constants.FirstMonth, Constants.LastMonth, Constants.MonthNamesArray, pointer, CronFieldType.Month)) == null)
                    {
                        throw new ArgumentException("month", nameof(cronExpression));
                    }

                    // 101011010101 0
                    const long monthsWith31Days = 0x15AA;
                    // 111111111101 0
                    const long monthsWith30Or31Days = 0x1FFA;

                    const long the31thDayOfMonth = 0x80000000;
                    const long the30thAnd31thDayOfMonth = 0xC0000000;
                    

                    if((expression._month & monthsWith31Days) == 0 && (expression._dayOfMonth & the31thDayOfMonth) != 0 ||
                        (expression._month & monthsWith30Or31Days) == 0 && (expression._dayOfMonth & the30thAnd31thDayOfMonth) != 0)
                    {
                        throw new ArgumentException("month", nameof(cronExpression));
                    }

                    // Days of week

                    if (*pointer == '*')
                    {
                        if(expression.Flags.HasFlag(CronExpressionFlag.DayOfMonthStar)) throw new ArgumentException("day of week", nameof(cronExpression));

                        expression.Flags |= CronExpressionFlag.DayOfWeekStar;
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
                        pointer = GetNumber(out expression._nthdayOfWeek, 1, null, pointer);

                        if (expression._nthdayOfWeek < 1 || expression._nthdayOfWeek > 5)
                        {
                            throw new ArgumentException("day of week", nameof(cronExpression));
                        }
                    }

                    if (*pointer != '\0')
                    {
                        throw new ArgumentException("invalid cron", nameof(cronExpression));
                    }

                    /* make sundays equivilent */
                    if (GetBit(expression._dayOfWeek, 0) || GetBit(expression._dayOfWeek, 7))
                    {
                        SetBit(ref expression._dayOfWeek, 0);
                        SetBit(ref expression._dayOfWeek, 7);
                    }

                    return expression;
                }
            }
        }

        public bool IsMatch(int second, int minute, int hour, int dayOfMonth, int month, int dayOfWeek, int year)
        {
            var isDayMatched = true;
            if (Flags.HasFlag(CronExpressionFlag.DayOfMonthLast))
            {
                if(dayOfMonth != Calendar.GetDaysInMonth(year, month)) return false;
            }
            else if (Flags.HasFlag(CronExpressionFlag.DayOfWeekLast))
            {
                if (dayOfMonth + 7 <= Calendar.GetDaysInMonth(year, month)) return false;
            }
            else if(_nthdayOfWeek != 0)
            {
                if ((dayOfMonth - (_nthdayOfWeek - 1) * 7 <= 0) || (dayOfMonth - _nthdayOfWeek * 7) > 0)
                {
                    return false;
                }
            }
            else if (_nearestWeekday)
            {
                isDayMatched = GetBit(_dayOfMonth, dayOfMonth) && dayOfWeek > 0 && dayOfWeek < 6 ||
                     GetBit(_dayOfMonth, dayOfMonth - 1) && dayOfWeek == 1 ||
                     GetBit(_dayOfMonth, dayOfMonth + 1) && dayOfWeek == 5 ||
                     GetBit(_dayOfMonth, 1) && dayOfWeek == 1 && (dayOfMonth == 2 || dayOfMonth == 3);

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

        public ZonedDateTime? Next(ZonedDateTime now)
        {
            return Next(now.LocalDateTime, now.Offset, now.Zone);
        }

        public ZonedDateTime? Next(LocalDateTime now, Offset currentOffset, DateTimeZone zone)
        {
            // TODO: add short path for UTC

            var mapping = zone.MapLocal(now);

            if (this.IsMatch(now))
            {
                switch (mapping.Count)
                {
                    case 0:
                        // Invalid time
                        // Interval jobs should be recalculated starting from next valid time (inclusive)
                        if (Flags.HasFlag(CronExpressionFlag.MinuteStar))
                        {
                            return Next(now.InZoneLeniently(zone));
                        }

                        // Strict jobs should be shifted to next valid time.
                        return now.InZoneLeniently(zone);
                    case 1:
                        // Strict
                        return now.InZoneStrictly(zone);
                    case 2:
                        // Ambiguous

                        // Interval jobs should be fired in both offsets

                        if (Flags.HasFlag(CronExpressionFlag.SecondStar) || Flags.HasFlag(CronExpressionFlag.MinuteStar) || Flags.HasFlag(CronExpressionFlag.HourStar))
                        {
                            return new ZonedDateTime(now, zone, currentOffset);
                        }

                        // Strict jobs should be fired in lowest offset only
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
                    // Current period, try to find anything here
                    var found = Next(now, early.IsoLocalEnd.PlusMinutes(-1));
                    if (found.HasValue)
                    {
                        return Next(found.Value, currentOffset, zone);
                    }

                    // Try to find anything starting from late offset
                    found = Next(late.IsoLocalStart, now.PlusMonths(24));
                    if (found.HasValue)
                    {
                        return Next(found.Value, late.WallOffset, zone);
                    }
                }
            }

            // Does not match, find next
            var nextFound = Next(now.PlusSeconds(1), now.PlusMonths(24));
            if (nextFound == null) return null;

            return Next(nextFound.Value, currentOffset, zone);
        }

        public LocalDateTime? Next(LocalDateTime baseTime, LocalDateTime endTime)
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

            var seconds = GetSet(_second, Constants.FirstSecond, Constants.LastSecond);
            var minutes = GetSet(_minute, Constants.FirstMinute, Constants.LastMinute);
            var hours = GetSet(_hour, Constants.FirstHour, Constants.LastHour);
            var days = GetSet(_dayOfMonth, Constants.FirstDayOfMonth, Constants.LastDayOfMonth);
            var months = GetSet(_month, Constants.FirstMonth, Constants.LastMonth);
            var daysOfWeek = GetSet(_dayOfWeek, Constants.FirstDayOfWeek, Constants.LastDayOfWeek);

            //
            // Second
            //

            var secondsView = seconds.GetViewBetween(second, Constants.LastSecond);

            if (secondsView.Count > 0)
            {
                second = secondsView.Min;
            }
            else
            {
                second = seconds.Min;
                minute++;
            }

            //
            // Minute
            //

            if (minute <= Constants.LastMinute)
            {
                var minutesView = minutes.GetViewBetween(minute, Constants.LastMinute);

                if (minutesView.Count > 0)
                {
                    minute = minutesView.Min;
                }
                else
                {
                    second = seconds.Min;
                    minute = minutes.Min;
                    hour++;
                }
            }
            else
            {
                second = seconds.Min;
                minute = minutes.Min;
                hour++;
            }

            //
            // Hour
            //

            if (hour <= Constants.LastHour)
            {
                var hoursView = hours.GetViewBetween(hour, Constants.LastHour);

                if (hoursView.Count > 0)
                {
                    hour = hoursView.Min;

                    if (hour > baseHour)
                    {
                        minute = minutes.Min;
                    }
                }
                else
                {
                    second = seconds.Min;
                    minute = minutes.Min;
                    hour = hours.Min;
                    day++;
                    if (day > Constants.LastDayOfMonth)
                    {
                        day = days.Min;
                        month++;
                    }
                }
            }
            else
            {
                second = seconds.Min;
                minute = minutes.Min;
                hour = hours.Min;
                day++;
                if (day > Constants.LastDayOfMonth)
                {
                    day = days.Min;
                    month++;
                }
            }

            //
            // Day of month
            //

            var daysView = days.GetViewBetween(day, Constants.LastDayOfMonth);

            if (daysView.Count > 0)
            {
                day = daysView.Min;
            }

            RetryDayMonth:

            if (daysView.Count == 0 || day == -1)
            {
                minute = minutes.Min;
                hour = hours.Min;
                day = days.Min;
                month++;
            }
            else if (day > baseDay)
            {
                second = seconds.Min;
                minute = minutes.Min;
                hour = hours.Min;
            }

            //
            // Month
            //
            if (month <= Constants.LastMonth)
            {
                var monthsView = months.GetViewBetween(month, Constants.LastMonth);

                if (monthsView.Count > 0)
                {
                    month = monthsView.Min;
                }

                if (monthsView.Count == 0)
                {
                    minute = minutes.Min;
                    hour = hours.Min;
                    day = days.Min;
                    month = months.Min;
                    year++;
                }
                else if (month > baseMonth)
                {
                    minute = minutes.Min;
                    hour = hours.Min;
                    day = days.Min;
                }
            }
            else
            {
                minute = minutes.Min;
                hour = hours.Min;
                day = days.Min;
                month = months.Min;
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

            var nextTime = new LocalDateTime(year, month, day, hour, minute, second, 0);

            if (nextTime > endTime)
                return null;

            //
            // L Symbol
            //

            if (Flags.HasFlag(CronExpressionFlag.DayOfMonthLast))
            {
                var lastDayOfMonth = Calendar.GetDaysInMonth(nextTime.Year, nextTime.Month);
                if (nextTime.Day == lastDayOfMonth)
                    return nextTime;

                return Next(new LocalDateTime(year, month, lastDayOfMonth, 0, 0, 0, 0), endTime);
            }
            if (Flags.HasFlag(CronExpressionFlag.DayOfWeekLast))
            {
                if (daysOfWeek.Contains(nextTime.DayOfWeek))
                {
                    if (nextTime.Month != nextTime.PlusWeeks(1).Month)
                        return nextTime;

                    return Next(new LocalDateTime(year, month, day - 1, 23, 59, 59, 59).PlusWeeks(1), endTime);
                }

                return Next(new LocalDateTime(year, month, day, 0, 0, 0, 0).PlusDays(1), endTime);
            }
            if (_nthdayOfWeek != 0)
            {
                if (daysOfWeek.Contains(nextTime.DayOfWeek))
                {
                    if (nextTime.Month != nextTime.PlusWeeks(-1 * _nthdayOfWeek).Month &&
                        nextTime.Month == nextTime.PlusWeeks(-1 * (_nthdayOfWeek - 1)).Month)
                    {
                        return nextTime;
                    }

                    return Next(new LocalDateTime(year, month, day - 1, 23, 59, 59, 59).PlusWeeks(1), endTime);
                }

                return Next(new LocalDateTime(year, month, day, 0, 0, 0, 0).PlusDays(1), endTime);
            }
            
            //
            // Day of week
            //

            if (daysOfWeek.Contains(nextTime.DayOfWeek))
                return nextTime;

            return Next(new LocalDateTime(year, month, day, 23, 59, 59, 0), endTime);
        }

        public IEnumerable<ZonedDateTime> AllNext(ZonedDateTime now, ZonedDateTime end)
        {
            ZonedDateTime? occurrence = now;

            while (true)
            {
                occurrence = Next(occurrence.Value);
                if (occurrence.HasValue)
                {
                    if (occurrence.Value > end) break;
                    yield return occurrence.Value;
                }
                else
                {
                    break;
                }

                // TODO: Handle if second is specified as star.
                occurrence = occurrence.Value.Plus(Duration.FromMinutes(1));
            }
        }

        private SortedSet<int> GetSet(long bits, int low, int high)
        {
            var result = new SortedSet<int>();
            for (var i = low; i <= high; i++)
            {
                if (GetBit(bits, i)) result.Add(i);
            }

            return result;
        }

        private static unsafe char* GetList(
          ref long bits, /* one bit per flag, default=FALSE */
          int low, int high, /* bounds, impl. offset for bitstr */
          int[] names, /* NULL or *[] of names for these elements */
          char* pointer,
          CronFieldType cronFieldType)
        {
            while (true)
            {
                if ((pointer = GetRange(ref bits, low, high, names, pointer, cronFieldType)) == null)
                {
                    return null;
                }

                if (*pointer == ',')
                {
                    pointer++;
                }
                else
                {
                    break;
                }
            }

            // exiting.  skip to some blanks, then skip over the blanks.
            /*while (*pointer != '\t' && *pointer != ' ' && *pointer != '\n' && *pointer != '\0')
            {
                pointer++;
            }*/

            while (*pointer == '\t' || *pointer == ' ')
            {
                pointer++;
            }

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
                }
                else if(*pointer == '/')
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

                // get the step size -- note: we don't pass the
                // names here, because the number is not an
                // element id, it's a step size.  'low' is
                // sent as a 0 since there is no offset either.
                if ((pointer = GetNumber(out num3, 0, null, pointer)) == null || num3 <= 0)
                {
                    return null;
                }

                // TODO: Check num3 against high
            }
            else
            {
                // no step.  default==1.
                num3 = 1;
            }

            // range. set all elements from num1 to num2, stepping
            // by num3.  (the step is a downward-compatible extension
            // proposed conceptually by bob@acornrc, syntactically
            // designed then implmented by paul vixie).
            for (var i = num1; i <= num2; i += num3)
            {
                SetBit(ref bits, i);
            }

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

                num = (num << 3) + (num << 1) + GetNumeric(*pointer++);

                if (!IsDigit(*pointer)) return pointer;

                // TODO: 123 should return false
                // TODO: 0a should return false

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
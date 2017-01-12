using System;
using System.Runtime.CompilerServices;

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

        // May be packed into 135 bits / 17 bytes, or 19 bytes unpacked + 4(2?) for Flags
        // Regular CRON string is 9 + 1(\0) bytes minimum
        private CronExpression()
        {
        }

        public CronExpressionFlag Flags { get; private set; }

        public static CronExpression Parse(string cronExpression)
        {
            if (string.IsNullOrEmpty(cronExpression)) throw new ArgumentNullException(nameof(cronExpression));

            // TODO: Add message to exception.
            //if(cronExpression.Split(' ').Length < 6) throw new FormatException();

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

                    if ((pointer = GetList(ref expression._second, Constants.FirstSecond, Constants.LastSecond, null, pointer)) == null)
                    {
                        throw new ArgumentException($"second '{cronExpression}'", nameof(cronExpression));
                    }

                    // Minutes

                    if (*pointer == '*')
                    {
                        expression.Flags |= CronExpressionFlag.MinuteStar;
                    }

                    if ((pointer = GetList(ref expression._minute, Constants.FirstMinute, Constants.LastMinute, null, pointer)) == null)
                    {
                        throw new ArgumentException($"minute '{cronExpression}'", nameof(cronExpression));
                    }

                    // Hours

                    if (*pointer == '*')
                    {
                        expression.Flags |= CronExpressionFlag.HourStar;
                    }

                    if ((pointer = GetList(ref expression._hour, Constants.FirstHour, Constants.LastHour, null, pointer)) == null)
                    {
                        throw new ArgumentException("hour", nameof(cronExpression));
                    }

                    // Days of month

                    if (*pointer == '*')
                    {
                        expression.Flags |= CronExpressionFlag.DayOfMonthStar;
                    }

                    if ((pointer = GetList(ref expression._dayOfMonth, Constants.FirstDayOfMonth, Constants.LastDayOfMonth, null, pointer)) == null)
                    {
                        throw new ArgumentException("day of month", nameof(cronExpression));
                    }

                    // Months

                    if ((pointer = GetList(ref expression._month, Constants.FirstMonth, Constants.LastMonth, Constants.MonthNamesArray, pointer)) == null)
                    {
                        throw new ArgumentException("month", nameof(cronExpression));
                    }

                    // Days of week

                    if (*pointer == '*')
                    {
                        expression.Flags |= CronExpressionFlag.DayOfWeekStar;
                    }

                    if ((pointer = GetList(ref expression._dayOfWeek, Constants.FirstDayOfWeek, Constants.LastDayOfWeek, Constants.DayOfWeekNamesArray, pointer)) == null)
                    {
                        throw new ArgumentException("day of week", nameof(cronExpression));
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

        public bool IsMatch(int seconds, int minute, int hour, int dayOfMonth, int month, int dayOfWeek)
        {
            // Make 0-based values out of these so we can use them as indicies
            // minute -= Constants.FirstMinute;
            //  hour -= Constants.FirstHour;
            // dayOfMonth -= Constants.FirstDayOfMonth;
            //  month -= Constants.FirstMonth;
            // dayOfWeek -= Constants.FirstDayOfWeek;

            // The dom/dow situation is odd:  
            //     "* * 1,15 * Sun" will run on the first and fifteenth AND every Sunday; 
            //     "* * * * Sun" will run *only* on Sundays; 
            //     "* * 1,15 * *" will run *only * the 1st and 15th.
            // this is why we keep DayOfMonthStar and DayOfWeekStar.
            // Yes, it's bizarre. Like many bizarre things, it's the standard.
            return GetBit(_minute, minute) &&
                   GetBit(_hour, hour) &&
                   GetBit(_month, month) &&
                   (((Flags & CronExpressionFlag.DayOfMonthStar) != 0) || ((Flags & CronExpressionFlag.DayOfWeekStar) != 0)
                       ? GetBit(_dayOfWeek, dayOfWeek) && GetBit(_dayOfMonth, dayOfMonth)
                       : GetBit(_dayOfWeek, dayOfWeek) || GetBit(_dayOfMonth, dayOfMonth));
        }

        private static unsafe char* GetList(
          ref long bits, /* one bit per flag, default=FALSE */
          int low, int high, /* bounds, impl. offset for bitstr */
          int[] names, /* NULL or *[] of names for these elements */
          char* pointer)
        {
            while (true)
            {
                if ((pointer = GetRange(ref bits, low, high, names, pointer)) == null)
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
            ref long bits, int low, int high, int[] names, char* pointer)
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

                if (*pointer != '-')
                {
                    /* not a range, it's a single number. */

                    // Unsupported syntax: Step specified without range,
                    // eg:   1/20 * * * *
                    if (*pointer == '/') return null;

                    SetBit(ref bits, num1);

                    return pointer;
                }

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
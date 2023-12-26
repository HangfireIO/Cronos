// The MIT License(MIT)
// 
// Copyright (c) 2023 Hangfire OÜ
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Cronos
{
    internal static class CronParser
    {
        private const int MinNthDayOfWeek = 1;
        private const int MaxNthDayOfWeek = 5;
        private const int SundayBits = 0b1000_0001;

        private static readonly CronExpression Yearly = Parse("0 0 1 1 *", CronFormat.Standard);
        private static readonly CronExpression Weekly = Parse("0 0 * * 0", CronFormat.Standard);
        private static readonly CronExpression Monthly = Parse("0 0 1 * *", CronFormat.Standard);
        private static readonly CronExpression Daily = Parse("0 0 * * *", CronFormat.Standard);
        private static readonly CronExpression Hourly = Parse("0 * * * *", CronFormat.Standard);
        private static readonly CronExpression Minutely = Parse("* * * * *", CronFormat.Standard);
        private static readonly CronExpression Secondly = Parse("* * * * * *", CronFormat.IncludeSeconds);

        public static unsafe CronExpression Parse(string expression, CronFormat format)
        {
            fixed (char* value = expression)
            {
                var pointer = value;

                SkipWhiteSpaces(ref pointer);

                if (Accept(ref pointer, '@'))
                {
                    var cronExpression = ParseMacro(ref pointer);
                    SkipWhiteSpaces(ref pointer);

                    if (cronExpression == null || !IsEndOfString(*pointer)) ThrowFormatException("Macro: Unexpected character '{0}' on position {1}.", *pointer, pointer - value);
                    return cronExpression;
                }

                long  second = default;
                byte  nthDayOfWeek = default;
                byte  lastMonthOffset = default;

                CronExpressionFlag flags = default;

                if (format == CronFormat.IncludeSeconds)
                {
                    second = ParseField(CronField.Seconds, ref pointer, ref flags);
                    ParseWhiteSpace(CronField.Seconds, ref pointer);
                }
                else
                {
                    SetBit(ref second, CronField.Seconds.First);
                }

                var minute = ParseField(CronField.Minutes, ref pointer, ref flags);
                ParseWhiteSpace(CronField.Minutes, ref pointer);

                var hour = (int)ParseField(CronField.Hours, ref pointer, ref flags);
                ParseWhiteSpace(CronField.Hours, ref pointer);

                var dayOfMonth = (int)ParseDayOfMonth(ref pointer, ref flags, ref lastMonthOffset);
                ParseWhiteSpace(CronField.DaysOfMonth, ref pointer);

                var month = (short)ParseField(CronField.Months, ref pointer, ref flags);
                ParseWhiteSpace(CronField.Months, ref pointer);

                var dayOfWeek = (byte)ParseDayOfWeek(ref pointer, ref flags, ref nthDayOfWeek);
                ParseEndOfString(ref pointer);

                // Make sundays equivalent.
                if ((dayOfWeek & SundayBits) != 0)
                {
                    dayOfWeek |= SundayBits;
                }

                return new CronExpression(
                    second,
                    minute,
                    hour,
                    dayOfMonth,
                    month,
                    dayOfWeek,
                    nthDayOfWeek,
                    lastMonthOffset,
                    flags);
            }
        }

        private static unsafe void SkipWhiteSpaces(ref char* pointer)
        {
            while (IsWhiteSpace(*pointer)) { pointer++; }
        }

        private static unsafe void ParseWhiteSpace(CronField prevField, ref char* pointer)
        {
            if (!IsWhiteSpace(*pointer)) ThrowFormatException(prevField, "Unexpected character '{0}'.", *pointer);
            SkipWhiteSpaces(ref pointer);
        }

        private static unsafe void ParseEndOfString(ref char* pointer)
        {
            if (!IsWhiteSpace(*pointer) && !IsEndOfString(*pointer)) ThrowFormatException(CronField.DaysOfWeek, "Unexpected character '{0}'.", *pointer);

            SkipWhiteSpaces(ref pointer);
            if (!IsEndOfString(*pointer)) ThrowFormatException("Unexpected character '{0}'.", *pointer);
        }

        [SuppressMessage("SonarLint", "S1764:IdenticalExpressionsShouldNotBeUsedOnBothSidesOfOperators", Justification = "Expected, as the AcceptCharacter method produces side effects.")]
        private static unsafe CronExpression ParseMacro(ref char* pointer)
        {
            switch (ToUpper(*pointer++))
            {
                case 'A':
                    if (AcceptCharacter(ref pointer, 'N') &&
                        AcceptCharacter(ref pointer, 'N') &&
                        AcceptCharacter(ref pointer, 'U') &&
                        AcceptCharacter(ref pointer, 'A') &&
                        AcceptCharacter(ref pointer, 'L') &&
                        AcceptCharacter(ref pointer, 'L') &&
                        AcceptCharacter(ref pointer, 'Y'))
                        return Yearly;
                    return null;
                case 'D':
                    if (AcceptCharacter(ref pointer, 'A') &&
                        AcceptCharacter(ref pointer, 'I') &&
                        AcceptCharacter(ref pointer, 'L') &&
                        AcceptCharacter(ref pointer, 'Y'))
                        return Daily;
                    return null;
                case 'E':
                    if (AcceptCharacter(ref pointer, 'V') &&
                        AcceptCharacter(ref pointer, 'E') &&
                        AcceptCharacter(ref pointer, 'R') &&
                        AcceptCharacter(ref pointer, 'Y') &&
                        Accept(ref pointer, '_'))
                    {
                        if (AcceptCharacter(ref pointer, 'M') &&
                            AcceptCharacter(ref pointer, 'I') &&
                            AcceptCharacter(ref pointer, 'N') &&
                            AcceptCharacter(ref pointer, 'U') &&
                            AcceptCharacter(ref pointer, 'T') &&
                            AcceptCharacter(ref pointer, 'E'))
                            return Minutely;

                        if (*(pointer - 1) != '_') return null;

                        if (AcceptCharacter(ref pointer, 'S') &&
                            AcceptCharacter(ref pointer, 'E') &&
                            AcceptCharacter(ref pointer, 'C') &&
                            AcceptCharacter(ref pointer, 'O') &&
                            AcceptCharacter(ref pointer, 'N') &&
                            AcceptCharacter(ref pointer, 'D'))
                            return Secondly;
                    }

                    return null;
                case 'H':
                    if (AcceptCharacter(ref pointer, 'O') &&
                        AcceptCharacter(ref pointer, 'U') &&
                        AcceptCharacter(ref pointer, 'R') &&
                        AcceptCharacter(ref pointer, 'L') &&
                        AcceptCharacter(ref pointer, 'Y'))
                        return Hourly;
                    return null;
                case 'M':
                    if (AcceptCharacter(ref pointer, 'O') &&
                        AcceptCharacter(ref pointer, 'N') &&
                        AcceptCharacter(ref pointer, 'T') &&
                        AcceptCharacter(ref pointer, 'H') &&
                        AcceptCharacter(ref pointer, 'L') &&
                        AcceptCharacter(ref pointer, 'Y'))
                        return Monthly;

                    if (ToUpper(*(pointer - 1)) == 'M' &&
                        AcceptCharacter(ref pointer, 'I') &&
                        AcceptCharacter(ref pointer, 'D') &&
                        AcceptCharacter(ref pointer, 'N') &&
                        AcceptCharacter(ref pointer, 'I') &&
                        AcceptCharacter(ref pointer, 'G') &&
                        AcceptCharacter(ref pointer, 'H') &&
                        AcceptCharacter(ref pointer, 'T'))
                        return Daily;

                    return null;
                case 'W':
                    if (AcceptCharacter(ref pointer, 'E') &&
                        AcceptCharacter(ref pointer, 'E') &&
                        AcceptCharacter(ref pointer, 'K') &&
                        AcceptCharacter(ref pointer, 'L') &&
                        AcceptCharacter(ref pointer, 'Y'))
                        return Weekly;
                    return null;
                case 'Y':
                    if (AcceptCharacter(ref pointer, 'E') &&
                        AcceptCharacter(ref pointer, 'A') &&
                        AcceptCharacter(ref pointer, 'R') &&
                        AcceptCharacter(ref pointer, 'L') &&
                        AcceptCharacter(ref pointer, 'Y'))
                        return Yearly;
                    return null;
                default:
                    pointer--;
                    return null;
            }
        }

        private static unsafe long ParseField(CronField field, ref char* pointer, ref CronExpressionFlag flags)
        {
            if (Accept(ref pointer, '*') || Accept(ref pointer, '?'))
            {
                if (field.CanDefineInterval) flags |= CronExpressionFlag.Interval;
                return ParseStar(field, ref pointer);
            }

            var num = ParseValue(field, ref pointer);

            var bits = ParseRange(field, ref pointer, num, ref flags);
            if (Accept(ref pointer, ',')) bits |= ParseList(field, ref pointer, ref flags);

            return bits;
        }

        private static unsafe long ParseDayOfMonth(ref char* pointer, ref CronExpressionFlag flags, ref byte lastDayOffset)
        {
            var field = CronField.DaysOfMonth;

            if (Accept(ref pointer, '*') || Accept(ref pointer, '?')) return ParseStar(field, ref pointer);

            if (AcceptCharacter(ref pointer, 'L')) return ParseLastDayOfMonth(field, ref pointer, ref flags, ref lastDayOffset);

            var dayOfMonth = ParseValue(field, ref pointer);

            if (AcceptCharacter(ref pointer, 'W'))
            {
                flags |= CronExpressionFlag.NearestWeekday;
                return GetBit(dayOfMonth);
            }

            var bits = ParseRange(field, ref pointer, dayOfMonth, ref flags);
            if (Accept(ref pointer, ',')) bits |= ParseList(field, ref pointer, ref flags);

            return bits;
        }

        private static unsafe long ParseDayOfWeek(ref char* pointer, ref CronExpressionFlag flags, ref byte nthWeekDay)
        {
            var field = CronField.DaysOfWeek;
            if (Accept(ref pointer, '*') || Accept(ref pointer, '?')) return ParseStar(field, ref pointer);

            var dayOfWeek = ParseValue(field, ref pointer);

            if (AcceptCharacter(ref pointer, 'L')) return ParseLastWeekDay(dayOfWeek, ref flags);
            if (Accept(ref pointer, '#')) return ParseNthWeekDay(field, ref pointer, dayOfWeek, ref flags, out nthWeekDay);

            var bits = ParseRange(field, ref pointer, dayOfWeek, ref flags);
            if (Accept(ref pointer, ',')) bits |= ParseList(field, ref pointer, ref flags);

            return bits;
        }

        private static unsafe long ParseStar(CronField field, ref char* pointer)
        {
            return Accept(ref pointer, '/')
                ? ParseStep(field, ref pointer, field.First, field.Last)
                : field.AllBits;
        }

        private static unsafe long ParseList(CronField field, ref char* pointer, ref CronExpressionFlag flags)
        {
            var num = ParseValue(field, ref pointer);
            var bits = ParseRange(field, ref pointer, num, ref flags);

            do
            {
                if (!Accept(ref pointer, ',')) return bits;

                bits |= ParseList(field, ref pointer, ref flags);
            } while (true);
        }

        private static unsafe long ParseRange(CronField field, ref char* pointer, int low, ref CronExpressionFlag flags)
        {
            if (!Accept(ref pointer, '-'))
            {
                if (!Accept(ref pointer, '/')) return GetBit(low);

                if (field.CanDefineInterval) flags |= CronExpressionFlag.Interval;
                return ParseStep(field, ref pointer, low, field.Last);
            }

            if (field.CanDefineInterval) flags |= CronExpressionFlag.Interval;

            var high = ParseValue(field, ref pointer);
            if (Accept(ref pointer, '/')) return ParseStep(field, ref pointer, low, high);
            return GetBits(field, low, high, 1);
        }

        private static unsafe long ParseStep(CronField field, ref char* pointer, int low, int high)
        {
            // Get the step size -- note: we don't pass the
            // names here, because the number is not an
            // element id, it's a step size.  'low' is
            // sent as a 0 since there is no offset either.
            var step = ParseNumber(field, ref pointer, 1, field.Last);
            return GetBits(field, low, high, step);
        }

        private static unsafe long ParseLastDayOfMonth(CronField field, ref char* pointer, ref CronExpressionFlag flags, ref byte lastMonthOffset)
        {
            flags |= CronExpressionFlag.DayOfMonthLast;

            if (Accept(ref pointer, '-')) lastMonthOffset = (byte)ParseNumber(field, ref pointer, 0, field.Last - 1);
            if (AcceptCharacter(ref pointer, 'W')) flags |= CronExpressionFlag.NearestWeekday;
            return field.AllBits;
        }

        private static unsafe long ParseNthWeekDay(CronField field, ref char* pointer, int dayOfWeek, ref CronExpressionFlag flags, out byte nthDayOfWeek)
        {
            nthDayOfWeek = (byte)ParseNumber(field, ref pointer, MinNthDayOfWeek, MaxNthDayOfWeek);
            flags |= CronExpressionFlag.NthDayOfWeek;
            return GetBit(dayOfWeek);
        }

        private static long ParseLastWeekDay(int dayOfWeek, ref CronExpressionFlag flags)
        {
            flags |= CronExpressionFlag.DayOfWeekLast;
            return GetBit(dayOfWeek);
        }

        private static unsafe bool Accept(ref char* pointer, char character)
        {
            if (*pointer == character)
            {
                pointer++;
                return true;
            }

            return false;
        }

        private static unsafe bool AcceptCharacter(ref char* pointer, char character)
        {
            if (ToUpper(*pointer) == character)
            {
                pointer++;
                return true;
            }

            return false;
        }

        private static unsafe int ParseNumber(CronField field, ref char* pointer, int low, int high)
        {
            var num = GetNumber(ref pointer, null);
            if (num == -1 || num < low || num > high)
            {
                ThrowFormatException(field, "Value must be a number between {0} and {1} (all inclusive).", low, high);
            }
            return num;
        }

        private static unsafe int ParseValue(CronField field, ref char* pointer)
        {
            var num = GetNumber(ref pointer, field.Names);
            if (num == -1 || num < field.First || num > field.Last)
            {
                ThrowFormatException(field, "Value must be a number between {0} and {1} (all inclusive).", field.First, field.Last);
            }
            return num;
        }

        private static long GetBits(CronField field, int num1, int num2, int step)
        {
            if (num2 < num1) return GetReversedRangeBits(field, num1, num2, step);
            if (step == 1) return (1L << (num2 + 1)) - (1L << num1);

            return GetRangeBits(num1, num2, step);
        }

        private static long GetRangeBits(int low, int high, int step)
        {
            var bits = 0L;
            for (var i = low; i <= high; i += step)
            {
                SetBit(ref bits, i);
            }
            return bits;
        }

        private static long GetReversedRangeBits(CronField field, int num1, int num2, int step)
        {
            var high = field.Last;
            // Skip one of sundays.
            if (field == CronField.DaysOfWeek) high--;

            var bits = GetRangeBits(num1, high, step);
            
            num1 = field.First + step - (high - num1) % step - 1;
            return bits | GetRangeBits(num1, num2, step);
        }

        private static long GetBit(int num1)
        {
            return 1L << num1;
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

        private static void SetBit(ref long value, int index)
        {
            value |= 1L << index;
        }

        private static bool IsEndOfString(int code)
        {
            return code == '\0';
        }

        private static bool IsWhiteSpace(int code)
        {
            return code == '\t' || code == ' ';
        }

        private static bool IsDigit(int code)
        {
            return code >= 48 && code <= 57;
        }

        private static bool IsLetter(int code)
        {
            return (code >= 65 && code <= 90) || (code >= 97 && code <= 122);
        }

        private static int GetNumeric(int code)
        {
            return code - 48;
        }

        private static int ToUpper(int code)
        {
            if (code >= 97 && code <= 122)
            {
                return code - 32;
            }

            return code;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowFormatException(CronField field, string format, params object[] args)
        {
            throw new CronFormatException(field, String.Format(CultureInfo.CurrentCulture, format, args));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowFormatException(string format, params object[] args)
        {
            throw new CronFormatException(String.Format(CultureInfo.CurrentCulture, format, args));
        }
    }
}
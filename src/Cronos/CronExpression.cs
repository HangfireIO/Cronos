// The MIT License(MIT)
// 
// Copyright (c) 2017 Hangfire OÜ
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
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;

namespace Cronos
{
    /// <summary>
    /// Provides a parser and scheduler for cron expressions.
    /// </summary>
    public sealed class CronExpression: IEquatable<CronExpression>
    {
        private const long NotFound = -1;

        /// <summary>
        /// Represents a cron expression that fires on Jan 1st every year at midnight.
        /// Equals to "0 0 1 1 *".
        /// </summary>
        public static readonly CronExpression Yearly = Parse("0 0 1 1 *", CronFormat.Standard);
        
        /// <summary>
        /// Represents a cron expression that fires at an unspecified time once per year.
        /// Equals to "H H H H H *".
        /// </summary>
        public static CronExpression YearlyWithJitter(int jitterSeed) =>
            Parse("H H H H H *", CronFormat.IncludeSeconds, jitterSeed);

        /// <summary>
        /// Represents a cron expression that fires every Sunday at midnight.
        /// Equals to "0 0 * * 0".
        /// </summary>
        public static readonly CronExpression Weekly = Parse("0 0 * * 0", CronFormat.Standard);

        /// <summary>
        /// Represents a cron expression that fires at an unspecified time once per week.
        /// Equals to "H H H * * H".
        /// </summary>
        public static CronExpression WeeklyWithJitter(int jitterSeed) =>
            Parse("H H H * * H", CronFormat.IncludeSeconds, jitterSeed);

        /// <summary>
        /// Represents a cron expression that fires on 1st day of every month at midnight.
        /// Equals to "0 0 1 * *".
        /// </summary>
        public static readonly CronExpression Monthly = Parse("0 0 1 * *", CronFormat.Standard);

        /// <summary>
        /// Represents a cron expression that fires at an unspecified time once per month.
        /// Equals to "H H H H * *".
        /// </summary>
        public static CronExpression MonthlyWithJitter(int jitterSeed) =>
            Parse("H H H H * *", CronFormat.IncludeSeconds, jitterSeed);

        /// <summary>
        /// Represents a cron expression that fires every day at midnight.
        /// Equals to "0 0 * * *".
        /// </summary>
        public static readonly CronExpression Daily = Parse("0 0 * * *", CronFormat.Standard);

        /// <summary>
        /// Represents a cron expression that fires at an unspecified time every day.
        /// Equals to "H H H * * *".
        /// </summary>
        public static CronExpression DailyWithJitter(int jitterSeed) =>
            Parse("H H H * * *", CronFormat.IncludeSeconds, jitterSeed);
        
        /// <summary>
        /// Represents a cron expression that fires every hour at the beginning of the hour.
        /// Equals to "0 * * * *".
        /// </summary>
        public static readonly CronExpression Hourly = Parse("0 * * * *", CronFormat.Standard);
        
        /// <summary>
        /// Represents a cron expression that fires at an unspecified time every hour.
        /// Equals to "H H * * * *".
        /// </summary>
        public static CronExpression HourlyWithJitter(int jitterSeed) =>
            Parse("H H * * * *", CronFormat.IncludeSeconds, jitterSeed);
        
        /// <summary>
        /// Represents a cron expression that fires every minute.
        /// Equals to "* * * * *".
        /// </summary>
        public static readonly CronExpression EveryMinute = Parse("* * * * *", CronFormat.Standard);
        
        /// <summary>
        /// Represents a cron expression that fires at an unspecified second every minute.
        /// Equals to "H * * * * *".
        /// </summary>
        public static CronExpression EveryMinuteWithJitter(int jitterSeed) =>
            Parse("H * * * * *", CronFormat.IncludeSeconds, jitterSeed);

        /// <summary>
        /// Represents a cron expression that fires every second.
        /// Equals to "* * * * * *". 
        /// </summary>
        public static readonly CronExpression EverySecond = Parse("* * * * * *", CronFormat.IncludeSeconds);

        private static readonly TimeZoneInfo UtcTimeZone = TimeZoneInfo.Utc;

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

        private readonly ulong  _second;     // 60 bits -> from 0 bit to 59 bit
        private readonly ulong  _minute;     // 60 bits -> from 0 bit to 59 bit
        private readonly uint   _hour;       // 24 bits -> from 0 bit to 23 bit
        private readonly uint   _dayOfMonth; // 31 bits -> from 1 bit to 31 bit
        private readonly ushort _month;      // 12 bits -> from 1 bit to 12 bit
        private readonly byte  _dayOfWeek;  // 8 bits  -> from 0 bit to 7 bit

        private readonly byte  _nthDayOfWeek;
        private readonly byte  _lastMonthOffset;

        private readonly CronExpressionFlag _flags;

        internal CronExpression(
            ulong second,
            ulong minute,
            uint hour,
            uint dayOfMonth,
            ushort month,
            byte dayOfWeek,
            byte nthDayOfWeek,
            byte lastMonthOffset,
            CronExpressionFlag flags)
        {
            _second = second;
            _minute = minute;
            _hour = hour;
            _dayOfMonth = dayOfMonth;
            _month = month;
            _dayOfWeek = dayOfWeek;
            _nthDayOfWeek = nthDayOfWeek;
            _lastMonthOffset = lastMonthOffset;
            _flags = flags;
        }

        ///<summary>
        /// Constructs a new <see cref="CronExpression"/> based on the specified
        /// cron expression. Its supported expressions consist of 5 fields:
        /// minute, hour, day of month, month, day of week. 
        /// See more: <a href="https://github.com/HangfireIO/Cronos">https://github.com/HangfireIO/Cronos</a>
        /// </summary>
        public static CronExpression Parse(string expression)
        {
            return Parse(expression, CronFormat.Standard);
        }

        ///<summary>
        /// Constructs a new <see cref="CronExpression"/> based on the specified
        /// cron expression and jitter seed. Its supported expressions consist of 5 fields:
        /// minute, hour, day of month, month, day of week. 
        /// See more: <a href="https://github.com/HangfireIO/Cronos">https://github.com/HangfireIO/Cronos</a>
        /// </summary>
        public static CronExpression Parse(string expression, int jitterSeed)
        {
            return Parse(expression, CronFormat.Standard, jitterSeed);
        }

        ///<summary>
        /// Constructs a new <see cref="CronExpression"/> based on the specified
        /// cron expression. Its supported expressions consist of 5 or 6 fields:
        /// second (optional), minute, hour, day of month, month, day of week. 
        /// See more: <a href="https://github.com/HangfireIO/Cronos">https://github.com/HangfireIO/Cronos</a>
        /// </summary>
        public static CronExpression Parse(string expression, CronFormat format)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(expression);
#else
            if (expression == null) throw new ArgumentNullException(nameof(expression));
#endif

            return CronParser.Parse(expression, format);
        }

        ///<summary>
        /// Constructs a new <see cref="CronExpression"/> based on the specified
        /// cron expression and jitter seed. Its supported expressions consist of 5 or 6 fields:
        /// second (optional), minute, hour, day of month, month, day of week. 
        /// See more: <a href="https://github.com/HangfireIO/Cronos">https://github.com/HangfireIO/Cronos</a>
        /// </summary>
        public static CronExpression Parse(string expression, CronFormat format, int jitterSeed)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(expression);
#else
            if (expression == null) throw new ArgumentNullException(nameof(expression));
#endif

            return CronParser.Parse(expression, format, jitterSeed);
        }

        /// <summary>
        /// Constructs a new <see cref="CronExpression"/> based on the specified cron expression with the
        /// <see cref="CronFormat.Standard"/> format.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        public static bool TryParse(string expression, [MaybeNullWhen(returnValue: false)] out CronExpression cronExpression)
        {
            return TryParse(expression, CronFormat.Standard, out cronExpression);
        }

        /// <summary>
        /// Constructs a new <see cref="CronExpression"/> based on the specified cron expression and jitter seed with
        /// the <see cref="CronFormat.Standard"/> format.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        public static bool TryParse(string expression, int jitterSeed, [MaybeNullWhen(returnValue: false)] out CronExpression cronExpression)
        {
            return TryParse(expression, CronFormat.Standard, jitterSeed, out cronExpression);
        }

        /// <summary>
        /// Constructs a new <see cref="CronExpression"/> based on the specified cron expression with the specified
        /// <paramref name="format"/>.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        public static bool TryParse(string expression, CronFormat format, [MaybeNullWhen(returnValue: false)] out CronExpression cronExpression)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(expression);
#else
            if (expression == null) throw new ArgumentNullException(nameof(expression));
#endif

            try
            {
                cronExpression = Parse(expression, format);
                return true;
            }
            catch (CronFormatException)
            {
                cronExpression = null;
                return false;
            }
            catch (MissingSeedException)
            {
                cronExpression = null;
                return false;
            }
        }

        /// <summary>
        /// Constructs a new <see cref="CronExpression"/> based on the specified cron expression and jitter seed with
        /// the specified <paramref name="format"/>.
        /// A return value indicates whether the operation succeeded.
        /// </summary>
        public static bool TryParse(string expression, CronFormat format, int jitterSeed, [MaybeNullWhen(returnValue: false)] out CronExpression cronExpression)
        {
#if NET6_0_OR_GREATER
            ArgumentNullException.ThrowIfNull(expression);
#else
            if (expression == null) throw new ArgumentNullException(nameof(expression));
#endif

            try
            {
                cronExpression = Parse(expression, format, jitterSeed);
                return true;
            }
            catch (CronFormatException)
            {
                cronExpression = null;
                return false;
            }
            catch (MissingSeedException)
            {
                cronExpression = null;
                return false;
            }
        }

        /// <summary>
        /// Calculates next occurrence starting with <paramref name="fromUtc"/> (optionally <paramref name="inclusive"/>) in UTC time zone.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public DateTime? GetNextOccurrence(DateTime fromUtc, bool inclusive = false)
        {
            if (fromUtc.Kind != DateTimeKind.Utc) ThrowWrongDateTimeKindException(nameof(fromUtc));

            var found = FindOccurrence(fromUtc.Ticks, inclusive);
            if (found == NotFound) return null;

            return new DateTime(found, DateTimeKind.Utc);
        }

        /// <summary>
        /// Calculates previous occurrence starting with <paramref name="fromUtc"/> (optionally <paramref name="inclusive"/>) in UTC time zone.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public DateTime? GetPreviousOccurrence(DateTime fromUtc, bool inclusive = false)
        {
            if (fromUtc.Kind != DateTimeKind.Utc) ThrowWrongDateTimeKindException(nameof(fromUtc));

            var found = FindPreviousOccurrence(fromUtc.Ticks, inclusive);
            if (found == NotFound) return null;

            return new DateTime(found, DateTimeKind.Utc);
        }

        /// <summary>
        /// Calculates next occurrence starting with <paramref name="fromUtc"/> (optionally <paramref name="inclusive"/>) in given <paramref name="zone"/>
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public DateTime? GetNextOccurrence(DateTime fromUtc, TimeZoneInfo zone, bool inclusive = false)
        {
            if (fromUtc.Kind != DateTimeKind.Utc) ThrowWrongDateTimeKindException(nameof(fromUtc));
            if (ReferenceEquals(zone, null)) ThrowArgumentNullException(nameof(zone));

            if (ReferenceEquals(zone, UtcTimeZone))
            {
                var found = FindOccurrence(fromUtc.Ticks, inclusive);
                if (found == NotFound) return null;

                return new DateTime(found, DateTimeKind.Utc);
            }

            var fromOffset = new DateTimeOffset(fromUtc);

#pragma warning disable CA1062
            var occurrence = GetOccurrenceConsideringTimeZone(fromOffset, zone, inclusive);
#pragma warning restore CA1062

            return occurrence?.UtcDateTime;
        }

        /// <summary>
        /// Calculates previous occurrence starting with <paramref name="fromUtc"/> (optionally <paramref name="inclusive"/>) in given <paramref name="zone"/>
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public DateTime? GetPreviousOccurrence(DateTime fromUtc, TimeZoneInfo zone, bool inclusive = false)
        {
            if (fromUtc.Kind != DateTimeKind.Utc) ThrowWrongDateTimeKindException(nameof(fromUtc));
            if (ReferenceEquals(zone, null)) ThrowArgumentNullException(nameof(zone));

            if (ReferenceEquals(zone, UtcTimeZone))
            {
                var found = FindPreviousOccurrence(fromUtc.Ticks, inclusive);
                if (found == NotFound) return null;

                return new DateTime(found, DateTimeKind.Utc);
            }

            var fromOffset = new DateTimeOffset(fromUtc);

#pragma warning disable CA1062
            var occurrence = GetPreviousOccurrenceConsideringTimeZone(fromOffset, zone, inclusive);
#pragma warning restore CA1062

            return occurrence?.UtcDateTime;
        }

        /// <summary>
        /// Calculates next occurrence starting with <paramref name="from"/> (optionally <paramref name="inclusive"/>) in given <paramref name="zone"/>
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public DateTimeOffset? GetNextOccurrence(DateTimeOffset from, TimeZoneInfo zone, bool inclusive = false)
        {
            if (ReferenceEquals(zone, null)) ThrowArgumentNullException(nameof(zone));

            if (ReferenceEquals(zone, UtcTimeZone))
            {
                var found = FindOccurrence(from.UtcTicks, inclusive);
                if (found == NotFound) return null;

                return new DateTimeOffset(found, TimeSpan.Zero);
            }

#pragma warning disable CA1062
            return GetOccurrenceConsideringTimeZone(from, zone, inclusive);
#pragma warning restore CA1062
        }

        /// <summary>
        /// Calculates previous occurrence starting with <paramref name="from"/> (optionally <paramref name="inclusive"/>) in given <paramref name="zone"/>
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public DateTimeOffset? GetPreviousOccurrence(DateTimeOffset from, TimeZoneInfo zone, bool inclusive = false)
        {
            if (ReferenceEquals(zone, null)) ThrowArgumentNullException(nameof(zone));

            if (ReferenceEquals(zone, UtcTimeZone))
            {
                var found = FindPreviousOccurrence(from.UtcTicks, inclusive);
                if (found == NotFound) return null;

                return new DateTimeOffset(found, TimeSpan.Zero);
            }

#pragma warning disable CA1062
            return GetPreviousOccurrenceConsideringTimeZone(from, zone, inclusive);
#pragma warning restore CA1062
        }

        /// <summary>
        /// Returns the list of next occurrences within the given date/time range,
        /// including <paramref name="fromUtc"/> and excluding <paramref name="toUtc"/>
        /// by default, and UTC time zone. When none of the occurrences found, an 
        /// empty list is returned.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public IEnumerable<DateTime> GetOccurrences(
            DateTime fromUtc,
            DateTime toUtc,
            bool fromInclusive = true,
            bool toInclusive = false)
        {
            if (fromUtc > toUtc) ThrowFromShouldBeLessThanToException(nameof(fromUtc), nameof(toUtc));

            for (var occurrence = GetNextOccurrence(fromUtc, fromInclusive);
                occurrence < toUtc || occurrence == toUtc && toInclusive;
                // ReSharper disable once RedundantArgumentDefaultValue
                // ReSharper disable once ArgumentsStyleLiteral
                occurrence = GetNextOccurrence(occurrence.Value, inclusive: false))
            {
                yield return occurrence.Value;
            }
        }

        /// <summary>
        /// Returns the list of next occurrences within the given date/time range, including
        /// <paramref name="fromUtc"/> and excluding <paramref name="toUtc"/> by default, and 
        /// specified time zone. When none of the occurrences found, an empty list is returned.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public IEnumerable<DateTime> GetOccurrences(
            DateTime fromUtc,
            DateTime toUtc,
            TimeZoneInfo zone,
            bool fromInclusive = true,
            bool toInclusive = false)
        {
            if (fromUtc > toUtc) ThrowFromShouldBeLessThanToException(nameof(fromUtc), nameof(toUtc));

            for (var occurrence = GetNextOccurrence(fromUtc, zone, fromInclusive);
                occurrence < toUtc || occurrence == toUtc && toInclusive;
                // ReSharper disable once RedundantArgumentDefaultValue
                // ReSharper disable once ArgumentsStyleLiteral
                occurrence = GetNextOccurrence(occurrence.Value, zone, inclusive: false))
            {
                yield return occurrence.Value;
            }
        }

        /// <summary>
        /// Returns the list of occurrences within the given date/time offset range,
        /// including <paramref name="from"/> and excluding <paramref name="to"/> by
        /// default. When none of the occurrences found, an empty list is returned.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public IEnumerable<DateTimeOffset> GetOccurrences(
            DateTimeOffset from,
            DateTimeOffset to,
            TimeZoneInfo zone,
            bool fromInclusive = true,
            bool toInclusive = false)
        {
            if (from > to) ThrowFromShouldBeLessThanToException(nameof(from), nameof(to));

            for (var occurrence = GetNextOccurrence(from, zone, fromInclusive);
                occurrence < to || occurrence == to && toInclusive;
                // ReSharper disable once RedundantArgumentDefaultValue
                // ReSharper disable once ArgumentsStyleLiteral
                occurrence = GetNextOccurrence(occurrence.Value, zone, inclusive: false))
            {
                yield return occurrence.Value;
            }
        }

        /// <summary>
        /// Returns the list of previous occurrences within the given date/time range,
        /// including <paramref name="fromUtc"/> and excluding <paramref name="toUtc"/>
        /// by default, and UTC time zone. When none of the occurrences found, an
        /// empty list is returned.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public IEnumerable<DateTime> GetOccurrencesDescending(
            DateTime fromUtc,
            DateTime toUtc,
            bool fromInclusive = true,
            bool toInclusive = false)
        {
            if (fromUtc < toUtc) ThrowFromShouldBeGreaterThanToException(nameof(fromUtc), nameof(toUtc));

            for (var occurrence = GetPreviousOccurrence(fromUtc, fromInclusive);
                occurrence > toUtc || occurrence == toUtc && toInclusive;
                occurrence = GetPreviousOccurrence(occurrence.Value, inclusive: false))
            {
                yield return occurrence.Value;
            }
        }

        /// <summary>
        /// Returns the list of previous occurrences within the given date/time range, including
        /// <paramref name="fromUtc"/> and excluding <paramref name="toUtc"/> by default, and
        /// specified time zone. When none of the occurrences found, an empty list is returned.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public IEnumerable<DateTime> GetOccurrencesDescending(
            DateTime fromUtc,
            DateTime toUtc,
            TimeZoneInfo zone,
            bool fromInclusive = true,
            bool toInclusive = false)
        {
            if (fromUtc < toUtc) ThrowFromShouldBeGreaterThanToException(nameof(fromUtc), nameof(toUtc));

            for (var occurrence = GetPreviousOccurrence(fromUtc, zone, fromInclusive);
                occurrence > toUtc || occurrence == toUtc && toInclusive;
                occurrence = GetPreviousOccurrence(occurrence.Value, zone, inclusive: false))
            {
                yield return occurrence.Value;
            }
        }

        /// <summary>
        /// Returns the list of previous occurrences within the given date/time offset range,
        /// including <paramref name="from"/> and excluding <paramref name="to"/> by default.
        /// When none of the occurrences found, an empty list is returned.
        /// </summary>
        /// <exception cref="ArgumentException"/>
        public IEnumerable<DateTimeOffset> GetOccurrencesDescending(
            DateTimeOffset from,
            DateTimeOffset to,
            TimeZoneInfo zone,
            bool fromInclusive = true,
            bool toInclusive = false)
        {
            if (from < to) ThrowFromShouldBeGreaterThanToException(nameof(from), nameof(to));

            for (var occurrence = GetPreviousOccurrence(from, zone, fromInclusive);
                occurrence > to || occurrence == to && toInclusive;
                occurrence = GetPreviousOccurrence(occurrence.Value, zone, inclusive: false))
            {
                yield return occurrence.Value;
            }
        }

        /// <inheritdoc />
        public override string ToString()
        {
            var expressionBuilder = new StringBuilder();

            if (_second != 1UL)
            {
                AppendFieldValue(expressionBuilder, CronField.Seconds, _second).Append(' ');
            }

            AppendFieldValue(expressionBuilder, CronField.Minutes, _minute).Append(' ');
            AppendFieldValue(expressionBuilder, CronField.Hours, _hour).Append(' ');
            AppendDayOfMonth(expressionBuilder, _dayOfMonth).Append(' ');
            AppendFieldValue(expressionBuilder, CronField.Months, _month).Append(' ');
            AppendDayOfWeek(expressionBuilder, _dayOfWeek);

            return expressionBuilder.ToString();
        }

        /// <summary>
        /// Determines whether the specified <see cref="Object"/> is equal to the current <see cref="Object"/>.
        /// </summary>
        /// <param name="other">The <see cref="Object"/> to compare with the current <see cref="Object"/>.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="Object"/> is equal to the current <see cref="Object"/>; otherwise, <c>false</c>.
        /// </returns>
        public bool Equals(CronExpression? other)
        {
            if (ReferenceEquals(other, null)) return false;

            return _second == other._second &&
                   _minute == other._minute &&
                   _hour == other._hour &&
                   _dayOfMonth == other._dayOfMonth &&
                   _month == other._month &&
                   _dayOfWeek == other._dayOfWeek &&
                   _nthDayOfWeek == other._nthDayOfWeek &&
                   _lastMonthOffset == other._lastMonthOffset &&
                   _flags == other._flags;
        }

        /// <summary>
        /// Determines whether the specified <see cref="System.Object" /> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="System.Object" /> to compare with this instance.</param>
        /// <returns>
        /// <c>true</c> if the specified <see cref="System.Object" /> is equal to this instance;
        /// otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object? obj) => Equals(obj as CronExpression);

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data
        /// structures like a hash table. 
        /// </returns>
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = _second.GetHashCode();
                hashCode = (hashCode * 397) ^ _minute.GetHashCode();
                hashCode = (hashCode * 397) ^ _hour.GetHashCode();
                hashCode = (hashCode * 397) ^ _dayOfMonth.GetHashCode();
                hashCode = (hashCode * 397) ^ _month.GetHashCode();
                hashCode = (hashCode * 397) ^ _dayOfWeek.GetHashCode();
                hashCode = (hashCode * 397) ^ _nthDayOfWeek.GetHashCode();
                hashCode = (hashCode * 397) ^ _lastMonthOffset.GetHashCode();
                hashCode = (hashCode * 397) ^ (int)_flags;

                return hashCode;
            }
        }

        /// <summary>
        /// Implements the operator ==.
        /// </summary>
        public static bool operator ==(CronExpression? left, CronExpression? right) => Equals(left, right);

        /// <summary>
        /// Implements the operator !=.
        /// </summary>
        public static bool operator !=(CronExpression? left, CronExpression? right) => !Equals(left, right);

        private DateTimeOffset? GetOccurrenceConsideringTimeZone(DateTimeOffset fromUtc, TimeZoneInfo zone, bool inclusive)
        {
            if (!DateTimeHelper.IsRound(fromUtc))
            {
                // Rarely, if fromUtc is very close to DST transition, `TimeZoneInfo.ConvertTime` may not convert it correctly on Windows.
                // E.g., In Jordan Time DST started 2017-03-31 00:00 local time. Clocks jump forward from `2017-03-31 00:00 +02:00` to `2017-03-31 01:00 +3:00`.
                // But `2017-03-30 23:59:59.9999000 +02:00` will be converted to `2017-03-31 00:59:59.9999000 +03:00` instead of `2017-03-30 23:59:59.9999000 +02:00` on Windows.
                // It can lead to skipped occurrences. To avoid such errors we floor fromUtc to seconds:
                // `2017-03-30 23:59:59.9999000 +02:00` will be floored to `2017-03-30 23:59:59.0000000 +02:00` and will be converted to `2017-03-30 23:59:59.0000000 +02:00`.
                fromUtc = DateTimeHelper.FloorToSeconds(fromUtc);
                inclusive = false;
            }

            var from = TimeZoneInfo.ConvertTime(fromUtc, zone);

            var fromLocal = from.DateTime;

            if (TimeZoneHelper.IsAmbiguousTime(zone, fromLocal))
            {
                var currentOffset = from.Offset;
                var standardOffset = zone.GetUtcOffset(fromLocal);
               
                if (standardOffset != currentOffset)
                {
                    var daylightOffset = TimeZoneHelper.GetDaylightOffset(zone, fromLocal);
                    var daylightTimeLocalEnd = TimeZoneHelper.GetDaylightTimeEnd(zone, fromLocal, daylightOffset).DateTime;

                    // Early period, try to find anything here.
                    var foundInDaylightOffset = FindOccurrence(fromLocal.Ticks, daylightTimeLocalEnd.Ticks, inclusive);
                    if (foundInDaylightOffset != NotFound) return new DateTimeOffset(foundInDaylightOffset, daylightOffset);

                    fromLocal = TimeZoneHelper.GetStandardTimeStart(zone, fromLocal, daylightOffset).DateTime;
                    inclusive = true;
                }

                // Skip late ambiguous interval.
                var ambiguousIntervalLocalEnd = TimeZoneHelper.GetAmbiguousIntervalEnd(zone, fromLocal).DateTime;

                if (HasFlag(CronExpressionFlag.Interval))
                {
                    var foundInStandardOffset = FindOccurrence(fromLocal.Ticks, ambiguousIntervalLocalEnd.Ticks - 1, inclusive);
                    if (foundInStandardOffset != NotFound) return new DateTimeOffset(foundInStandardOffset, standardOffset);
                }

                fromLocal = ambiguousIntervalLocalEnd;
                inclusive = true;
            }

            var occurrenceTicks = FindOccurrence(fromLocal.Ticks, inclusive);
            if (occurrenceTicks == NotFound) return null;

            var occurrence = new DateTime(occurrenceTicks, DateTimeKind.Unspecified);

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

        private DateTimeOffset? GetPreviousOccurrenceConsideringTimeZone(DateTimeOffset fromUtc, TimeZoneInfo zone, bool inclusive)
        {
            if (!DateTimeHelper.IsRound(fromUtc))
            {
                fromUtc = DateTimeHelper.FloorToSeconds(fromUtc);
            }

            var from = TimeZoneInfo.ConvertTime(fromUtc, zone);
            var fromLocal = from.DateTime;

            if (TimeZoneHelper.IsAmbiguousTime(zone, fromLocal))
            {
                var currentOffset = from.Offset;
                var standardOffset = zone.GetUtcOffset(fromLocal);
                var daylightOffset = TimeZoneHelper.GetDaylightOffset(zone, fromLocal);
                var ambiguousIntervalStart = TimeZoneHelper.GetStandardTimeStart(zone, fromLocal, daylightOffset).DateTime;

                if (standardOffset == currentOffset)
                {
                    if (HasFlag(CronExpressionFlag.Interval))
                    {
                        var foundInStandardOffset = FindPreviousOccurrence(fromLocal.Ticks, ambiguousIntervalStart.Ticks, inclusive);
                        if (foundInStandardOffset != NotFound) return new DateTimeOffset(foundInStandardOffset, standardOffset);
                    }

                    var daylightTimeLocalEnd = TimeZoneHelper.GetDaylightTimeEnd(zone, fromLocal, daylightOffset).DateTime;
                    var foundInDaylightOffset = FindPreviousOccurrence(daylightTimeLocalEnd.Ticks, ambiguousIntervalStart.Ticks, true);
                    if (foundInDaylightOffset != NotFound) return new DateTimeOffset(foundInDaylightOffset, daylightOffset);

                    fromLocal = ambiguousIntervalStart.AddTicks(-1);
                    inclusive = true;
                }
                else
                {
                    var foundInDaylightOffset = FindPreviousOccurrence(fromLocal.Ticks, ambiguousIntervalStart.Ticks, inclusive);
                    if (foundInDaylightOffset != NotFound) return new DateTimeOffset(foundInDaylightOffset, daylightOffset);

                    fromLocal = ambiguousIntervalStart.AddTicks(-1);
                    inclusive = true;
                }
            }

            var occurrenceTicks = FindPreviousOccurrence(fromLocal.Ticks, inclusive);
            if (occurrenceTicks == NotFound) return null;

            var occurrence = new DateTime(occurrenceTicks, DateTimeKind.Unspecified);

            if (zone.IsInvalidTime(occurrence))
            {
                return TimeZoneHelper.GetStandardTimeEnd(zone, occurrence);
            }

            if (TimeZoneHelper.IsAmbiguousTime(zone, occurrence))
            {
                var daylightOffset = TimeZoneHelper.GetDaylightOffset(zone, occurrence);
                if (HasFlag(CronExpressionFlag.Interval))
                {
                    return new DateTimeOffset(occurrence, zone.GetUtcOffset(occurrence));
                }

                return new DateTimeOffset(occurrence, daylightOffset);
            }

            return new DateTimeOffset(occurrence, zone.GetUtcOffset(occurrence));
        }

        private long FindOccurrence(long startTimeTicks, long endTimeTicks, bool startInclusive)
        {
            var found = FindOccurrence(startTimeTicks, startInclusive);

            if (found == NotFound || found > endTimeTicks) return NotFound;
            return found;
        }

        private long FindPreviousOccurrence(long startTimeTicks, long endTimeTicks, bool startInclusive)
        {
            var found = FindPreviousOccurrence(startTimeTicks, startInclusive);

            if (found == NotFound || found < endTimeTicks) return NotFound;
            return found;
        }

        private long FindOccurrence(long ticks, bool startInclusive)
        {
            if (!startInclusive) ticks++;

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

            var lastCheckedDay = day;

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

            if (!Move(_month, ref month))
            {
                year++;
                if (year > DateTime.MaxValue.Year)
                {
                    return NotFound;
                }
            }
            
            day = minMatchedDay;

            goto Retry;
        }

        private long FindPreviousOccurrence(long ticks, bool startInclusive)
        {
            if (!startInclusive)
            {
                if (ticks == DateTime.MinValue.Ticks) return NotFound;
                ticks--;
            }

            CalendarHelper.FillDateTimeParts(
                ticks,
                out int startSecond,
                out int startMinute,
                out int startHour,
                out int startDay,
                out int startMonth,
                out int startYear);

            if (ticks % TimeSpan.TicksPerSecond != 0) startSecond--;

            var maxMatchedDay = HasFlag(CronExpressionFlag.DayOfMonthLast) || HasFlag(CronExpressionFlag.NearestWeekday)
                ? CronField.DaysOfMonth.Last
                : GetLastSet(_dayOfMonth);

            var second = startSecond;
            var minute = startMinute;
            var hour = startHour;
            var day = startDay;
            var month = startMonth;
            var year = startYear;

            if (!GetBit(_second, second) && !MoveBack(_second, ref second)) minute--;
            if (minute < CronField.Minutes.First || !GetBit(_minute, minute) && !MoveBack(_minute, ref minute)) hour--;
            if (hour < CronField.Hours.First || !GetBit(_hour, hour) && !MoveBack(_hour, ref hour)) day--;

            if (!GetBit(_month, month)) goto RetryMonth;

            Retry:

            if (!TryGetPreviousDay(year, month, day, out var lastCheckedDay, out var actualDay)) goto RetryMonth;

            if (CalendarHelper.IsGreaterThan(startYear, startMonth, startDay, year, month, actualDay)) goto RolloverDay;
            if (hour < startHour) goto RolloverHour;
            if (minute < startMinute) goto RolloverMinute;
            goto ReturnResult;

            RolloverDay: hour = GetLastSet(_hour);
            RolloverHour: minute = GetLastSet(_minute);
            RolloverMinute: second = GetLastSet(_second);

            ReturnResult:

            var found = CalendarHelper.DateTimeToTicks(year, month, actualDay, hour, minute, second);
            if (found <= ticks) return found;

            day = lastCheckedDay;
            if (MoveBackDay(ref day)) goto Retry;

            RetryMonth:

            if (!MoveBack(_month, ref month))
            {
                year--;
                if (year < DateTime.MinValue.Year)
                {
                    return NotFound;
                }
            }

            day = maxMatchedDay;

            goto Retry;
        }

        private static bool Move(ulong fieldBits, ref int fieldValue)
        {
            if (fieldBits >> ++fieldValue == 0)
            {
                fieldValue = GetFirstSet(fieldBits);
                return false;
            }

            fieldValue += GetFirstSet(fieldBits >> fieldValue);
            return true;
        }

        private static bool MoveBack(ulong fieldBits, ref int fieldValue)
        {
            var eligibleBits = GetBitsAtOrBelow(fieldBits, fieldValue - 1);
            if (eligibleBits == 0)
            {
                fieldValue = GetLastSet(fieldBits);
                return false;
            }

            fieldValue = GetLastSet(eligibleBits);
            return true;
        }

        private bool TryGetPreviousDay(int year, int month, int dayLimit, out int day, out int actualDay)
        {
            var lastDayOfMonth = CalendarHelper.GetDaysInMonth(year, month);
            var maxDay = dayLimit > lastDayOfMonth ? lastDayOfMonth : dayLimit;

            if (HasFlag(CronExpressionFlag.NearestWeekday))
            {
                day = HasFlag(CronExpressionFlag.DayOfMonthLast)
                    ? GetLastDayOfMonth(year, month)
                    : GetFirstSet(_dayOfMonth);

                actualDay = CalendarHelper.MoveToNearestWeekDay(year, month, day);
                return actualDay <= maxDay && IsDayOfWeekMatch(year, month, actualDay);
            }

            if (HasFlag(CronExpressionFlag.DayOfMonthLast))
            {
                day = GetLastDayOfMonth(year, month);
                actualDay = day;
                return actualDay <= maxDay && IsDayOfWeekMatch(year, month, actualDay);
            }

            if (_dayOfMonth == CronField.DaysOfMonth.AllBits &&
                !HasFlag(CronExpressionFlag.DayOfWeekLast) &&
                !HasFlag(CronExpressionFlag.NthDayOfWeek))
            {
                day = maxDay;
                actualDay = day;

                if (_dayOfWeek == CronField.DaysOfWeek.AllBits)
                {
                    return true;
                }

                var dayOfWeek = (int)CalendarHelper.GetDayOfWeek(year, month, day);

                for (var delta = 0; delta < 7 && day - delta >= CronField.DaysOfMonth.First; delta++)
                {
                    var candidateDayOfWeek = dayOfWeek - delta;
                    if (candidateDayOfWeek < 0) candidateDayOfWeek += 7;

                    if (((_dayOfWeek >> candidateDayOfWeek) & 1) == 0) continue;

                    day -= delta;
                    actualDay = day;
                    return true;
                }

                actualDay = default;
                return false;
            }

            day = maxDay;

            if (!GetBit(_dayOfMonth, day) && !MoveBackDay(ref day))
            {
                actualDay = default;
                return false;
            }

            Retry:

            if (day > lastDayOfMonth)
            {
                if (MoveBackDay(ref day)) goto Retry;

                actualDay = default;
                return false;
            }

            actualDay = day;
            if (IsDayOfWeekMatch(year, month, actualDay))
            {
                return true;
            }

            if (MoveBackDay(ref day))
            {
                goto Retry;
            }

            actualDay = default;
            return false;
        }

        private bool MoveBackDay(ref int day)
        {
            if (HasFlag(CronExpressionFlag.NearestWeekday) || HasFlag(CronExpressionFlag.DayOfMonthLast))
            {
                return false;
            }

            if (_dayOfMonth == CronField.DaysOfMonth.AllBits)
            {
                if (day <= CronField.DaysOfMonth.First)
                {
                    day = CronField.DaysOfMonth.Last;
                    return false;
                }

                day--;
                return true;
            }

            return MoveBack(_dayOfMonth, ref day);
        }

        private bool IsDayMatch(int year, int month, int day)
        {
            bool dayOfMonthMatch;

            if (HasFlag(CronExpressionFlag.NearestWeekday))
            {
                dayOfMonthMatch = false;
                var lastDay = CalendarHelper.GetDaysInMonth(year, month);

                if (HasFlag(CronExpressionFlag.DayOfMonthLast))
                {
                    var targetDay = GetLastDayOfMonth(year, month);
                    dayOfMonthMatch = CalendarHelper.MoveToNearestWeekDay(year, month, targetDay) == day;
                }
                else
                {
                    for (var targetDay = CronField.DaysOfMonth.First; targetDay <= lastDay; targetDay++)
                    {
                        if (!GetBit(_dayOfMonth, targetDay)) continue;
                        if (CalendarHelper.MoveToNearestWeekDay(year, month, targetDay) != day) continue;

                        dayOfMonthMatch = true;
                        break;
                    }
                }
            }
            else if (HasFlag(CronExpressionFlag.DayOfMonthLast))
            {
                dayOfMonthMatch = GetLastDayOfMonth(year, month) == day;
            }
            else
            {
                dayOfMonthMatch = GetBit(_dayOfMonth, day);
            }

            return dayOfMonthMatch && IsDayOfWeekMatch(year, month, day);
        }

        private int GetLastDayOfMonth(int year, int month)
        {
            return CalendarHelper.GetDaysInMonth(year, month) - _lastMonthOffset;
        }

        private bool IsDayOfWeekMatch(int year, int month, int day)
        {
            if (HasFlag(CronExpressionFlag.DayOfWeekLast) && !CalendarHelper.IsLastDayOfWeek(year, month, day) ||
                HasFlag(CronExpressionFlag.NthDayOfWeek) && !CalendarHelper.IsNthDayOfWeek(day, _nthDayOfWeek))
            {
                return false;
            }

            if (_dayOfWeek == CronField.DaysOfWeek.AllBits) return true;

            var dayOfWeek = CalendarHelper.GetDayOfWeek(year, month, day);

            return ((_dayOfWeek >> (int)dayOfWeek) & 1) != 0;
        }

        private static int GetFirstSet(ulong value)
        {
            // TODO: Add description and source
            ulong res = unchecked((ulong)((long)value & -(long)value) * 0x022fdd63cc95386d) >> 58;
            return DeBruijnPositions[res];
        }

        private static int GetLastSet(ulong value)
        {
            var index = 0;

            while ((value >>= 1) != 0)
            {
                index++;
            }

            return index;
        }

        private static int GetLastSetWithin(ulong fieldBits, int maxValue)
        {
            return GetLastSet(GetBitsAtOrBelow(fieldBits, maxValue));
        }

        private static ulong GetBitsAtOrBelow(ulong fieldBits, int maxValue)
        {
            if (maxValue < 0) return 0;
            if (maxValue >= 63) return fieldBits;

            return fieldBits & ((1UL << (maxValue + 1)) - 1);
        }

        private bool HasFlag(CronExpressionFlag value)
        {
            return (_flags & value) != 0;
        }

        private static StringBuilder AppendFieldValue(StringBuilder expressionBuilder, CronField field, ulong fieldValue)
        {
            if (field.AllBits == fieldValue) return expressionBuilder.Append('*');

            // Unset 7 bit for Day of week field because both 0 and 7 stand for Sunday.
            if (field == CronField.DaysOfWeek) fieldValue &= ~(1U << field.Last);

            for (var i = GetFirstSet(fieldValue);; i = GetFirstSet(fieldValue >> i << i))
            {
                expressionBuilder.Append(i);
                if (fieldValue >> ++i == 0) break;
                expressionBuilder.Append(',');
            }

            return expressionBuilder;
        }

        private StringBuilder AppendDayOfMonth(StringBuilder expressionBuilder, uint domValue)
        {
            if (HasFlag(CronExpressionFlag.DayOfMonthLast))
            {
                expressionBuilder.Append('L');
                if (_lastMonthOffset != 0) expressionBuilder.Append(String.Format(CultureInfo.InvariantCulture, "-{0}", _lastMonthOffset));
            }
            else
            {
                AppendFieldValue(expressionBuilder, CronField.DaysOfMonth, (uint)domValue);
            }

            if (HasFlag(CronExpressionFlag.NearestWeekday)) expressionBuilder.Append('W');

            return expressionBuilder;
        }

        private void AppendDayOfWeek(StringBuilder expressionBuilder, uint dowValue)
        {
            AppendFieldValue(expressionBuilder, CronField.DaysOfWeek, dowValue);

            if (HasFlag(CronExpressionFlag.DayOfWeekLast)) expressionBuilder.Append('L');
            else if (HasFlag(CronExpressionFlag.NthDayOfWeek)) expressionBuilder.Append(String.Format(CultureInfo.InvariantCulture, "#{0}", _nthDayOfWeek));
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        private static void ThrowFromShouldBeLessThanToException(string fromName, string toName)
        {
            throw new ArgumentException($"The value of the {fromName} argument should be less than the value of the {toName} argument.", fromName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        private static void ThrowFromShouldBeGreaterThanToException(string fromName, string toName)
        {
            throw new ArgumentException($"The value of the {fromName} argument should be greater than or equal to the value of the {toName} argument.", fromName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        private static void ThrowWrongDateTimeKindException(string paramName)
        {
            throw new ArgumentException("The supplied DateTime must have the Kind property set to Utc", paramName);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        [DoesNotReturn]
        private static void ThrowArgumentNullException(string paramName)
        {
            throw new ArgumentNullException(paramName);
        }

        private static bool GetBit(ulong value, int index)
        {
            return (value & (1UL << index)) != 0;
        }
    }
}

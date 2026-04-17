using System;
using System.Globalization;
using System.Linq;
using Xunit;

namespace Cronos.Tests
{
    public class CronExpressionReverseFacts
    {
        private static readonly bool IsUnix =
#if NETCOREAPP1_1
            !System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
#else
            Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix;
#endif

        private static readonly string EasternTimeZoneId = IsUnix ? "America/New_York" : "Eastern Standard Time";
        private static readonly string JordanTimeZoneId = IsUnix ? "Asia/Amman" : "Jordan Standard Time";
        private static readonly string LordHoweTimeZoneId = IsUnix ? "Australia/Lord_Howe" : "Lord Howe Standard Time";
        private static readonly string PacificTimeZoneId = IsUnix ? "America/Santiago" : "Pacific SA Standard Time";

        private static readonly TimeZoneInfo EasternTimeZone = TimeZoneInfo.FindSystemTimeZoneById(EasternTimeZoneId);
        private static readonly TimeZoneInfo JordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById(JordanTimeZoneId);
        private static readonly TimeZoneInfo LordHoweTimeZone = TimeZoneInfo.FindSystemTimeZoneById(LordHoweTimeZoneId);
        private static readonly TimeZoneInfo PacificTimeZone = TimeZoneInfo.FindSystemTimeZoneById(PacificTimeZoneId);

        [Theory]
        [InlineData(DateTimeKind.Unspecified, false)]
        [InlineData(DateTimeKind.Unspecified, true)]
        [InlineData(DateTimeKind.Local, false)]
        [InlineData(DateTimeKind.Local, true)]
        public void GetPreviousOccurrence_ThrowsAnException_WhenFromDoesNotHaveUtcKind(DateTimeKind kind, bool inclusive)
        {
            var from = new DateTime(2017, 03, 15, 0, 0, 0, kind);

            var exception = Assert.Throws<ArgumentException>(() => CronExpression.EveryMinute.GetPreviousOccurrence(from, inclusive));

            Assert.Equal("fromUtc", exception.ParamName);
        }

        [Theory]
        [InlineData(DateTimeKind.Unspecified, false)]
        [InlineData(DateTimeKind.Unspecified, true)]
        [InlineData(DateTimeKind.Local, false)]
        [InlineData(DateTimeKind.Local, true)]
        public void GetPreviousOccurrence_DateTimeTimeZone_ThrowsAnException_WhenFromHasAWrongKind(DateTimeKind kind, bool inclusive)
        {
            var from = new DateTime(2017, 03, 22, 0, 0, 0, kind);

            var exception = Assert.Throws<ArgumentException>(() => CronExpression.EveryMinute.GetPreviousOccurrence(from, TimeZoneInfo.Local, inclusive));

            Assert.Equal("fromUtc", exception.ParamName);
        }

        [Fact]
        public void GetPreviousOccurrence_DateTimeTimeZone_ThrowsAnException_WhenZoneIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => CronExpression.EveryMinute.GetPreviousOccurrence(DateTime.UtcNow, null!));

            Assert.Equal("zone", exception.ParamName);
        }

        [Fact]
        public void GetPreviousOccurrence_DateTimeOffsetTimeZone_ThrowsAnException_WhenZoneIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => CronExpression.EveryMinute.GetPreviousOccurrence(DateTimeOffset.UtcNow, null!));

            Assert.Equal("zone", exception.ParamName);
        }

        [Theory]
        [InlineData(false, "2017-03-22 09:31:00")]
        [InlineData(true, "2017-03-22 09:32:00")]
        public void GetPreviousOccurrence_ReturnsCorrectUtcDate(bool inclusive, string expectedString)
        {
            var from = new DateTime(2017, 03, 22, 9, 32, 0, DateTimeKind.Utc);
            var expected = GetUtcDateTime(expectedString);

            var occurrence = CronExpression.EveryMinute.GetPreviousOccurrence(from, inclusive);

            Assert.Equal(expected, occurrence);
            Assert.Equal(DateTimeKind.Utc, occurrence?.Kind);
        }

        [Fact]
        public void GetPreviousOccurrence_ReturnsCorrectDate_WhenSecondsAreIncluded()
        {
            var expression = CronExpression.Parse("20 * * * * *", CronFormat.IncludeSeconds);
            var from = GetUtcDateTime("2017-03-22 17:35:40");

            var previous = expression.GetPreviousOccurrence(from);

            Assert.Equal(GetUtcDateTime("2017-03-22 17:35:20"), previous);
        }

        [Fact]
        public void GetPreviousOccurrence_ReturnsCorrectDate_ForComplexUtcExpression()
        {
            var expression = CronExpression.Parse("*/10 12-20 * DEC 3");
            var from = GetUtcDateTime("2017-12-06 12:00:00");

            var previous = expression.GetPreviousOccurrence(from, inclusive: true);

            Assert.Equal(GetUtcDateTime("2017-12-06 12:00:00"), previous);
        }

        [Theory]
        [InlineData("10-30 * * * * *", "2017-03-22 17:35:31", "2017-03-22 17:35:30", CronFormat.IncludeSeconds)]
        [InlineData("0 0 20-5/5 * *", "2017-06-05 00:00:00", "2017-06-04 00:00:00", CronFormat.Standard)]
        [InlineData("0 0 * 12-2 *", "2017-03-01 00:00:00", "2017-02-28 00:00:00", CronFormat.Standard)]
        [InlineData("0 0 * * thu-sat", "2016-12-12 00:00:00", "2016-12-10 00:00:00", CronFormat.Standard)]
        [InlineData("0 5 18 13 * 5", "2017-10-13 18:05:00", "2017-01-13 18:05:00", CronFormat.IncludeSeconds)]
        [InlineData("0 0 L-1 * *", "2016-12-30 00:00:00", "2016-11-29 00:00:00", CronFormat.Standard)]
        [InlineData("0 0 L-2W * *", "2017-03-01 00:00:00", "2017-02-27 00:00:00", CronFormat.Standard)]
        [InlineData("0 0 * * SUN#1", "2017-02-05 00:00:00", "2017-01-01 00:00:00", CronFormat.Standard)]
        public void GetPreviousOccurrence_ReturnsCorrectDate_ForBroaderUtcMatrix(string cronExpression, string fromString, string expectedString, CronFormat format)
        {
            var expression = CronExpression.Parse(cronExpression, format);
            var from = GetUtcDateTime(fromString);

            var previous = expression.GetPreviousOccurrence(from);

            Assert.Equal(GetUtcDateTime(expectedString), previous);
        }

        [Fact]
        public void GetPreviousOccurrence_ReturnsCorrectDate_WhenMacroExpressionHasJitterSeed()
        {
            var expression = CronExpression.Parse("@daily", 12345);
            var from = GetLocalInstant("2017-03-22 12:00:00", EasternTimeZone);
            var startOfDay = GetLocalInstant("2017-03-22 00:00:00", EasternTimeZone);

            var previous = expression.GetPreviousOccurrence(from, EasternTimeZone);
            var next = expression.GetNextOccurrence(previous!.Value, EasternTimeZone);
            var nextFromStartOfDay = expression.GetNextOccurrence(startOfDay, EasternTimeZone, inclusive: true);

            Assert.True(previous < from);
            Assert.Equal(nextFromStartOfDay, next);
            Assert.True(next > from);
        }

        [Fact]
        public void GetPreviousOccurrence_ReturnsCorrectDate_WhenExpressionContainsHash()
        {
            var expression = CronExpression.Parse("H * * * *", 3);
            var from = GetLocalInstant("2017-03-23 17:18:00", EasternTimeZone);

            var previous = expression.GetPreviousOccurrence(from, EasternTimeZone);
            var next = expression.GetNextOccurrence(previous!.Value, EasternTimeZone);

            Assert.Equal(GetLocalInstant("2017-03-23 17:17:00", EasternTimeZone), previous);
            Assert.Equal(GetLocalInstant("2017-03-23 18:17:00", EasternTimeZone), next);
            Assert.True(previous < from);
            Assert.True(next > from);
        }

        [Fact]
        public void GetPreviousOccurrence_ReturnsNull_WhenCronExpressionIsUnreachable()
        {
            var expression = CronExpression.Parse("* * 31 2 *");
            var from = GetUtcDateTime("2017-03-22 00:00:00");

            var previous = expression.GetPreviousOccurrence(from);

            Assert.Null(previous);
        }

        [Fact]
        public void GetPreviousOccurrence_RollsBackAcrossMonthBoundary()
        {
            var expression = CronExpression.Parse("0 0 1 * *");
            var from = GetUtcDateTime("2017-03-01 00:00:00");

            var previous = expression.GetPreviousOccurrence(from);

            Assert.Equal(GetUtcDateTime("2017-02-01 00:00:00"), previous);
        }

        [Theory]
        [InlineData("0 0 L * *", "2017-03-31 00:00:00", "2017-02-28 00:00:00")]
        [InlineData("0 0 1W * *", "2017-03-03 00:00:00", "2017-03-01 00:00:00")]
        [InlineData("0 0 LW * *", "2017-04-01 00:00:00", "2017-03-31 00:00:00")]
        [InlineData("0 0 * * 6#3", "2017-03-19 00:00:00", "2017-03-18 00:00:00")]
        public void GetPreviousOccurrence_ReturnsCorrectDate_ForSpecialDayModifiers(string cronExpression, string fromString, string expectedString)
        {
            var expression = CronExpression.Parse(cronExpression);
            var from = GetUtcDateTime(fromString);

            var previous = expression.GetPreviousOccurrence(from);

            Assert.Equal(GetUtcDateTime(expectedString), previous);
        }

        [Theory]
        [InlineData("0 0 L-1W * *", "2017-03-01 00:00:00", "2017-02-27 00:00:00")]
        [InlineData("0 0 ? * sat-tue", "2016-12-14 00:00:00", "2016-12-13 00:00:00")]
        [InlineData("0 0 13 * 5", "2017-10-13 00:00:00", "2017-01-13 00:00:00")]
        public void GetPreviousOccurrence_ReturnsCorrectDate_ForAdditionalReverseCases(string cronExpression, string fromString, string expectedString)
        {
            var expression = CronExpression.Parse(cronExpression);
            var from = GetUtcDateTime(fromString);

            var previous = expression.GetPreviousOccurrence(from);

            Assert.Equal(GetUtcDateTime(expectedString), previous);
        }

        [Fact]
        public void GetOccurrencesDescending_ReturnsExpectedCollection()
        {
            var expression = CronExpression.Parse("* * * * *");
            var from = GetUtcDateTime("2017-03-22 00:02:00");
            var to = GetUtcDateTime("2017-03-22 00:00:00");

            var occurrences = expression
                .GetOccurrencesDescending(from, to, fromInclusive: true, toInclusive: true)
                .ToArray();

            Assert.Equal(
                new[]
                {
                    GetUtcDateTime("2017-03-22 00:02:00"),
                    GetUtcDateTime("2017-03-22 00:01:00"),
                    GetUtcDateTime("2017-03-22 00:00:00")
                },
                occurrences);
        }

        [Fact]
        public void GetOccurrencesDescending_ReturnsReverseOfForwardCollection()
        {
            var expression = CronExpression.Parse("*/15 * * * *");
            var from = GetUtcDateTime("2017-03-22 01:00:00");
            var to = GetUtcDateTime("2017-03-22 00:00:00");

            var descending = expression.GetOccurrencesDescending(from, to, fromInclusive: true, toInclusive: true).ToArray();
            var ascending = expression.GetOccurrences(to, from, fromInclusive: true, toInclusive: true).Reverse().ToArray();

            Assert.Equal(ascending, descending);
        }

        [Fact]
        public void GetOccurrencesDescending_ReturnsReverseOfForwardCollection_WhenZoneIsSpecified()
        {
            var expression = CronExpression.Parse("0 */30 * * * *", CronFormat.IncludeSeconds);
            var from = GetUtcDateTime("2016-11-06 07:00:00");
            var to = GetUtcDateTime("2016-11-06 04:00:00");

            var descending = expression.GetOccurrencesDescending(from, to, EasternTimeZone, fromInclusive: true, toInclusive: true).ToArray();
            var ascending = expression.GetOccurrences(to, from, EasternTimeZone, fromInclusive: true, toInclusive: true).Reverse().ToArray();

            Assert.Equal(ascending, descending);
        }

        [Fact]
        public void GetPreviousOccurrence_CanStepAcrossJordanDstAdjustedMonthlySequence()
        {
            var expression = CronExpression.Parse("30 0 L * *");
            var from = GetInstant("2017-04-30 00:30:00 +03:00");

            var previous = expression.GetPreviousOccurrence(from, JordanTimeZone, inclusive: false);
            var beforePrevious = expression.GetPreviousOccurrence(previous!.Value, JordanTimeZone, inclusive: false);

            Assert.Equal(GetInstant("2017-03-31 00:30:00 +03:00"), previous);
            Assert.Equal(GetInstant("2017-02-28 00:30:00 +02:00"), beforePrevious);
        }

        [Fact]
        public void GetPreviousOccurrence_CanStepAcrossLordHoweRepeatedHourIntervalSequence()
        {
            var expression = CronExpression.Parse("0 */30 1 * * *", CronFormat.IncludeSeconds);
            var from = GetInstant("2017-04-02 01:30:00 +10:30");

            var previous = expression.GetPreviousOccurrence(from, LordHoweTimeZone, inclusive: false);
            var beforePrevious = expression.GetPreviousOccurrence(previous!.Value, LordHoweTimeZone, inclusive: false);

            Assert.Equal(GetInstant("2017-04-02 01:30:00 +11:00"), previous);
            Assert.Equal(GetInstant("2017-04-02 01:00:00 +11:00"), beforePrevious);
        }

        [Fact]
        public void GetOccurrencesDescending_DateTime_ThrowsAnException_WhenFromLessThanTo()
        {
            var expression = CronExpression.Parse("* * * * *");

            var exception = Assert.Throws<ArgumentException>(
                () => expression.GetOccurrencesDescending(DateTime.UtcNow, DateTime.UtcNow.AddMinutes(1)).ToArray());

            Assert.Equal("fromUtc", exception.ParamName);
        }

        [Fact]
        public void GetPreviousOccurrence_UsesSingleOccurrenceForAmbiguousNonIntervalTime()
        {
            var expression = CronExpression.Parse("30 1 * * *");
            var from = GetInstant("2017-11-05 03:00:00 -05:00");

            var previous = expression.GetPreviousOccurrence(from, EasternTimeZone);

            Assert.Equal(GetInstant("2017-11-05 01:30:00 -04:00"), previous);
        }

        [Fact]
        public void GetPreviousOccurrence_PreservesAmbiguousIntervalOccurrencesInReverseOrder()
        {
            var expression = CronExpression.Parse("*/30 1 5 11 *");
            var from = GetInstant("2017-11-05 02:00:00 -05:00");

            var first = expression.GetPreviousOccurrence(from, EasternTimeZone);
            var second = expression.GetPreviousOccurrence(first!.Value, EasternTimeZone);
            var third = expression.GetPreviousOccurrence(second!.Value, EasternTimeZone);

            Assert.Equal(GetInstant("2017-11-05 01:30:00 -05:00"), first);
            Assert.Equal(GetInstant("2017-11-05 01:00:00 -05:00"), second);
            Assert.Equal(GetInstant("2017-11-05 01:30:00 -04:00"), third);
        }

        [Fact]
        public void GetPreviousOccurrence_AdjustsInvalidTimeBackwardAcrossSpringForward()
        {
            var expression = CronExpression.Parse("30 2 * * *");
            var from = GetInstant("2017-03-12 04:00:00 -04:00");

            var previous = expression.GetPreviousOccurrence(from, EasternTimeZone);

            Assert.Equal(GetInstant("2017-03-12 01:59:59 -05:00"), previous);
        }

        [Theory]
        [InlineData("30 0 L * *", "2017-04-30 00:30 +03:00", "2017-03-31 00:30 +03:00")]
        [InlineData("30 0 LW * *", "2018-04-30 00:30 +03:00", "2018-03-30 00:30 +03:00")]
        public void GetPreviousOccurrence_HandleJordanForwardShiftCases(string cronExpression, string fromString, string expectedString)
        {
            var expression = CronExpression.Parse(cronExpression);
            var from = GetInstant(fromString);

            var previous = expression.GetPreviousOccurrence(from, JordanTimeZone);

            Assert.Equal(GetInstant(expectedString), previous);
        }

        [Fact]
        public void GetPreviousOccurrence_HandleLordHoweBackwardShift_ForNonIntervalExpression()
        {
            var expression = CronExpression.Parse("0 30 1 * * *", CronFormat.IncludeSeconds);
            var from = GetInstant("2017-04-03 01:30:00 +10:30");

            var previous = expression.GetPreviousOccurrence(from, LordHoweTimeZone);

            Assert.Equal(GetInstant("2017-04-02 01:30:00 +11:00"), previous);
        }

        [Fact]
        public void GetPreviousOccurrence_HandlePacificBackwardShift_AroundRepeatedHour()
        {
            var expression = CronExpression.Parse("30 23 * * *");
            var from = GetInstant("2017-05-14 23:30:00 -04:00");

            var previous = expression.GetPreviousOccurrence(from, PacificTimeZone);

            Assert.Equal(GetInstant("2017-05-13 23:30:00 -03:00"), previous);
        }

        [Fact]
        public void GetPreviousOccurrence_DoesNotReturnValueLaterThanNonRoundInput()
        {
            var expression = CronExpression.Parse("* * * * *");
            var from = GetInstant("2017-03-12 03:00:00.5000000 -04:00");

            var previous = expression.GetPreviousOccurrence(from, EasternTimeZone, inclusive: true);

            Assert.True(previous <= from);
        }

        [Fact]
        public void GetPreviousOccurrence_FromDateTimeMinValueInclusive_SuccessfullyReturned()
        {
            var from = new DateTime(0, DateTimeKind.Utc);

            var previous = CronExpression.Parse("* * * * *")
                .GetPreviousOccurrence(from, inclusive: true);

            Assert.Equal(from, previous);
        }

        private static DateTime GetUtcDateTime(string dateTimeString)
        {
            var dateTime = DateTime.ParseExact(
                dateTimeString,
                new[]
                {
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyy-MM-dd HH:mm"
                },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);

            return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
        }

        private static DateTimeOffset GetLocalInstant(string localDateTimeString, TimeZoneInfo zone)
        {
            var dateTime = DateTime.ParseExact(
                localDateTimeString,
                new[]
                {
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyy-MM-dd HH:mm"
                },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);

            return new DateTimeOffset(dateTime, zone.GetUtcOffset(dateTime));
        }

        private static DateTimeOffset GetInstant(string dateTimeOffsetString)
        {
            return DateTimeOffset.ParseExact(
                dateTimeOffsetString,
                new[]
                {
                    "yyyy-MM-dd HH:mm zzz",
                    "yyyy-MM-dd HH:mm:ss zzz",
                    "yyyy-MM-dd HH:mm:ss.fffffff zzz"
                },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);
        }
    }
}

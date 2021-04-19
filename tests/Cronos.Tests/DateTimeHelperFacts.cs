using System;
using System.Globalization;
using Xunit;

namespace Cronos.Tests
{
    public class DateTimeHelperFacts
    {
        [Theory]

        [InlineData("2017-03-30 23:59:59.0000000 +02:00", "2017-03-30 23:59:59.0000000 +02:00")]

        [InlineData("2017-03-30 23:59:59.9000000 +03:00", "2017-03-30 23:59:59.0000000 +03:00")]
        [InlineData("2017-03-30 23:59:59.9900000 +03:00", "2017-03-30 23:59:59.0000000 +03:00")]
        [InlineData("2017-03-30 23:59:59.9990000 +03:00", "2017-03-30 23:59:59.0000000 +03:00")]
        [InlineData("2017-03-30 23:59:59.9999000 +03:00", "2017-03-30 23:59:59.0000000 +03:00")]
        [InlineData("2017-03-30 23:59:59.9999900 +03:00", "2017-03-30 23:59:59.0000000 +03:00")]
        [InlineData("2017-03-30 23:59:59.9999990 +03:00", "2017-03-30 23:59:59.0000000 +03:00")]
        [InlineData("2017-03-30 23:59:59.9999999 +03:00", "2017-03-30 23:59:59.0000000 +03:00")]

        [InlineData("2017-03-30 23:59:59.0000001 +01:00", "2017-03-30 23:59:59.0000000 +01:00")]
        [InlineData("2017-03-30 23:59:59.0000010 +01:00", "2017-03-30 23:59:59.0000000 +01:00")]
        [InlineData("2017-03-30 23:59:59.0000100 +01:00", "2017-03-30 23:59:59.0000000 +01:00")]
        [InlineData("2017-03-30 23:59:59.0001000 +01:00", "2017-03-30 23:59:59.0000000 +01:00")]
        [InlineData("2017-03-30 23:59:59.0010000 +01:00", "2017-03-30 23:59:59.0000000 +01:00")]
        [InlineData("2017-03-30 23:59:59.0100000 +01:00", "2017-03-30 23:59:59.0000000 +01:00")]
        [InlineData("2017-03-30 23:59:59.1000000 +01:00", "2017-03-30 23:59:59.0000000 +01:00")]
        public void FloorToSeconds_WorksCorrectlyWithDateTimeOffset(string dateTime, string expected)
        {
            var dateTimeOffset = GetDateTimeOffsetInstant(dateTime);
            var expectedDateTimeOffset = GetDateTimeOffsetInstant(expected);

            var flooredDateTimeOffset = DateTimeHelper.FloorToSeconds(dateTimeOffset);

            Assert.Equal(expectedDateTimeOffset, flooredDateTimeOffset);
            Assert.Equal(expectedDateTimeOffset.Offset, flooredDateTimeOffset.Offset);
        }

        [Theory]

        [InlineData("2021-04-19 01:45:45.0000000", "2021-04-19 01:45:45.0000000", DateTimeKind.Unspecified)]

        [InlineData("2021-04-19 01:45:45.9000000", "2021-04-19 01:45:45.0000000", DateTimeKind.Utc)]
        [InlineData("2021-04-19 01:45:45.9900000", "2021-04-19 01:45:45.0000000", DateTimeKind.Utc)]
        [InlineData("2021-04-19 01:45:45.9990000", "2021-04-19 01:45:45.0000000", DateTimeKind.Utc)]
        [InlineData("2021-04-19 01:45:45.9999000", "2021-04-19 01:45:45.0000000", DateTimeKind.Utc)]
        [InlineData("2021-04-19 01:45:45.9999900", "2021-04-19 01:45:45.0000000", DateTimeKind.Utc)]
        [InlineData("2021-04-19 01:45:45.9999990", "2021-04-19 01:45:45.0000000", DateTimeKind.Utc)]
        [InlineData("2021-04-19 01:45:45.9999999", "2021-04-19 01:45:45.0000000", DateTimeKind.Utc)]

        [InlineData("2021-04-19 01:45:45.0000001", "2021-04-19 01:45:45.0000000", DateTimeKind.Unspecified)]
        [InlineData("2021-04-19 01:45:45.0000010", "2021-04-19 01:45:45.0000000", DateTimeKind.Unspecified)]
        [InlineData("2021-04-19 01:45:45.0000100", "2021-04-19 01:45:45.0000000", DateTimeKind.Unspecified)]
        [InlineData("2021-04-19 01:45:45.0001000", "2021-04-19 01:45:45.0000000", DateTimeKind.Unspecified)]
        [InlineData("2021-04-19 01:45:45.0010000", "2021-04-19 01:45:45.0000000", DateTimeKind.Unspecified)]
        [InlineData("2021-04-19 01:45:45.0100000", "2021-04-19 01:45:45.0000000", DateTimeKind.Unspecified)]
        [InlineData("2021-04-19 01:45:45.1000000", "2021-04-19 01:45:45.0000000", DateTimeKind.Unspecified)]
        public void FloorToSeconds_WorksCorrectlyWithDateTimeUtc(string dateTime, string expected, DateTimeKind kind)
        {
            var dateTimeInstant = GetDateTimeInstant(dateTime, kind);
            var expectedDateTimeInstant = GetDateTimeInstant(expected, kind);

            var flooredDateTime = DateTimeHelper.FloorToSeconds(dateTimeInstant);

            Assert.Equal(expectedDateTimeInstant, flooredDateTime);
            Assert.Equal(expectedDateTimeInstant.Kind, flooredDateTime.Kind);
        }

        private static DateTimeOffset GetDateTimeOffsetInstant(string dateTimeOffsetString)
        {
            dateTimeOffsetString = dateTimeOffsetString.Trim();

            var dateTime = DateTimeOffset.ParseExact(
                dateTimeOffsetString,
                new[]
                {
                    "yyyy-MM-dd HH:mm:ss.fffffff zzz",
                },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);

            return dateTime;
        }

        private static DateTime GetDateTimeInstant(string dateTimeString, DateTimeKind kind)
        {
            dateTimeString = dateTimeString.Trim();

            var dateTime = DateTime.ParseExact(
                dateTimeString,
                new[]
                {
                    "yyyy-MM-dd HH:mm:ss.fffffff",
                },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);

            dateTime = DateTime.SpecifyKind(dateTime, kind);

            return dateTime;
        }
    }
}
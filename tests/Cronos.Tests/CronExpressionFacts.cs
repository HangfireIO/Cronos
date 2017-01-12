using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using NodaTime;
using Xunit;

namespace Cronos.Tests
{
    public class CronExpressionFacts
    {
        [Fact]
        public void BasicFact()
        {
            var expression = CronExpression.Parse("* * * * * *");

            var result = expression.IsMatch(new LocalDateTime(2016, 03, 18, 12, 0, 0));

            Assert.True(result);
        }

        [Fact]
        public void Parse_ThrowAnException_WhenCronExressionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => CronExpression.Parse(null));

            Assert.Equal("cronExpression", exception.ParamName);
        }

        [Fact]
        public void Parse_ThrowAnException_WhenCronExpressionIsEmpty()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => CronExpression.Parse(""));

            Assert.Equal("cronExpression", exception.ParamName);
        }

        [Fact]
        public void Parse_ThrowAnException_WhenCronExpressionDoesNotContainSeconds()
        {
            var exception = Assert.Throws<ArgumentException>(() => CronExpression.Parse("* * * * *"));

            Assert.Equal("cronExpression", exception.ParamName);
        }

        [Theory]
        [MemberData(nameof(GetRandomDates))]
        public void IsMatch_ReturnsTrueForAnyDate_When6StarsWerePassed(LocalDateTime dateTime)
        {
            var expression = CronExpression.Parse("* * * * * *");

            var result = expression.IsMatch(dateTime);

            Assert.True(result);
        }

        [Theory]
        [InlineData("00 20 * * * *", true)]
        [InlineData("00 19,20,21 * * * *", true)]
        [InlineData("00 10-30 * * * *", true)]
        [InlineData("00 */20 * * * *", true)]
        [InlineData("00 10 * * * *", false)]
        [InlineData("00 10,30 * * * *", false)]
        [InlineData("00 10-19 * * * *", false)]
        [InlineData("00 */30 * * * *", false)]
        public void IsMatch_ReturnsCorrectResult_WhenOnlyMinutesAreSpecified(string cronExpression, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 17, 20));

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("* * 15 * * *", true)]
        [InlineData("* * 14,15,16 * * *", true)]
        [InlineData("* * 10-20 * * *", true)]
        [InlineData("* * */5 * * *", true)]
        [InlineData("* * 10 * * *", false)]
        [InlineData("* * 10,20 * * *", false)]
        [InlineData("* * 16-23 * * *", false)]
        [InlineData("* * */20 * * *", false)]
        public void IsMatch_ReturnsCorrectResult_WhenOnlyHoursAreSpecified(string cronExpression, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 15, 20));

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("* * * 9 * *", true)]
        [InlineData("* * * 09 * *", true)]
        [InlineData("* * * 7,8,9 * *", true)]
        [InlineData("* * * 5-10 * *", true)]
        [InlineData("* * * */4 * *", true)] // TODO: That's bad
        [InlineData("* * * 10 * *", false)]
        [InlineData("* * * 10,20 * *", false)]
        [InlineData("* * * 16-23 * *", false)]
        [InlineData("* * * */3 * *", false)] // TODO: That's bad
        public void IsMatch_ReturnsCorrectResult_WhenOnlyDaysAreSpecified(string cronExpression, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 15, 20));

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("* * * * 12 *", true)]
        [InlineData("* * * * 3,5,12 *", true)]
        [InlineData("* * * * 5-12 *", true)]
        [InlineData("* * * * DEC *", true)]
        [InlineData("* * * * mar-dec *", true)]
        [InlineData("* * * * */4 *", false)] // TODO: That's very bad
        [InlineData("* * * * 10 *", false)]
        [InlineData("* * * * 10,11 *", false)]
        [InlineData("* * * * 03-10 *", false)]
        [InlineData("* * * * */3 *", false)] // TODO: That's very bad
        [InlineData("* * * * */5 *", false)]
        [InlineData("* * * * APR-NOV *", false)]
        public void IsMatch_ReturnsCorrectResult_WhenOnlyMonthsAreSpecified(string cronExpression, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 15, 20));

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("* * * * * 5", true)]
        [InlineData("* * * * * 05", true)]
        [InlineData("* * * * * 3,5,7", true)]
        [InlineData("* * * * * 4-7", true)]
        [InlineData("* * * * * FRI", true)]
        [InlineData("* * * * * thu-sat", true)]
        [InlineData("* * * * * */5", true)]
        [InlineData("* * * * * thu-sun", false)] // TODO: that's bad
        [InlineData("* * * * * 2", false)]
        [InlineData("* * * * * 1,3", false)]
        [InlineData("* * * * * 02-4", false)]
        [InlineData("* * * * * */3", false)]
        [InlineData("* * * * * mon-wed", false)]
        public void IsMatch_ReturnsCorrectResult_WhenOnlyDaysOfWeekAreSpecified(string cronExpression, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 15, 20)); // It's Friday!

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("00 47 17 09 12 5")]
        [InlineData("00 47 17 09 DEC FRI")]
        [InlineData("00 40-50 15-20 5-10 11,12 5,6,7")]
        public void IsMatch_ReturnsTrue_WhenAllFieldsMatchTheSpecifiedDate(string cronExpression)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 17, 47));

            Assert.True(result);
        }

        [Theory]
        [InlineData("00 47 17 09 12 5", true)] // For reference
        [InlineData("00 40 17 09 12 5")]
        [InlineData("00 47 15 09 12 5")]
        [InlineData("00 47 17 12 12 *")]
        [InlineData("00 47 17 09 3 5")]
        [InlineData("00 47 17 * 12 4")]
        public void IsMatch_ReturnsFalse_WhenAnyFieldDoesNotMatchTheSpecifiedDate(string cronExpression, bool shouldMatch = false)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 17, 47));

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("00 05 18 * 12 *", true)]
        [InlineData("00 05 18 09 12 05", true)]
        [InlineData("00 05 18 01 12 05", true)]
        [InlineData("00 05 18 09 12 01", true)]
        [InlineData("00 05 18 01 12 *", false)]
        [InlineData("00 05 18 *  12 07", false)]
        public void IsMatch_HandlesSpecialCase_WhenBoth_DayOfWeek_And_DayOfMonth_WereSet(string cronExpression, bool shouldMatch)
        {
            // The dom/dow situation is odd:  
            //     "* * 1,15 * Sun" will run on the first and fifteenth AND every Sunday; 
            //     "* * * * Sun" will run *only* on Sundays; 
            //     "* * 1,15 * *" will run *only * the 1st and 15th.
            // this is why we keep DayOfMonthStar and DayOfWeekStar.
            // Yes, it's bizarre. Like many bizarre things, it's the standard.

            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 18, 05));

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("00 00 00 11 12 0")]
        [InlineData("00 00 00 11 12 7")]
        [InlineData("00 00 00 11 12 SUN")]
        [InlineData("00 00 00 11 12 sun")]
        public void IsMatch_HandlesSpecialCase_ForSundays(string cronExpression)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 11, 00, 00));

            Assert.True(result);
        }

        [Fact]
        public void Next_()
        {
            var expression = CronExpression.Parse("* * * * * *");
            var now = new LocalDateTime(2016, 12, 16, 00, 00, 00).InUtc();

            var result = expression.Next(now);

            Assert.Equal(now, result);
        }

        [Fact]
        public void Next2()
        {
            var expression = CronExpression.Parse("00 05 * * * *");
            var now = new LocalDateTime(2016, 12, 16, 00, 00, 00).InUtc();

            var result = expression.Next(now);

            Assert.Equal(now.Plus(Duration.FromMinutes(5)), result);
        }

        private static readonly DateTimeZone TimeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull("America/New_York");

        [Fact]
        public void Next3()
        {
            var expression = CronExpression.Parse("00 30 02 * * *");
            var now = new LocalDateTime(2016, 03, 13, 00, 00).InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2016, 03, 13, 03, 00), result.Value.LocalDateTime);
        }

        [Fact]
        public void Next4()
        {
            var expression = CronExpression.Parse("00 */30 * * * *");
            var now = new LocalDateTime(2016, 03, 13, 01, 45).InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2016, 03, 13, 03, 00), result.Value.LocalDateTime);
        }

        [Fact]
        public void Next5()
        {
            var expression = CronExpression.Parse("00 30 02 13 03 *");
            var now = new LocalDateTime(2016, 03, 13, 01, 45).InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2016, 03, 13, 03, 00), result.Value.LocalDateTime);
        }

        [Fact]
        public void Next6()
        {
            var expression = CronExpression.Parse("00 30 02 13 03 *");
            var now = new LocalDateTime(2016, 03, 13, 03, 00).InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2017, 03, 13, 02, 30), result.Value.LocalDateTime);
        }

        [Fact]
        public void Next7()
        {
            var expression = CronExpression.Parse("00 */30 02 13 03 *");
            var now = new LocalDateTime(2016, 03, 13, 01, 45).InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2017, 03, 13, 02, 00), result.Value.LocalDateTime);
        }

        [Fact]
        public void Dst()
        {
            CreateEntry("0 */30 * * * *");

            ExecuteSchedulerStandardTimeToDaylightSavingTime();
            // Skipped due to intervals, no problems here

            AssertExecutedAt(
                "00:00 ST",
                "00:30 ST",
                "01:00 ST",
                "01:30 ST",
                //"02:00 ST" - invalid time
                //"02:30 ST" - invalid time
                "03:00 DST",
                "03:30 DST",
                "04:00 DST",
                "04:30 DST");
        }

        [Fact]
        public void Dst2()
        {
            CreateEntry("0 */30 */2 * * *");

            ExecuteSchedulerStandardTimeToDaylightSavingTime();
            // Skipped due to intervals, can be avoided by enumerating hours and minutes
            // "0,30 0-23/2 * * *"
            AssertExecutedAt(
                "00:00 ST",
                "00:30 ST",
                //"02:00 ST" - invalid time
                //"02:30 ST" - invalid time
                "04:00 DST",
                "04:30 DST");
        }

        [Fact]
        public void Dst21()
        {
            CreateEntry("0 0,30 0-23/2 * * *");

            ExecuteSchedulerStandardTimeToDaylightSavingTime();

            // Run missed, strict
            AssertExecutedAt(
                "00:00 ST",
                "00:30 ST",
                //"02:00 ST" - invalid time
                //"02:30 ST" - invalid time
                "03:00 DST", // 02:30 equivalent skipped, but...
                "04:00 DST",
                "04:30 DST");
        }

        [Fact]
        public void Dst312()
        {
            CreateEntry("0 0 * * * *");

            ExecuteSchedulerStandardTimeToDaylightSavingTime();

            // Duplicates removed
            AssertExecutedAt(
                "00:00 ST",
                "01:00 ST",
                //"02:00 ST" - invalid time, skipped
                "03:00 DST",
                "04:00 DST");
        }

        [Fact]
        public void Dst3()
        {
            CreateEntry("0 */30 2 * * *");

            ExecuteSchedulerStandardTimeToDaylightSavingTime();

            // TODO: may be confusing!
            // Skipped due to intervals, can be avoided by using "0,30 02 * * *"
            AssertExecutedAt(
                //"02:00 ST" - invalid time
                //"02:30 ST" - invalid time
                new string[0]);
        }

        [Fact]
        public void Dst4()
        {
            CreateEntry("0 0,30 2 * * *");

            ExecuteSchedulerStandardTimeToDaylightSavingTime();

            // TODO: exclude duplicates
            // Run missed
            AssertExecutedAt(
                //"02:00 ST" - invalid time
                //"02:30 ST" - invalid time
                "03:00 DST");
        }

        [Fact]
        public void Dst5()
        {
            CreateEntry("0 30 2 * * *");

            ExecuteSchedulerStandardTimeToDaylightSavingTime();

            // Run missed, delay
            AssertExecutedAt(
                //"02:30 ST" - invalid time
                "03:00 DST");
        }

        [Fact]
        public void Dst6()
        {
            CreateEntry("0 0 */2 * * *");

            ExecuteSchedulerStandardTimeToDaylightSavingTime();

            // Skipped due to intervals, "0 0-23/2 * * *" can be used to avoid skipping
            // TODO: differ from Linux Cron
            AssertExecutedAt(
                "00:00 ST",
                //"02:00 ST" - invalid time
                "03:00 DST",
                "04:00 DST");
        }

        [Fact]
        public void Dst61()
        {
            CreateEntry("0 0 0-23/2 * * *");

            ExecuteSchedulerStandardTimeToDaylightSavingTime();

            // Run missed
            AssertExecutedAt(
                "00:00 ST",
                "03:00 DST",
                //"02:00 ST" - invalid time
                "04:00 DST");
        }

        [Fact]
        public void DstToSt1()
        {
            CreateEntry("0 */30 * * * *");

            ExecuteSchedulerDaylightSavingTimeToStandardTime();

            // As usual due to intervals
            AssertExecutedAt(
                "00:00 DST",
                "00:30 DST",
                "01:00 DST",
                "01:30 DST",
                "01:00 ST",
                "01:30 ST",
                "02:00 ST",
                "02:30 ST");
        }

        [Fact]
        public void DstToSt2()
        {
            CreateEntry("0 */30 */2 * * *");

            ExecuteSchedulerDaylightSavingTimeToStandardTime();

            // As usual due to intervals
            AssertExecutedAt(
                "00:00 DST",
                "00:30 DST",
                // 02:00 DST == 01:00 ST, one hour delay
                "02:00 ST",
                "02:30 ST");
        }

        [Fact]
        public void DstToSt3()
        {
            CreateEntry("0 0 1 * * *");

            ExecuteSchedulerDaylightSavingTimeToStandardTime();

            // Duplicates skipped, due to strict
            AssertExecutedAt(
                "01:00 DST"
                //"01:00 ST" - ignore
                );
        }

        [Fact]
        public void DstToSt4()
        {
            CreateEntry("0 */30 1 * * *");

            ExecuteSchedulerDaylightSavingTimeToStandardTime();

            // TODO: differ from Linux Cron
            // Duplicates skipped due to non-wildcard hour
            AssertExecutedAt(
                "01:00 DST",
                "01:30 DST",
                "01:00 ST",
                "01:30 ST");
        }

        [Fact]
        public void DstToSt6()
        {
            CreateEntry("0 0,30 1 * * *");

            ExecuteSchedulerDaylightSavingTimeToStandardTime();

            // Duplicates skipped due to non-wildcard
            AssertExecutedAt(
                "01:00 DST",
                "01:30 DST"
                //"01:00 ST"
                //"01:30 ST"
                );
        }

        [Fact]
        public void DstToSt5()
        {
            CreateEntry("0 0 */2 * * *");

            ExecuteSchedulerDaylightSavingTimeToStandardTime();

            // Duplicates skipped due to non-wildcard minute
            AssertExecutedAt(
                "00:00 DST",
                //02:00 DST == 01:00 ST, one hour delay
                "02:00 ST");
        }

        private CronExpression _expression;
        private ZonedDateTime[] _executed;
        private static readonly DateTimeZone America = DateTimeZoneProviders.Bcl.GetZoneOrNull("Eastern Standard Time");
        private readonly ZonedDateTime _start = new LocalDateTime(2016, 03, 13, 00, 00).InZoneStrictly(America);
        private readonly ZonedDateTime _end = new LocalDateTime(2016, 03, 13, 04, 59).InZoneStrictly(America);

        private void ExecuteSchedulerStandardTimeToDaylightSavingTime()
        {
            _executed = _expression.AllNext(_start, _end).ToArray();
        }

        private void ExecuteSchedulerDaylightSavingTimeToStandardTime()
        {
            _executed = _expression.AllNext(new LocalDateTime(2016, 11, 06, 00, 00).InZoneStrictly(America), new LocalDateTime(2016, 11, 06, 02, 59).InZoneStrictly(America)).ToArray();
        }

        private void CreateEntry(string expression)
        {
            _expression = CronExpression.Parse(expression);
        }

        private void AssertExecutedAt(params string[] expectedTimes)
        {
            var actualTimes = new List<string>();
            for (var i = 0; i < _executed.Length; i++)
            {
                var time = _executed[i];
                var sb = new StringBuilder();

                sb.Append(time.ToString("HH:mm", CultureInfo.InvariantCulture));
                sb.Append(" " + (time.IsDaylightSavingTime() ? "DST" : "ST"));

                actualTimes.Add(sb.ToString());
            }

            var combinedExpectedTimes = String.Join(", ", expectedTimes);
            var combinedActualTimes = String.Join(", ", actualTimes);

            Assert.Equal(combinedExpectedTimes, combinedActualTimes);
        }

        private static IEnumerable<object> GetRandomDates()
        {
            return new[]
            {
                new object[] { new LocalDateTime(2016, 12, 09, 16, 46) },
                new object[] { new LocalDateTime(2016, 03, 09, 16, 46) },
                new object[] { new LocalDateTime(2016, 12, 30, 16, 46) },
                new object[] { new LocalDateTime(2016, 12, 09, 02, 46) },
                new object[] { new LocalDateTime(2016, 12, 09, 16, 09) },
                new object[] { new LocalDateTime(2099, 12, 09, 16, 46) }
            };
        }
    }
}
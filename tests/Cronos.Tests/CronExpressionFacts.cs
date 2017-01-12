using System;
using System.Collections.Generic;
using System.Globalization;
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
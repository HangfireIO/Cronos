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
        private static readonly DateTimeZone America = DateTimeZoneProviders.Bcl.GetZoneOrNull("Eastern Standard Time");

        [Fact]
        public void BasicFact()
        {
            var expression = CronExpression.Parse("* * * * * ?");

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


        // seconds field is invalid.

        [Theory]
        [InlineData("60 * * * * *")]
        [InlineData("-1 * * * * *")]
        [InlineData("- * * * * *")]
        [InlineData(", * * * * *")]
        [InlineData(",1 * * * * *")]
        [InlineData("/ * * * * *")]
        [InlineData("*/ * * * * *")]
        [InlineData("1/ * * * * *")]
        [InlineData("# * * * * *")]
        [InlineData("*#1 * * * * *")]
        [InlineData("0#2 * * * * *")]
        [InlineData("L * * * * *")]
        [InlineData("W * * * * *")]
        [InlineData("LW * * * * *")]
        [InlineData("1/2147483648 * * * * ?")]
        [InlineData("? * * * * ?")]

        // minute field is invalid.

        [InlineData("* 60 * * * *")]
        [InlineData("* -1 * * * *")]
        [InlineData("* - * * * *")]
        [InlineData("* , * * * *")]
        [InlineData("* ,1 * * * *")]
        [InlineData("* / * * * *")]
        [InlineData("* # * * * *")]
        [InlineData("* *#1 * * * *")]
        [InlineData("* 5#3 * * * *")]
        [InlineData("* L * * * *")]
        [InlineData("* W * * * *")]
        [InlineData("* LW * * * *")]
        [InlineData("* ? * * * ?")]

        // hour field is invalid.

        [InlineData("* * 25 * * *")]
        [InlineData("* * -1 * * *")]
        [InlineData("* * - * * *")]
        [InlineData("* * , * * *")]
        [InlineData("* * ,1 * * *")]
        [InlineData("* * / * * *")]
        [InlineData("* * # * * *")]
        [InlineData("* * *#2 * * *")]
        [InlineData("* * 10#1 * * *")]
        [InlineData("* * L * * *")]
        [InlineData("* * W * * *")]
        [InlineData("* * LW * * *")]
        [InlineData("* * ? * * ?")]

        // day of month field is invalid.

        [InlineData("* * * 32 * *")]
        [InlineData("* * * 31 4 *")]
        [InlineData("* * * 31 6 *")]
        [InlineData("* * * 31 9 *")]
        [InlineData("* * * 31 11 *")]
        [InlineData("* * * 30 2 *")]
        [InlineData("* * * 10-32 * *")]
        [InlineData("* * * 31-32 * *")]
        [InlineData("* * * 30-31 2 *")]
        [InlineData("* * * 30-31 2 *")]
        [InlineData("* * * -1 * *")]
        [InlineData("* * * - * *")]
        [InlineData("* * * , * *")]
        [InlineData("* * * ,1 * *")]
        [InlineData("* * * / * *")]
        [InlineData("* * * # * *")]
        [InlineData("* * * *#3 * *")]
        [InlineData("* * * 4#1 * *")]
        [InlineData("* * * W * *")]

        //month is invalid

        [InlineData("* * * *  13 *")]
        [InlineData("* * * *  -1 *")]
        [InlineData("* * * *   - *")]
        [InlineData("* * * *   , *")]
        [InlineData("* * * * ,1 *")]
        [InlineData("* * * *   / *")]
        [InlineData("* * * *  */ *")]
        [InlineData("* * * *  1/ *")]
        [InlineData("* * * *   # *")]
        [InlineData("* * * * *#1 *")]
        [InlineData("* * * * 2#2 *")]
        [InlineData("* * * *   L *")]
        [InlineData("* * * *   W *")]
        [InlineData("* * * *  LW *")]
        [InlineData("? * * *   ? *")]

        // day of week field is invalid.

        [InlineData("* * * * * 8")]
        [InlineData("* * * * * -1")]
        [InlineData("* * * * * -")]
        [InlineData("* * * * * ,")]
        [InlineData(" * * * * ,1")]
        [InlineData("* * * * * /")]
        [InlineData("* * * * * */")]
        [InlineData("* * * * * 1/")]
        [InlineData("* * * * * #")]
        [InlineData("* * * * * 0#")]
        [InlineData("* * * * * 5#6")]
        [InlineData("* * * * * SUN#6")]
        [InlineData("* * * * * 0#0")]
        [InlineData("* * * * * SUT")]
        [InlineData("* * * * * L")]
        [InlineData("* * * * * W")]
        [InlineData("* * * * * LW")]
        public void Parse_ThrowAnException_WhenCronExpressionIsInvalid(string cronExpression)
        {
            var exception = Assert.Throws<ArgumentException>(() => CronExpression.Parse(cronExpression));

            Assert.Equal("cronExpression", exception.ParamName);
        }

        [Fact]
        public void Parse_ThrowException_WhenBoth_DayOfMonth_And_DayOfWeek_IsStar()
        {
            Assert.Throws<ArgumentException>(() => CronExpression.Parse("0 12 12 * * *"));
        }

        [Theory]
        [MemberData(nameof(GetRandomDates))]
        public void IsMatch_ReturnsTrueForAnyDate_When6StarsWerePassed(LocalDateTime dateTime)
        {
            var expression = CronExpression.Parse("* * * * * ?");

            var result = expression.IsMatch(dateTime);

            Assert.True(result);
        }

        [Theory]
        [InlineData("20 * * * * ?", true)]
        [InlineData("19,20,21 * * * * ?", true)]
        [InlineData("10-30 * * * * ?", true)]
        [InlineData("*/20 * * * * ?", true)]
        [InlineData("10 * * * * ?", false)]
        [InlineData("10,30 * * * * ?", false)]
        [InlineData("10-19 * * * * ?", false)]
        [InlineData("*/30 * * * * ?", false)]
        public void IsMatch_ReturnsCorrectResult_WhenOnlySecondsAreSpecified(string cronExpression, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 17, 35, 20));

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("59 * * * * ?")]
        [InlineData("40-59 * * * * ?")]
        [InlineData("1,59 * * * * ?")]
        public void IsMatch_ReturnsCorrectResult_WhenMaxSecondsAreSpecified(string cronExpression)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2017, 01, 13, 17, 35, 59));

            Assert.True(result);
        }

        [Theory]
        [InlineData("0 * * * * ?", true)]
        [InlineData("0-10 * * * * ?", true)]
        [InlineData("0,14 * * * * ?", true)]
        public void IsMatch_ReturnsCorrectResult_WhenMinSecondsAreSpecified(string cronExpression, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2017, 01, 13, 17, 35, 00));

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("* 20 * * * ?", true)]
        [InlineData("* 19,20,21 * * * ?", true)]
        [InlineData("* 10-30 * * * ?", true)]
        [InlineData("* */20 * * * ?", true)]
        [InlineData("* 10 * * * ?", false)]
        [InlineData("* 10,30 * * * ?", false)]
        [InlineData("* 10-19 * * * ?", false)]
        [InlineData("* */30 * * * ?", false)]
        public void IsMatch_ReturnsCorrectResult_WhenOnlyMinutesAreSpecified(string cronExpression, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 17, 20));

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("* * 15 * * ?", true)]
        [InlineData("* * 14,15,16 * * ?", true)]
        [InlineData("* * 10-20 * * ?", true)]
        [InlineData("* * */5 * * ?", true)]
        [InlineData("* * 10 * * ?", false)]
        [InlineData("* * 10,20 * * ?", false)]
        [InlineData("* * 16-23 * * ?", false)]
        [InlineData("* * */20 * * ?", false)]
        public void IsMatch_ReturnsCorrectResult_WhenOnlyHoursAreSpecified(string cronExpression, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 15, 20));

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("* * * 9 * ?", true)]
        [InlineData("* * * 09 * ?", true)]
        [InlineData("* * * 7,8,9 * ?", true)]
        [InlineData("* * * 5-10 * ?", true)]
        [InlineData("* * * */4 * ?", true)] // TODO: That's bad
        [InlineData("* * * 10 * ?", false)]
        [InlineData("* * * 10,20 * ?", false)]
        [InlineData("* * * 16-23 * ?", false)]
        [InlineData("* * * */3 * ?", false)] // TODO: That's bad
        public void IsMatch_ReturnsCorrectResult_WhenOnlyDaysOfMonthAreSpecified(string cronExpression, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 15, 20));

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("* * * ? 12 *", true)]
        [InlineData("* * * ? 3,5,12 *", true)]
        [InlineData("* * * ? 5-12 *", true)]
        [InlineData("* * * ? DEC *", true)]
        [InlineData("* * * ? mar-dec *", true)]
        [InlineData("* * * ? */4 *", false)] // TODO: That's very bad
        [InlineData("* * * ? 10 *", false)]
        [InlineData("* * * ? 10,11 *", false)]
        [InlineData("* * * ? 03-10 *", false)]
        [InlineData("* * * ? */3 *", false)] // TODO: That's very bad
        [InlineData("* * * ? */5 *", false)]
        [InlineData("* * * ? APR-NOV *", false)]
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
        [InlineData("* * * * * FRI/3", true)]
        [InlineData("* * * * * thu-sat", true)]
        [InlineData("* * * ? * */5", true)]
        [InlineData("* * * * * thu-sun", false)] // TODO: that's bad
        [InlineData("* * * * * 2", false)]
        [InlineData("* * * * * 1,3", false)]
        [InlineData("* * * * * 02-4", false)]
        [InlineData("* * * ? * */3", false)]
        [InlineData("* * * * * thu/2", false)]
        [InlineData("* * * * * mon-wed", false)]
        public void IsMatch_ReturnsCorrectResult_WhenOnlyDaysOfWeekAreSpecified(string cronExpression, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 15, 20)); // It's Friday!

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("54 47 17 09 12 5")]
        [InlineData("54 47 17 09 DEC FRI")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7")]
        public void IsMatch_ReturnsTrue_WhenAllFieldsMatchTheSpecifiedDate(string cronExpression)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 17, 47, 54));

            Assert.True(result);
        }

        [Theory]
        [InlineData("54 47 17 09 12 5", true)] // For reference
        [InlineData("50 47 17 09 12 5")]
        [InlineData("54 40 17 09 12 5")]
        [InlineData("54 47 15 09 12 5")]
        [InlineData("54 47 17 12 12 *")]
        [InlineData("54 47 17 09 3 5")]
        [InlineData("54 47 17 * 12 4")]
        public void IsMatch_ReturnsFalse_WhenAnyFieldDoesNotMatchTheSpecifiedDate(string cronExpression, bool shouldMatch = false)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2016, 12, 09, 17, 47, 54));

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("00 05 18 13 01 05", true)]
        [InlineData("00 05 18 13 *  05", true)]
        [InlineData("00 05 18 13 01 01", false)]
        [InlineData("00 05 18 01 01 05", false)]
        [InlineData("05 05 18 13 01 05", false)]
        [InlineData("00 00 18 13 01 05", false)]
        [InlineData("00 05 00 13 01 05", false)]
        [InlineData("00 05 18 13 12 05", false)]
        public void IsMatch_HandlesSpecialCase_WhenBoth_DayOfWeek_And_DayOfMonth_WereSet(string cronExpression, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.IsMatch(new LocalDateTime(2017, 01, 13, 18, 05));

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

        [Theory]
        [MemberData(nameof(GetLastDaysOfMonth))]
        public void IsMatch_ReturnTrue_WhenLMarkInDayOfMonthMatchesTheSpecifiedDate(LocalDateTime dateTime)
        {
            var expression = CronExpression.Parse("* * * L * ?");

            var result = expression.IsMatch(dateTime);

            Assert.True(result);
        }

        [Theory]
        [MemberData(nameof(GetNotLastDaysOfMonth))]
        public void IsMatch_ReturnTrue_WhenLMarkInDayOfMonthDoesNotMatchTheSpecifiedDate(LocalDateTime dateTime)
        {
            var expression = CronExpression.Parse("* * * L * ?");

            var result = expression.IsMatch(dateTime);

            Assert.False(result);
        }

        [Theory]
        [InlineData("* * * ? * SUN#1", 2017, 1, 1, true)]
        [InlineData("* * * ? * 0#1", 2017, 1, 1, true)]
        [InlineData("* * * ? * 0#2", 2017, 1, 8, true)]
        [InlineData("* * * ? * 5#3", 2017, 1, 20,  true)]
        [InlineData("* * * ? * 5#3", 2017, 5, 19,  true)]
        [InlineData("* * * ? * 0#1", 2017, 1, 8,  false)]
        [InlineData("* * * ? * 0#2", 2017, 1, 1,  false)]
        [InlineData("* * * ? * 3#2", 2017, 1, 24, false)]
        public void IsMatch_ReturnCorrectValue_WhenSharpIsUsedInDayOfWeek(string cronExpression, int year, int month, int day, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            var dateTime = new LocalDateTime(year, month, day, 0, 0);
            var result = expression.IsMatch(dateTime);

            Assert.Equal(shouldMatch, result);
        }

        [Theory]
        [InlineData("* * * 1W * ?", 2017, 1, 2, true)]
        [InlineData("* * * 1W * ?", 2017, 1, 1, false)]
        public void IsMatch_ReturnCorrectValue_WhenWIsUsedInDayOfMonth(string cronExpression, int year, int month, int day, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            var dateTime = new LocalDateTime(year, month, day, 0, 0);
            var result = expression.IsMatch(dateTime);

            Assert.Equal(shouldMatch, result);
        }

        [Fact]
        public void Next_()
        {
            var expression = CronExpression.Parse("* * * * * ?");
            var now = new LocalDateTime(2016, 12, 16, 00, 00, 00).InUtc();

            var result = expression.Next(now);

            Assert.Equal(now, result);
        }

        [Fact]
        public void Next2()
        {
            var expression = CronExpression.Parse("00 05 * * * ?");
            var now = new LocalDateTime(2016, 12, 16, 00, 00, 00).InUtc();

            var result = expression.Next(now);

            Assert.Equal(now.Plus(Duration.FromMinutes(5)), result);
        }

        private static readonly DateTimeZone TimeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull("America/New_York");

        [Fact]
        public void Next3()
        {
            var expression = CronExpression.Parse("00 30 02 * * ?");
            var now = new LocalDateTime(2016, 03, 13, 00, 00).InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2016, 03, 13, 03, 00), result?.LocalDateTime);
        }

        [Fact]
        public void Next4()
        {
            var expression = CronExpression.Parse("00 */30 * * * ?");
            var now = new LocalDateTime(2016, 03, 13, 01, 45).InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2016, 03, 13, 03, 00), result?.LocalDateTime);
        }

        [Fact]
        public void Next5()
        {
            var expression = CronExpression.Parse("00 30 02 13 03 ?");
            var now = new LocalDateTime(2016, 03, 13, 01, 45).InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2016, 03, 13, 03, 00), result?.LocalDateTime);
        }

        [Fact]
        public void Next6()
        {
            var expression = CronExpression.Parse("00 30 02 13 03 ?");
            var now = new LocalDateTime(2016, 03, 13, 03, 00).InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2017, 03, 13, 02, 30), result?.LocalDateTime);
        }

        [Fact]
        public void Next7()
        {
            var expression = CronExpression.Parse("00 */30 02 13 03 *");
            var now = new LocalDateTime(2016, 03, 13, 01, 45).InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2017, 03, 13, 02, 00), result?.LocalDateTime);
        }

        [Fact]
        public void Next_HandleMovingToNextMinute()
        {
            var expression = CronExpression.Parse("0 * * * * ?");

            var now = new LocalDateTime(2017, 1, 14, 12, 58, 59).InZoneStrictly(TimeZone);
            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2017, 1, 14, 12, 59, 0), result?.LocalDateTime);
        }

        [Fact]
        public void Next_HandleMovingToNextHour()
        {
            var expression = CronExpression.Parse("0 0 * * * ?");

            var now = new LocalDateTime(2017, 1, 14, 12, 59, 0).InZoneStrictly(TimeZone);
            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2017, 1, 14, 13, 0, 0), result?.LocalDateTime);
        }

        [Fact]
        public void Next_HandleMovingToNextDay()
        {
            var expression = CronExpression.Parse("0 0 0 * * ?");

            var now = new LocalDateTime(2017, 1, 14, 23, 0, 0).InZoneStrictly(TimeZone);
            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2017, 1, 15, 0, 0, 0), result?.LocalDateTime);
        }

        [Fact]
        public void Next_HandleMovingToNextMonth()
        {
            var expression = CronExpression.Parse("0 0 0 1 * ?");

            var now = new LocalDateTime(2017, 1, 31, 0, 0, 0).InZoneStrictly(TimeZone);
            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2017, 2, 1, 0, 0, 0), result?.LocalDateTime);
        }

        [Fact]
        public void Next_HandleMovingToNextYear()
        {
            var expression = CronExpression.Parse("0 0 0 * * ?");

            var now = new LocalDateTime(2017, 12, 31, 23, 59, 58).InZoneStrictly(TimeZone);
            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2018, 1, 1, 0, 0, 0), result?.LocalDateTime);
        }

        [Theory]
        [MemberData(nameof(GetNotLastDaysOfMonth))]
        public void Next_ReturnCorrectDate_WhenDayOfMonthIsSpecifiedAsLSymbol(LocalDateTime dateTime)
        {
            var expression = CronExpression.Parse("* * * L * ?");
            var now = dateTime.InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(now.Year, now.Month, now.Calendar.GetDaysInMonth(now.Year, now.Month), 00, 00), result?.LocalDateTime);
        }

        [Theory]
        [MemberData(nameof(GetLastDaysOfMonth))]
        public void Next_ReturnCorrectDate_WhenDayOfMonthIsSpecifiedAsLSymbol2(LocalDateTime dateTime)
        {
            var expression = CronExpression.Parse("* * * L * ?");
            var now = dateTime.InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(dateTime, result?.LocalDateTime);
        }

        [Theory]
        [InlineData(2017, 01, 29, "* * * ? * 0L")] // last sunday
        [InlineData(2017, 01, 30, "* * * ? * 1L")] // last monday
        [InlineData(2017, 01, 31, "* * * ? * 2L")] // last tuesday
        [InlineData(2017, 01, 25, "* * * ? * 3L")] // last wednesday
        [InlineData(2017, 01, 26, "* * * ? * 4L")] // last thursday
        [InlineData(2017, 01, 27, "* * * ? * 5L")] // last friday
        [InlineData(2017, 01, 28, "* * * ? * 6L")] // last saturday
        [InlineData(2017, 01, 29, "* * * ? * 7L")] // last sunday
        public void Next_ReturnTheSameDate_WhenDayOfWeekIsSpecifiedAsLSymbol_And_DateGivenDoesMatch(int year, int month, int day, string cronExpression)
        {
            var dateTime = new LocalDateTime(year, month, day, 0, 0);
            var expression = CronExpression.Parse(cronExpression);
            var now = dateTime.InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(dateTime, result?.LocalDateTime);
        }

        [Theory]
        [InlineData(2017, 01, 25, "* * * ? * 0L", 2017, 01, 29)]
        [InlineData(2017, 01, 01, "* * * ? * 1L", 2017, 01, 30)]
        [InlineData(2017, 03, 29, "* * * ? * 2L", 2017, 04, 25)]
        [InlineData(2017, 12, 31, "* * * ? * 5L", 2018, 01, 26)]
        public void Next_ReturnCorrectValue_WhenDayOfWeekIsSpecifiedAsLSymbol(
            int startYear, 
            int startMonth, 
            int startDay, 
            string cronExpression, 
            int expectedYear, 
            int expectedMonth, 
            int expectedDay)
        {
            var dateTime = new LocalDateTime(startYear, startMonth, startDay, 0, 0);
            var expression = CronExpression.Parse(cronExpression);
            var now = dateTime.InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(expectedYear, expectedMonth, expectedDay, 0, 0), result?.LocalDateTime);
        }

        [Theory]
        [InlineData(2017, 01, 25, "* * * ? * 0#5", 2017, 01, 29)]
        [InlineData(2017, 01, 30, "* * * ? * 0#5", 2017, 04, 30)]
        public void Next_ReturnCorrectValue_WhenSharpIsUsedInDayOfWeek(
           int startYear,
           int startMonth,
           int startDay,
           string cronExpression,
           int expectedYear,
           int expectedMonth,
           int expectedDay)
        {
            var dateTime = new LocalDateTime(startYear, startMonth, startDay, 0, 0);
            var expression = CronExpression.Parse(cronExpression);
            var now = dateTime.InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(expectedYear, expectedMonth, expectedDay, 0, 0), result?.LocalDateTime);
        }


        [Theory]

        // Skipped due to intervals, no problems here
        [InlineData("0 */30 * * * ?",
            "00:00 ST",
            "00:30 ST",
            "01:00 ST",
            "01:30 ST",
            //"02:00 ST" - invalid time
            //"02:30 ST" - invalid time
            "03:00 DST",
            "03:30 DST",
            "04:00 DST",
            "04:30 DST")]

        // Skipped due to intervals, can be avoided by enumerating hours and minutes
        // "0,30 0-23/2 * * *"
        [InlineData("0 */30 */2 * * ?",
            "00:00 ST",
            "00:30 ST",
            //"02:00 ST" - invalid time
            //"02:30 ST" - invalid time
            "04:00 DST",
            "04:30 DST")]

        // Run missed, strict
        [InlineData("0 0,30 0-23/2 * * ?",
            "00:00 ST",
            "00:30 ST",
            //"02:00 ST" - invalid time
            //"02:30 ST" - invalid time
            "03:00 DST", // 02:30 equivalent skipped, but...
            "04:00 DST",
            "04:30 DST")]

        // Duplicates removed
        [InlineData("0 0 * * * ?",
            "00:00 ST",
            "01:00 ST",
            //"02:00 ST" - invalid time, skipped
            "03:00 DST",
            "04:00 DST")]

        // TODO: may be confusing!
        // Skipped due to intervals, can be avoided by using "0,30 02 * * *"
        [InlineData("0 */30 2 * * ?",
            //"02:00 ST" - invalid time
            //"02:30 ST" - invalid time
            new string[0])]

        // TODO: exclude duplicates
        // Run missed
        [InlineData("0 0,30 2 * * ?",
            //"02:00 ST" - invalid time
            //"02:30 ST" - invalid time
            "03:00 DST")]

        // Run missed, delay
        [InlineData("0 30 2 * * ?",
            //"02:30 ST" - invalid time
            "03:00 DST")]

        // Skipped due to intervals, "0 0-23/2 * * *" can be used to avoid skipping
        // TODO: differ from Linux Cron
        [InlineData("0 0 */2 * * ?",
            "00:00 ST",
            //"02:00 ST" - invalid time
            "03:00 DST",
            "04:00 DST")]

        // Run missed
        [InlineData("0 0 0-23/2 * * ?",
            "00:00 ST",
            //"02:00 ST" - invalid time
            "03:00 DST",
            "04:00 DST")]
        public void AllNext_HandleDST_WhenTheClockJumpsForward(string cronExpression, params string[] expectedExecutingTimes)
        {
            var expression = CronExpression.Parse(cronExpression);

            var executed = expression.AllNext(
                new LocalDateTime(2016, 03, 13, 00, 00).InZoneStrictly(America),
                new LocalDateTime(2016, 03, 13, 04, 59).InZoneStrictly(America)
            ).ToArray();

            AssertExecutedAt(executed, expectedExecutingTimes);
        }

        [Theory]
        // Skipped due to intervals, no problems here
        [InlineData("0 */30 * * * ?", "03:00 DST")]

        // Skipped due to intervals, can be avoided by enumerating hours and minutes
        // "0,30 0-23/2 * * *"
        [InlineData("0 */30 */2 * * ?", "04:00 DST")]

        // Run missed, strict
        [InlineData("0 0,30 0-23/2 * * ?", "03:00 DST")]

        // Duplicates removed
        [InlineData("0 0 * * * ?", "03:00 DST")]

        // TODO: may be confusing!
        // Skipped due to intervals, can be avoided by using "0,30 02 * * *"
        [InlineData("0 */30 2 * * ?", null)]

        // TODO: exclude duplicates
        // Run missed
        [InlineData("0 0,30 2 * * ?", "03:00 DST")]

        // Run missed, delay
        [InlineData("0 30 2 * * ?", "03:00 DST")]

        // Skipped due to intervals, "0 0-23/2 * * *" can be used to avoid skipping
        // TODO: differ from Linux Cron
        [InlineData("0 0 */2 * * ?", "03:00 DST")]

        // Run missed
        [InlineData("0 0 0-23/2 * * ?", "03:00 DST")]
        public void Next_HandleDST_WhenTheClockJumpsForward(string cronExpression, string expectedTime)
        {
            // Arrange
            var expression = CronExpression.Parse(cronExpression);

            var lastStandardTime = new LocalDateTime(2016, 03, 13, 01, 59, 59).InZoneStrictly(America);
            var endDateTime = new LocalDateTime(2016, 03, 13, 23, 59, 59).InZoneStrictly(America);

            // Act
            var executed = expression.Next(lastStandardTime);

            if (executed > endDateTime) executed = null;

            // Assert
            Assert.Equal(expectedTime, DateTimeToString(executed));
        }

        [Theory]

        // As usual due to intervals
        [InlineData("0 */30 * * * ?",
            "00:00 DST",
            "00:30 DST",
            "01:00 DST",
            "01:30 DST",
            "01:00 ST",
            "01:30 ST",
            "02:00 ST",
            "02:30 ST")]

        // As usual due to intervals
        [InlineData("0 */30 */2 * * ?",
            "00:00 DST",
            "00:30 DST",
            // 02:00 DST == 01:00 ST, one hour delay
            "02:00 ST",
            "02:30 ST")]

        // As usual due to intervals
        [InlineData("0 0 1 * * ?",
            "01:00 DST"
            //"01:00 ST" - ignore
            )]

        // TODO: differ from Linux Cron
        // Duplicates skipped due to non-wildcard hour
        [InlineData("0 */30 1 * * ?",
            "01:00 DST",
            "01:30 DST",
            "01:00 ST",
            "01:30 ST")]

        // Duplicates skipped due to non-wildcard minute
        [InlineData("0 0 */2 * * ?",
            "00:00 DST",
            //02:00 DST == 01:00 ST, one hour delay
            "02:00 ST")]

        // Duplicates skipped due to non-wildcard
        [InlineData("0 0,30 1 * * ?",
            "01:00 DST",
            "01:30 DST"
            //"01:00 ST"
            //"01:30 ST"
            )]
        
        // Duplicates skipped due to non-wildcard
        [InlineData("0 30 * * * ?",
            "00:30 DST",
            "01:30 DST",
            "01:30 ST",
            "02:30 ST"
            )]
        public void AllNext_HandleDST_WhenTheClockJumpsBackward(string cronExpression, params string[] expectedExecutingTimes)
        {
            //CreateEntry(expression);

            //ExecuteSchedulerDaylightSavingTimeToStandardTime();

            //AssertExecutedAt(expectedExecutingTimes);

            var expression = CronExpression.Parse(cronExpression);

            var executed = expression.AllNext(
                new LocalDateTime(2016, 11, 06, 00, 00).InZoneStrictly(America),
                new LocalDateTime(2016, 11, 06, 02, 59).InZoneStrictly(America)
            ).ToArray();

            AssertExecutedAt(executed, expectedExecutingTimes);
        }

        [Theory]

        // As usual due to intervals
        [InlineData("0 */30 * * * ?", "01:30 DST", "01:30 DST")]
        [InlineData("0 */30 * * * ?", "01:59 DST", "01:00 ST")]
        [InlineData("0 */30 * * * ?", "01:15 ST", "01:30 ST")]

        // As usual due to intervals
        [InlineData("0 */30 */2 * * ?", "01:30 DST", "02:00 ST")]

        // As usual due to intervals
        [InlineData("0 0 1 * * ?", "01:00 DST", "01:00 DST")]
        [InlineData("0 0 1 * * ?", "01:30 DST", null)]

        // TODO: differ from Linux Cron
        // Duplicates skipped due to non-wildcard hour
        [InlineData("0 */30 1 * * ?", "01:20 DST", "01:30 DST")]
        [InlineData("0 */30 1 * * ?", "01:59 DST", "01:00 ST")]
        [InlineData("0 */30 1 * * ?", "01:30 ST", "01:30 ST")]

        // Duplicates skipped due to non-wildcard minute
        [InlineData("0 0 */2 * * ?", "00:30 DST", "02:00 ST")]

        // Duplicates skipped due to non-wildcard
        [InlineData("0 0,30 1 * * ?", "01:00 DST", "01:00 DST")]
        [InlineData("0 0,30 1 * * ?", "01:20 DST", "01:30 DST")]
        [InlineData("0 0,30 1 * * ?", "01:59 DST", null)]

        // Duplicates skipped due to non-wildcard
        [InlineData("0 30 * * * ?", "01:30 DST", "01:30 DST")]
        [InlineData("0 30 * * * ?", "01:59 DST", "01:30 ST")]
        public void Next_HandleDST_WhenTheClockJumpsBackward(string cronExpression, string startTimeWithDstMarker, string expectedTime)
        {
            // Arrange
            var expression = CronExpression.Parse(cronExpression);

            var startDateTime = GetZonedDateTime(new LocalDate(2016, 11, 06), startTimeWithDstMarker);
            var endDateTime = new LocalDateTime(2016, 11, 06, 23, 59, 59).InZoneStrictly(America);

            // Act
            var executed = expression.Next(startDateTime);

            if (executed > endDateTime) executed = null;

            // Assert
            Assert.Equal(expectedTime, DateTimeToString(executed));
        }

        [Theory]
        [InlineData("0 12 12 * * ?")]
        [InlineData("0 12 12 */2 * ?")]
        [InlineData("0 12 12 11-18 * ?")]
        [InlineData("0 12 12 * 1 ?")]
        [InlineData("0 12 12 12 1 ?")]
        public void IsMatch_ReturnCorrectValue_WhenDayOfWeekSpecifiedAsQuestion(string cronExpression)
        {
            var expression = CronExpression.Parse(cronExpression);

            expression.IsMatch(new LocalDateTime(2017, 1, 12, 12, 12, 0));
        }

        [Theory]
        [InlineData("0 12 12 ? * *", true)]
        [InlineData("0 12 12 ? * TUE/3", false)]
        [InlineData("0 12 12 ? * THU", true)]
        public void IsMatch_ReturnCorrectValue_WhenDayOfMonthSpecifiedAsQuestion(string cronExpression, bool shouldMatch)
        {
            var expression = CronExpression.Parse(cronExpression);

            expression.IsMatch(new LocalDateTime(2017, 1, 12, 12, 12, 0));
        }

        [Theory]
        [InlineData("? * * * * *")]
        [InlineData("* ? * * * *")]
        [InlineData("* * ? * * *")]
        [InlineData("* * * * ? *")]
        public void IsMatch_HandlesQuestionMarkCantBeSpecfiedForDayOfMonthOrDayOfWeek(string cronExpression)
        {
            var exception = Assert.Throws<ArgumentException>(() => CronExpression.Parse(cronExpression));
            Assert.Equal("cronExpression", exception.ParamName);
        }

        private void AssertExecutedAt(ZonedDateTime[] executedTimes, params string[] expectedTimes)
        {
            var actualTimes = new List<string>();
            for (var i = 0; i < executedTimes.Length; i++)
            {
                var time = executedTimes[i];
                var sb = new StringBuilder();

                sb.Append(time.ToString("HH:mm", CultureInfo.InvariantCulture));
                sb.Append(" " + (time.IsDaylightSavingTime() ? "DST" : "ST"));

                actualTimes.Add(sb.ToString());
            }

            var combinedExpectedTimes = String.Join(", ", expectedTimes);
            var combinedActualTimes = String.Join(", ", actualTimes);

            Assert.Equal(combinedExpectedTimes, combinedActualTimes);
        }

        private static ZonedDateTime GetZonedDateTime(LocalDate date, string timeWithDstMarker)
        {
            var timeAndDstMarker = timeWithDstMarker.Split(' ');

            var time = TimeSpan.Parse(timeAndDstMarker[0]);
            var localDateTime = date.At(new LocalTime(time.Hours, time.Minutes));

            var isDst = timeAndDstMarker[1] == "DST";

            return isDst ?
                localDateTime.InZone(America, mapping => mapping.First()) :
                localDateTime.InZone(America, mapping => mapping.Last());
        }

        private string DateTimeToString(ZonedDateTime? zonedDateTime)
        {
            if (zonedDateTime == null) return null;
            var sb = new StringBuilder();

            sb.Append(zonedDateTime.Value.ToString("HH:mm", CultureInfo.InvariantCulture));
            sb.Append(" " + (zonedDateTime.Value.IsDaylightSavingTime() ? "DST" : "ST"));

            return sb.ToString();

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

        private static IEnumerable<object> GetLastDaysOfMonth()
        {
            return new[]
            {
                new object[] { new LocalDateTime(2016, 1, 31, 0, 0) },
                new object[] { new LocalDateTime(2016, 2, 29, 0, 0) },
                new object[] { new LocalDateTime(2017, 2, 28, 0, 0) },
                new object[] { new LocalDateTime(2100, 2, 28, 0, 0) },
                new object[] { new LocalDateTime(2016, 3, 31, 0, 0) },
                new object[] { new LocalDateTime(2016, 4, 30, 0, 0) },
                new object[] { new LocalDateTime(2016, 5, 31, 0, 0) },
                new object[] { new LocalDateTime(2016, 6, 30, 0, 0) },
                new object[] { new LocalDateTime(2016, 7, 31, 0, 0) },
                new object[] { new LocalDateTime(2016, 8, 31, 0, 0) },
                new object[] { new LocalDateTime(2016, 9, 30, 0, 0) },
                new object[] { new LocalDateTime(2016, 10, 31, 0, 0) },
                new object[] { new LocalDateTime(2016, 11, 30, 0, 0) },
                new object[] { new LocalDateTime(2016, 12, 31, 0, 0) },
                new object[] { new LocalDateTime(2099, 12, 31, 0, 0) }
            };
        }

        private static IEnumerable<object> GetNotLastDaysOfMonth()
        {
            return new[]
            {
                new object[] { new LocalDateTime(2017, 1, 28, 0, 0) },
                new object[] { new LocalDateTime(2017, 2, 26, 0, 0) },
                new object[] { new LocalDateTime(2016, 3, 1, 0, 0) },
                new object[] { new LocalDateTime(2016, 3, 15, 0, 0) },
                new object[] { new LocalDateTime(2016, 4, 1, 0, 0) },
                new object[] { new LocalDateTime(2016, 5, 23, 0, 0) },
                new object[] { new LocalDateTime(2016, 6, 12, 0, 0) },
                new object[] { new LocalDateTime(2016, 7, 4, 0, 0) },
                new object[] { new LocalDateTime(2016, 8, 15, 0, 0) },
                new object[] { new LocalDateTime(2016, 9, 11, 0, 0) },
                new object[] { new LocalDateTime(2016, 10, 4, 0, 0) },
                new object[] { new LocalDateTime(2016, 11, 25, 0, 0) },
                new object[] { new LocalDateTime(2016, 12, 17, 0, 0) },
            };
        }
    }
}
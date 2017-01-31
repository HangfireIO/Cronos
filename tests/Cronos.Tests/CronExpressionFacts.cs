using System;
using System.Collections.Generic;
using NodaTime;
using Xunit;

namespace Cronos.Tests
{
    public class CronExpressionFacts
    {
        private static readonly DateTimeZone America = DateTimeZoneProviders.Bcl.GetZoneOrNull("Eastern Standard Time");
        private static readonly DateTimeZone TimeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull("America/New_York");

        [Fact]
        public void BasicFact()
        {
            var expression = CronExpression.Parse("* * * * * ?");

            var dateTime = new LocalDateTime(2016, 03, 18, 12, 0, 0);
            var result = expression.Next(dateTime, dateTime.PlusYears(1));

            Assert.Equal(new LocalDateTime(2016, 03, 18, 12, 0, 0), result);
        }

        [Theory]

        // handle tabs.
        [InlineData("*	*	* * * ?")]

        // handle white spaces at the beginning and end of expression.
        [InlineData(" 	*	*	* * * ?    ")]
        public void HandleWhiteSpaces(string cronExpression)
        {
            var expression = CronExpression.Parse(cronExpression);

            var dateTime = new LocalDateTime(2016, 03, 18, 12, 0, 0);
            var result = expression.Next(dateTime, dateTime.PlusYears(1));

            Assert.Equal(new LocalDateTime(2016, 03, 18, 12, 0, 0), result);
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
        [InlineData("5- * * * * *")]
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
        [InlineData("* 7- * * * *")]
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
        [InlineData("* * 0- * * *")]
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
        [InlineData("* * * 8- * *")]
        [InlineData("* * * , * *")]
        [InlineData("* * * ,1 * *")]
        [InlineData("* * * / * *")]
        [InlineData("* * * # * *")]
        [InlineData("* * * *#3 * *")]
        [InlineData("* * * 4#1 * *")]
        [InlineData("* * * W * *")]
        [InlineData("* * * ?/2 * *")]

        //month is invalid

        [InlineData("* * * *  13 *")]
        [InlineData("* * * *  -1 *")]
        [InlineData("* * * *   - *")]
        [InlineData("* * * *  2- *")]
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
        [InlineData("* * * * * 3-")]
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
        [InlineData("* * * * * SU0")]
        [InlineData("* * * * * SUNDAY")]
        [InlineData("* * * * * L")]
        [InlineData("* * * * * W")]
        [InlineData("* * * * * LW")]
        public void Parse_ThrowAnException_WhenCronExpressionIsInvalid(string cronExpression)
        {
            var exception = Assert.Throws<ArgumentException>(() => CronExpression.Parse(cronExpression));

            Assert.Equal("cronExpression", exception.ParamName);
        }

        [Theory]
        [InlineData("? * * * * *")]
        [InlineData("* ? * * * *")]
        [InlineData("* * ? * * *")]
        [InlineData("* * * * ? *")]
        public void Parse_HandlesQuestionMarkCanBeSpecfiedOnlyForDayOfMonthOrDayOfWeek(string cronExpression)
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
        public void Next_ReturnsTheSameDateForAnyDate_When6StarsWerePassed(LocalDateTime dateTime)
        {
            var expression = CronExpression.Parse("* * * * * ?");

            var nextExecuted = expression.Next(dateTime.InZoneStrictly(America));

            Assert.Equal(dateTime.InZoneStrictly(America), nextExecuted);
        }

        [Theory]
        [InlineData("20 * * * * ?", "17:35:20", "17:35:20")]
        [InlineData("19,20,21 * * * * ?", "17:35:20", "17:35:20")]
        [InlineData("10-30 * * * * ?", "17:35:20", "17:35:20")]
        [InlineData("*/20 * * * * ?", "17:35:20", "17:35:20")]
        [InlineData("10 * * * * ?", "17:35:20", "17:36:10")]
        [InlineData("10,30 * * * * ?", "17:35:20", "17:35:30")]
        [InlineData("10-19 * * * * ?", "17:35:20", "17:36:10")]
        [InlineData("*/30 * * * * ?", "17:35:20", "17:35:30")]
        public void Next_ReturnsCorrectResult_WhenOnlySecondsAreSpecified(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var date = new LocalDate(2016, 12, 09);
            var nextExecuted = expression.Next(GetZonedDateTime(date, startTime));

            Assert.Equal(GetZonedDateTime(date, expectedTime), nextExecuted);
        }

        [Theory]
        [InlineData("59 * * * * ?", "17:35:55", "17:35:59")]
        [InlineData("40-59 * * * * ?", "17:59:59", "17:59:59")]
        [InlineData("1,59 * * * * ?", "23:00:58", "23:00:59")]
        public void Next_ReturnsCorrectResult_WhenMaxSecondsAreSpecified(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var date = new LocalDate(2016, 12, 09);
            var nextExecuted = expression.Next(GetZonedDateTime(date, startTime));

            Assert.Equal(GetZonedDateTime(date, expectedTime), nextExecuted);
        }

        [Theory]
        [InlineData("0 * * * * ?", "15:59:00", "15:59:00")]
        [InlineData("0-10 * * * * ?", "15:58:59", "15:59:00")]
        [InlineData("0,14 * * * * ?", "15:58:59", "15:59:00")]
        public void Next_ReturnsCorrectResult_WhenMinSecondsAreSpecified(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var date = new LocalDate(2016, 12, 09);
            var nextExecuted = expression.Next(GetZonedDateTime(date, startTime));

            Assert.Equal(GetZonedDateTime(date, expectedTime), nextExecuted);
        }

        [Theory]
        [InlineData("* 20 * * * ?", "15:59", "16:20")]
        [InlineData("* 19,20,21 * * * ?", "15:59", "16:19")]
        [InlineData("* 10-30 * * * ?", "15:59", "16:10")]
        [InlineData("* */20 * * * ?", "15:59", "16:00")]
        [InlineData("* 10 * * * ?", "15:59", "16:10")]
        [InlineData("* 10,30 * * * ?", "15:59", "16:10")]
        [InlineData("* 10-19 * * * ?", "16:15", "16:15")]
        [InlineData("* */30 * * * ?", "15:59", "16:00")]
        [InlineData("* */30 * * * ?", "16:01", "16:30")]
        public void Next_ReturnsCorrectResult_WhenOnlyMinutesAreSpecified(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var date = new LocalDate(2016, 12, 09);
            var nextExecuted = expression.Next(GetZonedDateTime(date, startTime));

            Assert.Equal(GetZonedDateTime(date, expectedTime), nextExecuted);
        }

        [Theory]
        [InlineData("* * 15 * * ?", "14:30", "15:00")]
        [InlineData("* * 14,15,16 * * ?", "14:30", "14:30")]
        [InlineData("* * 10-20 * * ?", "14:30", "14:30")]
        [InlineData("* * */5 * * ?", "14:30", "15:00")]
        [InlineData("* * 10 * * ?", "09:30", "10:00")]
        [InlineData("* * 10,20 * * ?", "14:30", "20:00")]
        [InlineData("* * 16-23 * * ?", "14:30", "16:00")]
        [InlineData("* * */20 * * ?", "14:30", "20:00")]
        public void Next_ReturnsCorrectResult_WhenOnlyHoursAreSpecified(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var date = new LocalDate(2016, 12, 09);
            var nextExecuted = expression.Next(GetZonedDateTime(date, startTime));

            Assert.Equal(GetZonedDateTime(date, expectedTime), nextExecuted);
        }

        [Theory]
        [InlineData("* * * 9 * ?", "2016/12/09", "2016/12/09")]
        [InlineData("* * * 09 * ?", "2016/12/09", "2016/12/09")]
        [InlineData("* * * 7,8,9 * ?", "2016/12/09", "2016/12/09")]
        [InlineData("* * * 5-10 * ?", "2016/12/09", "2016/12/09")]
        [InlineData("* * * */4 * ?", "2016/12/09", "2016/12/09")] // TODO: That's bad
        [InlineData("* * * 10 * ?", "2016/12/09", "2016/12/10")]
        [InlineData("* * * 10,20 * ?", "2016/12/09", "2016/12/10")]
        [InlineData("* * * 16-23 * ?", "2016/12/09", "2016/12/16")]
        [InlineData("* * * */3 * ?", "2016/12/09", "2016/12/10")] // TODO: That's bad
        public void Next_ReturnsCorrectResult_WhenOnlyDaysOfMonthAreSpecified(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var nextExecuted = expression.Next(GetZonedDateTime(startTime));

            Assert.Equal(GetZonedDateTime(expectedTime), nextExecuted);
        }

        [Theory]
        [InlineData("* * * ? 12 *", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? 3,5,12 *", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? 5-12 *", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? DEC *", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? mar-dec *", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? */4 *", "2016/12/09", "2017/01/01")] // TODO: That's very bad
        [InlineData("* * * ? 10 *", "2016/12/09", "2017/10/01")]
        [InlineData("* * * ? 10,11 *", "2016/12/09", "2017/10/01")]
        [InlineData("* * * ? 03-10 *", "2016/12/09", "2017/03/01")]
        [InlineData("* * * ? */3 *", "2016/12/09", "2017/01/01")] // TODO: That's very bad
        [InlineData("* * * ? */5 *", "2016/12/09", "2017/01/01")]
        [InlineData("* * * ? APR-NOV *", "2016/12/09", "2017/04/01")]
        public void Next_ReturnsCorrectResult_WhenOnlyMonthsAreSpecified(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var nextExecuted = expression.Next(GetZonedDateTime(startTime));

            Assert.Equal(GetZonedDateTime(expectedTime), nextExecuted);
        }

        // 2016/12/09 is friday.
        [Theory]
        [InlineData("* * * ? * 5", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? * 05", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? * 3,5,7", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? * 4-7", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? * FRI", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? * FRI/3", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? * thu-sat", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? * */5", "2016/12/09", "2016/12/09")]
        //[InlineData("* * * ? * thu-sun", "2016/12/09", "2016/12/09")] // TODO: that's bad
        [InlineData("* * * ? * 2", "2016/12/09", "2016/12/13")]
        [InlineData("* * * ? * 1,3", "2016/12/09", "2016/12/12")]
        [InlineData("* * * ? * 02-4", "2016/12/09", "2016/12/13")]
        [InlineData("* * * ? * */3", "2016/12/09", "2016/12/10")]
        [InlineData("* * * ? * thu/2", "2016/12/09", "2016/12/10")]
        [InlineData("* * * ? * mon-wed", "2016/12/09", "2016/12/12")]
        public void Next_ReturnsCorrectResult_WhenOnlyDaysOfWeekAreSpecified(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var nextExecuted = expression.Next(GetZonedDateTime(startTime));

            Assert.Equal(GetZonedDateTime(expectedTime), nextExecuted);
        }

        [Theory]
        [InlineData("54 47 17 09 12 5", "2016/12/01 00:00:00", "2016/12/09 17:47:54")]
        [InlineData("54 47 17 09 DEC FRI", "2016/12/01 00:00:00", "2016/12/09 17:47:54")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016/12/01 00:00:00", "2016/12/09 15:40:50")]
        public void Next_ReturnsTrue_WhenAllFieldsMatchTheSpecifiedDate(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var nextExecuted = expression.Next(GetZonedDateTime(startTime));

            Assert.Equal(GetZonedDateTime(expectedTime), nextExecuted);
        }

        [Theory]
        [InlineData("00 05 18 13 01 05", "2017/01/13 18:05")]
        [InlineData("00 05 18 13 *  05", "2017/01/13 18:05")]
        [InlineData("00 05 18 13 01 01", null)]
        [InlineData("00 05 18 01 01 05", null)]
        public void Next_HandlesSpecialCase_WhenBoth_DayOfWeek_And_DayOfMonth_WereSet(string cronExpression, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.Next(new LocalDate(2017, 01, 01).AtMidnight().InZoneStrictly(America));

            var expectedDateTime = expectedTime != null ? GetZonedDateTime(expectedTime) : (ZonedDateTime?)null;
            Assert.Equal(expectedDateTime, result);
        }

        [Theory]
        [InlineData("00 00 00 11 12 0")]
        [InlineData("00 00 00 11 12 7")]
        [InlineData("00 00 00 11 12 SUN")]
        [InlineData("00 00 00 11 12 sun")]
        public void Next_HandlesSpecialCase_ForSundays(string cronExpression)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.Next(new LocalDateTime(2016, 12, 11, 00, 00).InZoneStrictly(America));

            Assert.Equal(new LocalDateTime(2016, 12, 11, 00, 00).InZoneStrictly(America), result);
        }

        [Theory]
        [MemberData(nameof(GetLastDaysOfMonth))]
        public void Next_ReturnsTheSameDate_WhenLMarkInDayOfMonthMatchesTheSpecifiedDate(LocalDateTime dateTime)
        {
            var expression = CronExpression.Parse("* * * L * ?");

            var result = expression.Next(dateTime.InZoneStrictly(America));

            Assert.Equal(dateTime.InZoneStrictly(America), result);
        }

        [Theory]
        [MemberData(nameof(GetNotLastDaysOfMonth))]
        public void Next_Returns_WhenLMarkInDayOfMonthDoesNotMatchTheSpecifiedDate(LocalDateTime dateTime)
        {
            var expression = CronExpression.Parse("* * * L * ?");

            var result = expression.Next(dateTime.InZoneStrictly(America));

            Assert.NotEqual(dateTime.InZoneStrictly(America), result);
        }

        [Theory]
        [InlineData("* * * ? * SUN#1", "2017/1/1", "2017/1/1")]
        [InlineData("* * * ? * 0#1", "2017/1/1", "2017/1/1")]
        [InlineData("* * * ? * 0#2", "2017/1/1", "2017/1/8")]
        [InlineData("* * * ? * 0#2", "2017/1/1", "2017/1/8")]
        [InlineData("* * * ? * 5#3", "2017/1/1", "2017/1/20")]
        [InlineData("* * * ? * 3#2", "2017/1/1", "2017/1/11")]
        public void Next_ReturnsCorrectValue_WhenSharpIsUsedInDayOfWeek(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var result = expression.Next(GetZonedDateTime(startTime));

            Assert.Equal(GetZonedDateTime(expectedTime), result);
        }

        // TODO: StackOverflow exception. Next method should handle 'W' symbol in cron expression.

        //[Theory]
        //[InlineData("* * * 1W * ?", "2017/1/2", "2017/1/2")]
        //[InlineData("* * * 1W * ?", "2017/1/1", "2017/1/2")]
        //public void Next_ReturnsCorrectValue_WhenWIsUsedInDayOfMonth(string cronExpression, string startTime, string expectedTime)
        //{
        //    var expression = CronExpression.Parse(cronExpression);

        //    var result = expression.Next(GetZonedDateTime(startTime));

        //    Assert.Equal(result, GetZonedDateTime(expectedTime));
        //}

        [Fact]
        public void Next_ReturnsTheSameUtcDate_WhenCronExpressionHas6Stars()
        {
            var expression = CronExpression.Parse("* * * * * ?");
            var now = new LocalDateTime(2016, 12, 16, 00, 00, 00).InUtc();

            var result = expression.Next(now);

            Assert.Equal(now, result);
        }

        [Fact]
        public void Next_ReturnsCorrectUtcDate_WhenSecondAndMinuteFieldsAreSpecified()
        {
            var expression = CronExpression.Parse("00 05 * * * ?");
            var now = new LocalDateTime(2016, 12, 16, 00, 00, 00).InUtc();

            var result = expression.Next(now);

            Assert.Equal(now.Plus(Duration.FromMinutes(5)), result);
        }

        [Fact]
        public void Next_ReturnCorrectDate_WhenSecondAndMinuteAndHourFieldsAreSpecified()
        {
            var expression = CronExpression.Parse("00 30 02 * * ?");
            var now = new LocalDateTime(2016, 03, 13, 00, 00).InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(2016, 03, 13, 03, 00), result?.LocalDateTime);
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

        [Fact]
        public void Next_SkipMonth_IfDayOfMonthIsSpecified_And_MonthHasLessDays()
        {
            var expression = CronExpression.Parse("0 0 0 30 * ?");

            var nextExecuting = expression.Next(new LocalDateTime(2017, 2, 25, 0, 0).InZoneStrictly(America));

            Assert.Equal(new LocalDateTime(2017, 3, 30, 0, 0).InZoneStrictly(America), nextExecuting);
        }

        [Fact]
        public void Next_HandleLeapYear()
        {
            var expression = CronExpression.Parse("0 0 0 29 2 ?");
            var nextExecuting = expression.Next(new LocalDateTime(2017, 2, 25, 0, 0).InZoneStrictly(America));

            Assert.Equal(new LocalDateTime(2020, 2, 29, 0, 0).InZoneStrictly(America), nextExecuting);
        }

        [Theory]
        [MemberData(nameof(GetNotLastDaysOfMonth))]
        public void Next_ReturnsCorrectDate_WhenDayOfMonthIsSpecifiedAsLSymbol(LocalDateTime dateTime)
        {
            var expression = CronExpression.Parse("* * * L * ?");
            var now = dateTime.InZoneStrictly(TimeZone);

            var result = expression.Next(now);

            Assert.Equal(new LocalDateTime(now.Year, now.Month, now.Calendar.GetDaysInMonth(now.Year, now.Month), 00, 00), result?.LocalDateTime);
        }

        [Theory]
        [MemberData(nameof(GetLastDaysOfMonth))]
        public void Next_ReturnsCorrectDate_WhenDayOfMonthIsSpecifiedAsLSymbol2(LocalDateTime dateTime)
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
        public void Next_ReturnsCorrectValue_WhenDayOfWeekIsSpecifiedAsLSymbol(
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
        public void Next_ReturnsCorrectValue_WhenSharpIsUsedInDayOfWeek(
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
        [InlineData("0 */30 * * * ?", "01:45 ST", "03:00 DST")]

        // Skipped due to intervals, can be avoided by enumerating hours and minutes
        // "0,30 0-23/2 * * *"
        [InlineData("0 */30 */2 * * ?", "01:59 ST", "04:00 DST")]

        // Run missed, strict
        [InlineData("0 0,30 0-23/2 * * ?", "01:59 ST", "03:00 DST")]

        // TODO: may be confusing!
        // Skipped due to intervals, can be avoided by using "0,30 02 * * *"
        [InlineData("0 */30 2 * * ?", "01:59 ST", null)]

        // Run missed
        [InlineData("0 0,30 2 * * ?", "01:59 ST", "03:00 DST")]
        [InlineData("0 30 02 13 03 ?", "01:45 ST", "03:00 DST")]

        // Run missed, delay
        [InlineData("0 30 2 * * ?", "01:59 ST", "03:00 DST")]

        // Skipped due to intervals, "0 0-23/2 * * *" can be used to avoid skipping
        // TODO: differ from Linux Cron
        [InlineData("0 0 */2 * * ?", "01:59 ST", "03:00 DST")]

        // Run missed
        [InlineData("0 0 0-23/2 * * ?", "01:59 ST", "03:00 DST")]
        public void Next_HandleDST_WhenTheClockJumpsForward(string cronExpression, string startTime, string expectedTime)
        {
            // Arrange
            var expression = CronExpression.Parse(cronExpression);

            // 2016/03/13 is date when the clock jumps forward from 1:59 ST to 3:00 DST in America
            var date = new LocalDate(2016, 03, 13);
            var endDateTime = date.At(new LocalTime(23, 59, 59)).InZoneStrictly(America);

            // Act
            var executed = expression.Next(GetZonedDateTime(date, startTime));

            if (executed > endDateTime) executed = null;

            // Assert
            var expectedDateTime = expectedTime != null ? GetZonedDateTime(date, expectedTime) : (ZonedDateTime?)null;

            Assert.Equal(expectedDateTime, executed);
        }

        [Theory]

        [InlineData("0 */30 2 * * ?", "2016/03/12 23:59", "2016/03/14 02:00")]
        [InlineData("0 30 2 13 03 ?", "2016/03/13 23:59", "2017/03/13 02:30")]
        [InlineData("0 */30 2 13 3 *", "2016/03/13 23:59", "2017/03/13 02:00")]
        public void Next_HandleDST_WhenTheClockJumpsForward_AndResultIsOutTheGivenDay(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var executed = expression.Next(GetZonedDateTime(startTime));

            var expectedDateTime = expectedTime != null ? GetZonedDateTime(expectedTime) : (ZonedDateTime?)null;

            Assert.Equal(expectedDateTime, executed);
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
        [InlineData("0 0 1 * * ?", "01:00 ST", null)]

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
        [InlineData("0 0,30 1 * * ?", "01:00 ST", null)]

        // Duplicates skipped due to non-wildcard
        [InlineData("0 30 * * * ?", "01:30 DST", "01:30 DST")]
        [InlineData("0 30 * * * ?", "01:59 DST", "01:30 ST")]
        public void Next_HandleDST_WhenTheClockJumpsBackward(string cronExpression, string startTime, string expectedTime)
        {
            // Arrange
            var expression = CronExpression.Parse(cronExpression);

            var date = new LocalDate(2016, 11, 06);

            var endDateTime = date.At(new LocalTime(23, 59, 59)).InZoneStrictly(America);

            // Act
            var executed = expression.Next(GetZonedDateTime(date, startTime));

            if (executed > endDateTime) executed = null;

            // Assert
            var expectedDateTime = expectedTime != null ? GetZonedDateTime(date, expectedTime) : (ZonedDateTime?)null;
            Assert.Equal(expectedDateTime, executed);
        }

        private static ZonedDateTime GetZonedDateTime(LocalDate date, string timeString)
        {
            var timeAndDstMarker = timeString.Split(' ');
            var dstMarkerIncluded = timeAndDstMarker.Length > 1;

            var time = TimeSpan.Parse(timeAndDstMarker[0]);
            var localDateTime = date.At(new LocalTime(time.Hours, time.Minutes, time.Seconds));

            if (!dstMarkerIncluded) return localDateTime.InZoneStrictly(America);

            var isDst = timeAndDstMarker[1] == "DST";

            return isDst ?
                localDateTime.InZone(America, mapping => mapping.First()) :
                localDateTime.InZone(America, mapping => mapping.Last());
        }

        private static ZonedDateTime GetZonedDateTime(string dateTimeString)
        {
            var dateTime = DateTime.Parse(dateTimeString);

            var localDateTime = new LocalDateTime(
                dateTime.Year, 
                dateTime.Month, 
                dateTime.Day, 
                dateTime.Hour,
                dateTime.Minute,
                dateTime.Second);

            return localDateTime.InZoneStrictly(America);
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
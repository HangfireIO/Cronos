using System;
using System.Collections.Generic;
using NodaTime;
using Xunit;

namespace Cronos.Tests
{
    public class CronExpressionFacts
    {
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
            var exception = Assert.Throws<FormatException>(() => CronExpression.Parse("* * * * *"));
        }

        [Theory]
        [MemberData(nameof(GetRandomDates))]
        public void IsMatch_ReturnsTrueForAnyDate_When6StarsWerePassed(LocalDateTime dateTime)
        {
            var expression = CronExpression.Parse("* * * * * *");

            var result = expression.IsMatch(dateTime);

            Assert.True(result);
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
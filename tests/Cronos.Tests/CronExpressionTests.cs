using System;
using System.Collections.Generic;
using Xunit;

namespace Cronos.Tests
{
    public class CronExpressionTests
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

        [Theory]
        [MemberData(nameof(GetRandomDates))]
        public void IsMatch_ReturnsTrueForAnyDate_When5StarsWerePassed(DateTime dateTime)
        {
            var expression = CronExpression.Parse("* * * * *");

            var result = expression.IsMatch(dateTime);

            Assert.True(result);
        }

        private static IEnumerable<object> GetRandomDates()
        {
            return new[]
            {
                new object[] { new DateTime(2017, 01, 12, 16, 59, 50) },
                new object[] { new DateTime(2016, 03, 08, 11, 46, 15) },
                new object[] { new DateTime(2017, 04, 12, 16, 46, 0) },
                new object[] { new DateTime(2016, 2, 29, 02, 46, 0) },
                new object[] { new DateTime(2016, 12, 31, 16, 09, 0) },
                new object[] { new DateTime(2099, 12, 09, 16, 46, 57) },
                new object[] { new DateTime(1970, 01, 01, 0, 0, 0) },
                new object[] { DateTime.MinValue },
                new object[] { DateTime.MaxValue }
            };
        }
    }
}
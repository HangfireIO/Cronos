using System;
using Xunit;

namespace Cronos.Tests
{
    public class CronExpressionTests
    {
        [Fact]
        public void Parse_BasicTest()
        {
            var expression = CronExpression.Parse("* * * * *");

            var result = expression.IsMatch(new DateTime(2017, 01, 12, 12, 42, 58));

            Assert.True(result);
        }
    }
}
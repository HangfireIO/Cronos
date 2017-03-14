using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using Xunit;

namespace Cronos.Tests
{
    public class CronExpressionFacts
    {
        private static readonly bool IsUnix =
#if NETCOREAPP1_0
            !RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
#else
            Environment.OSVersion.Platform == PlatformID.MacOSX || Environment.OSVersion.Platform == PlatformID.Unix;
#endif
        private static readonly string EasternTimeZoneId = IsUnix ? "America/New_York" : "Eastern Standard Time";
        private static readonly string JordanTimeZoneId = IsUnix ? "Asia/Amman" : "Jordan Standard Time";

        private static readonly TimeZoneInfo EasternTimeZone = TimeZoneInfo.FindSystemTimeZoneById(EasternTimeZoneId);
        private static readonly TimeZoneInfo JordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById(JordanTimeZoneId);

        private static readonly DateTime Today = new DateTime(2016, 12, 09);

        [Theory]

        // Handle tabs.
        [InlineData("*	*	* * * *")]

        // Handle white spaces at the beginning and end of expression.
        [InlineData(" 	*	*	* * * *    ")]
        public void HandleWhiteSpaces(string cronExpression)
        {
            var a = new DateTime(2016, 03, 13, 01, 50, 00).AddSeconds(1);
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var startDateTimeOffset = new DateTimeOffset(2016, 03, 18, 12, 0, 0, TimeSpan.Zero);
            var endDateTimeOffset = new DateTimeOffset(2017, 03, 18, 12, 0, 0, TimeSpan.Zero);

            var result = expression.GetOccurrence(startDateTimeOffset, endDateTimeOffset, TimeZoneInfo.Utc);

            Assert.Equal(new DateTimeOffset(2016, 03, 18, 12, 0, 0, TimeSpan.Zero), result);
        }

        [Fact]
        public void Parse_ThrowAnException_WhenCronExpressionIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => CronExpression.Parse(null));

            Assert.Equal("expression", exception.ParamName);
        }

        [Fact]
        public void Parse_ThrowAnException_WhenCronExpressionIsEmpty()
        {
            var exception = Assert.Throws<ArgumentNullException>(() => CronExpression.Parse(""));

            Assert.Equal("expression", exception.ParamName);
        }

        [Theory]

        // Second field is invalid.

        [InlineData("-1   * * * * *")]
        [InlineData("-    * * * * *")]
        [InlineData("5-   * * * * *")]
        [InlineData(",    * * * * *")]
        [InlineData(",1   * * * * *")]
        [InlineData("/    * * * * *")]
        [InlineData("*/   * * * * *")]
        [InlineData("1/   * * * * *")]
        [InlineData("1/0  * * * * *")]
        [InlineData("1/60 * * * * *")]
        [InlineData("1/k  * * * * *")]
        [InlineData("1k   * * * * *")]
        [InlineData("#    * * * * *")]
        [InlineData("*#1  * * * * *")]
        [InlineData("0#2  * * * * *")]
        [InlineData("L    * * * * *")]
        [InlineData("W    * * * * *")]
        [InlineData("LW   * * * * *")]
        [InlineData("?    * * * * *")]

        // 2147483648 = Int32.MaxValue + 1

        [InlineData("1/2147483648 * * * * *")]

        // Minute field is invalid.

        [InlineData("* 60    * * * *")]
        [InlineData("* -1    * * * *")]
        [InlineData("* -     * * * *")]
        [InlineData("* 7-    * * * *")]
        [InlineData("* ,     * * * *")]
        [InlineData("* ,1    * * * *")]
        [InlineData("* */    * * * *")]
        [InlineData("* /     * * * *")]
        [InlineData("* 1/    * * * *")]
        [InlineData("* 1/0   * * * *")]
        [InlineData("* 1/60  * * * *")]
        [InlineData("* 1/k   * * * *")]
        [InlineData("* 1k    * * * *")]
        [InlineData("* #     * * * *")]
        [InlineData("* *#1   * * * *")]
        [InlineData("* 5#3   * * * *")]
        [InlineData("* L     * * * *")]
        [InlineData("* W     * * * *")]
        [InlineData("* LW    * * * *")]
        [InlineData("* ?     * * * *")]

        // Hour field is invalid.

        [InlineData("* * 25   * * *")]
        [InlineData("* * -1   * * *")]
        [InlineData("* * -    * * *")]
        [InlineData("* * 0-   * * *")]
        [InlineData("* * ,    * * *")]
        [InlineData("* * ,1   * * *")]
        [InlineData("* * /    * * *")]
        [InlineData("* * 1/   * * *")]
        [InlineData("* * 1/0  * * *")]
        [InlineData("* * 1/24 * * *")]
        [InlineData("* * 1/k  * * *")]
        [InlineData("* * 1k   * * *")]
        [InlineData("* * #    * * *")]
        [InlineData("* * *#2  * * *")]
        [InlineData("* * 10#1 * * *")]
        [InlineData("* * L    * * *")]
        [InlineData("* * W    * * *")]
        [InlineData("* * LW   * * *")]
        [InlineData("* * ?    * * *")]

        // Day of month field is invalid.

        [InlineData("* * * 32     *  *")]
        [InlineData("* * * 10-32  *  *")]
        [InlineData("* * * 31-32  *  *")]
        [InlineData("* * * -1     *  *")]
        [InlineData("* * * -      *  *")]
        [InlineData("* * * 8-     *  *")]
        [InlineData("* * * ,      *  *")]
        [InlineData("* * * ,1     *  *")]
        [InlineData("* * * /      *  *")]
        [InlineData("* * * 1/     *  *")]
        [InlineData("* * * 1/0    *  *")]
        [InlineData("* * * 1/32   *  *")]
        [InlineData("* * * 1/k    *  *")]
        [InlineData("* * * 1m     *  *")]
        [InlineData("* * * T      *  *")]
        [InlineData("* * * MON    *  *")]
        [InlineData("* * * #      *  *")]
        [InlineData("* * * *#3    *  *")]
        [InlineData("* * * 4#1    *  *")]
        [InlineData("* * * W      *  *")]
        [InlineData("* * * 1-2W   *  *")]
        [InlineData("* * * 1,2W   *  *")]
        [InlineData("* * * 1/2W   *  *")]
        [InlineData("* * * 1-2/2W *  *")]
        [InlineData("* * * 1LW    *  *")]
        [InlineData("* * * L-31   *  *")]
        [InlineData("* * * ?/2    *  *")]

        // Month field is invalid.

        [InlineData("* * * * 13   *")]
        [InlineData("* * * * -1   *")]
        [InlineData("* * * * -    *")]
        [InlineData("* * * * 2-   *")]
        [InlineData("* * * * ,    *")]
        [InlineData("* * * * ,1   *")]
        [InlineData("* * * * /    *")]
        [InlineData("* * * * */   *")]
        [InlineData("* * * * 1/   *")]
        [InlineData("* * * * 1/0  *")]
        [InlineData("* * * * 1/13 *")]
        [InlineData("* * * * 1/k  *")]
        [InlineData("* * * * 1k   *")]
        [InlineData("* * * * #    *")]
        [InlineData("* * * * *#1  *")]
        [InlineData("* * * * 2#2  *")]
        [InlineData("* * * * L    *")]
        [InlineData("* * * * W    *")]
        [InlineData("* * * * LW   *")]
        [InlineData("* * * * ?    *")]

        // Day of week field is invalid.

        [InlineData("* * * * * 8      ")]
        [InlineData("* * * * * -1     ")]
        [InlineData("* * * * * -      ")]
        [InlineData("* * * * * 3-     ")]
        [InlineData("* * * * * ,      ")]
        [InlineData("* * * * * ,1     ")]
        [InlineData("* * * * * /      ")]
        [InlineData("* * * * * */     ")]
        [InlineData("* * * * * 1/     ")]
        [InlineData("* * * * * 1/0    ")]
        [InlineData("* * * * * 1/8    ")]
        [InlineData("* * * * * #      ")]
        [InlineData("* * * * * 0#     ")]
        [InlineData("* * * * * 5#6    ")]
        [InlineData("* * * * * SUN#6  ")]
        [InlineData("* * * * * SUN#050")]
        [InlineData("* * * * * 0#0    ")]
        [InlineData("* * * * * SUT    ")]
        [InlineData("* * * * * SU0    ")]
        [InlineData("* * * * * SUNDAY ")]
        [InlineData("* * * * * L      ")]
        [InlineData("* * * * * W      ")]
        [InlineData("* * * * * LW     ")]

        // '?' can be specfied only for day of month or day of week.

        [InlineData("? * * * * *")]
        [InlineData("* ? * * * *")]
        [InlineData("* * ? * * *")]
        [InlineData("* * * * ? *")]
        [InlineData("* * * ? * ?")]

        // Fields count is invalid.

        [InlineData("* * * *")]
        [InlineData("* * * * * * *")]
        public void Parse_ThrowsAnException_WhenCronExpressionIsInvalid(string cronExpression)
        {
            Assert.Throws<FormatException>(() => CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds));
        }

        [Fact]
        public void GetOccurrence_ThrowsAnException_WhenDateTimeArgumentsHasAWrongKind()
        {
            var expression = CronExpression.Parse("* * * * *");

            var startException = Assert.Throws<ArgumentException>(() => expression.GetOccurrence(
                DateTime.Now,
                DateTime.UtcNow,
                TimeZoneInfo.Local));

            var endException = Assert.Throws<ArgumentException>(() => expression.GetOccurrence(
                DateTime.UtcNow,
                DateTime.Now,
                TimeZoneInfo.Local));

            Assert.Equal("utcStartInclusive", startException.ParamName);
            Assert.Equal("utcEndInclusive", endException.ParamName);
        }

        [Theory]

        // Basic facts.

        [InlineData("* * * * * *", "17:35:00", "17:35:00")]

        // Second specified.

        [InlineData("20    * * * * *", "17:35:00", "17:35:20")]
        [InlineData("20    * * * * *", "17:35:20", "17:35:20")]
        [InlineData("20    * * * * *", "17:35:40", "17:36:20")]
        [InlineData("10-30 * * * * *", "17:35:09", "17:35:10")]
        [InlineData("10-30 * * * * *", "17:35:10", "17:35:10")]
        [InlineData("10-30 * * * * *", "17:35:20", "17:35:20")]
        [InlineData("10-30 * * * * *", "17:35:30", "17:35:30")]
        [InlineData("10-30 * * * * *", "17:35:31", "17:36:10")]
        [InlineData("*/20  * * * * *", "17:35:00", "17:35:00")]
        [InlineData("*/20  * * * * *", "17:35:11", "17:35:20")]
        [InlineData("*/20  * * * * *", "17:35:20", "17:35:20")]
        [InlineData("*/20  * * * * *", "17:35:25", "17:35:40")]
        [InlineData("*/20  * * * * *", "17:35:59", "17:36:00")]
        [InlineData("10/5  * * * * *", "17:35:00", "17:35:10")]
        [InlineData("10/5  * * * * *", "17:35:12", "17:35:15")]
        [InlineData("10/5  * * * * *", "17:35:59", "17:36:10")]
        [InlineData("0     * * * * *", "17:35:59", "17:36:00")]
        [InlineData("0     * * * * *", "17:59:59", "18:00:00")]

        [InlineData("5-8,19,20,35-41 * * * * *", "17:35:01", "17:35:05")]
        [InlineData("5-8,19,20,35-41 * * * * *", "17:35:06", "17:35:06")]
        [InlineData("5-8,19,20,35-41 * * * * *", "17:35:18", "17:35:19")]
        [InlineData("5-8,19,20,35-41 * * * * *", "17:35:19", "17:35:19")]
        [InlineData("5-8,19,20,35-41 * * * * *", "17:35:20", "17:35:20")]
        [InlineData("5-8,19,20,35-41 * * * * *", "17:35:21", "17:35:35")]
        [InlineData("5-8,19,20,35-41 * * * * *", "17:35:36", "17:35:36")]
        [InlineData("5-8,19,20,35-41 * * * * *", "17:35:42", "17:36:05")]

        [InlineData("55-5 * * * * ?", "17:35:42", "17:35:55")]
        [InlineData("55-5 * * * * ?", "17:35:57", "17:35:57")]
        [InlineData("55-5 * * * * ?", "17:35:59", "17:35:59")]
        [InlineData("55-5 * * * * ?", "17:36:00", "17:36:00")]
        [InlineData("55-5 * * * * ?", "17:36:05", "17:36:05")]
        [InlineData("55-5 * * * * ?", "17:36:06", "17:36:55")]

        [InlineData("57-5/3 * * * * ?", "17:36:06", "17:36:57")]
        [InlineData("57-5/3 * * * * ?", "17:36:58", "17:37:00")]
        [InlineData("57-5/3 * * * * ?", "17:37:01", "17:37:03")]
        [InlineData("57-5/3 * * * * ?", "17:37:04", "17:37:57")]

        [InlineData("59-58 * * * * ?", "17:37:04", "17:37:04")]
        [InlineData("59-58 * * * * ?", "17:37:58", "17:37:58")]
        [InlineData("59-58 * * * * ?", "17:37:59", "17:37:59")]
        [InlineData("59-58 * * * * ?", "17:38:00", "17:38:00")]

        // Minute specified.

        [InlineData("* 12    * * * *", "15:05", "15:12")]
        [InlineData("* 12    * * * *", "15:12", "15:12")]
        [InlineData("* 12    * * * *", "15:59", "16:12")]
        [InlineData("* 31-39 * * * *", "15:00", "15:31")]
        [InlineData("* 31-39 * * * *", "15:30", "15:31")]
        [InlineData("* 31-39 * * * *", "15:31", "15:31")]
        [InlineData("* 31-39 * * * *", "15:39", "15:39")]
        [InlineData("* 31-39 * * * *", "15:59", "16:31")]
        [InlineData("* */20  * * * *", "15:00", "15:00")]
        [InlineData("* */20  * * * *", "15:10", "15:20")]
        [InlineData("* */20  * * * *", "15:59", "16:00")]
        [InlineData("* 10/5  * * * *", "15:00", "15:10")]
        [InlineData("* 10/5  * * * *", "15:14", "15:15")]
        [InlineData("* 10/5  * * * *", "15:59", "16:10")]
        [InlineData("* 0     * * * *", "15:59", "16:00")]

        [InlineData("* 5-8,19,20,35-41 * * * *", "15:01", "15:05")]
        [InlineData("* 5-8,19,20,35-41 * * * *", "15:06", "15:06")]
        [InlineData("* 5-8,19,20,35-41 * * * *", "15:18", "15:19")]
        [InlineData("* 5-8,19,20,35-41 * * * *", "15:19", "15:19")]
        [InlineData("* 5-8,19,20,35-41 * * * *", "15:20", "15:20")]
        [InlineData("* 5-8,19,20,35-41 * * * *", "15:21", "15:35")]
        [InlineData("* 5-8,19,20,35-41 * * * *", "15:36", "15:36")]
        [InlineData("* 5-8,19,20,35-41 * * * *", "15:42", "16:05")]

        [InlineData("* 51-4 * * * *", "17:35", "17:51")]
        [InlineData("* 51-4 * * * *", "17:51", "17:51")]
        [InlineData("* 51-4 * * * *", "17:55", "17:55")]
        [InlineData("* 51-4 * * * *", "17:59", "17:59")]
        [InlineData("* 51-4 * * * *", "18:00", "18:00")]
        [InlineData("* 51-4 * * * *", "18:04", "18:04")]
        [InlineData("* 51-4 * * * *", "18:05", "18:51")]

        [InlineData("* 56-4/4 * * * *", "17:55", "17:56")]
        [InlineData("* 56-4/4 * * * *", "17:57", "18:00")]
        [InlineData("* 56-4/4 * * * *", "18:01", "18:04")]
        [InlineData("* 56-4/4 * * * *", "18:05", "18:56")]

        [InlineData("* 45-44 * * * *", "18:45", "18:45")]
        [InlineData("* 45-44 * * * *", "18:55", "18:55")]
        [InlineData("* 45-44 * * * *", "18:59", "18:59")]
        [InlineData("* 45-44 * * * *", "19:00", "19:00")]
        [InlineData("* 45-44 * * * *", "19:44", "19:44")]

        // Hour specified.

        [InlineData("* * 11   * * *", "10:59", "11:00")]
        [InlineData("* * 11   * * *", "11:30", "11:30")]
        [InlineData("* * 3-22 * * *", "01:40", "03:00")]
        [InlineData("* * 3-22 * * *", "11:40", "11:40")]
        [InlineData("* * */2  * * *", "00:00", "00:00")]
        [InlineData("* * */2  * * *", "01:00", "02:00")]
        [InlineData("* * 4/5  * * *", "00:45", "04:00")]
        [InlineData("* * 4/5  * * *", "04:14", "04:14")]
        [InlineData("* * 4/5  * * *", "05:00", "09:00")]

        [InlineData("* * 3-5,10,11,13-17 * * *", "01:55", "03:00")]
        [InlineData("* * 3-5,10,11,13-17 * * *", "04:55", "04:55")]
        [InlineData("* * 3-5,10,11,13-17 * * *", "06:10", "10:00")]
        [InlineData("* * 3-5,10,11,13-17 * * *", "10:55", "10:55")]
        [InlineData("* * 3-5,10,11,13-17 * * *", "11:25", "11:25")]
        [InlineData("* * 3-5,10,11,13-17 * * *", "12:30", "13:00")]
        [InlineData("* * 3-5,10,11,13-17 * * *", "17:30", "17:30")]

        [InlineData("* * 23-3/2 * * *", "17:30", "23:00")]
        [InlineData("* * 23-3/2 * * *", "00:30", "01:00")]
        [InlineData("* * 23-3/2 * * *", "02:00", "03:00")]
        [InlineData("* * 23-3/2 * * *", "04:00", "23:00")]

        [InlineData("* * 23-22 * * *", "22:10", "22:10")]
        [InlineData("* * 23-22 * * *", "23:10", "23:10")]
        [InlineData("* * 23-22 * * *", "00:10", "00:10")]
        [InlineData("* * 23-22 * * *", "07:10", "07:10")]

        // Day of month specified.

        [InlineData("* * * 9     * *", "2016-11-01", "2016-11-09")]
        [InlineData("* * * 9     * *", "2016-11-09", "2016-11-09")]
        [InlineData("* * * 09    * *", "2016-11-10", "2016-12-09")]
        [InlineData("* * * */4   * *", "2016-12-01", "2016-12-01")]
        [InlineData("* * * */4   * *", "2016-12-02", "2016-12-05")]
        [InlineData("* * * */4   * *", "2016-12-06", "2016-12-09")]
        [InlineData("* * * */3   * *", "2016-12-02", "2016-12-04")]
        [InlineData("* * * 10,20 * *", "2016-12-09", "2016-12-10")]
        [InlineData("* * * 10,20 * *", "2016-12-12", "2016-12-20")]
        [InlineData("* * * 16-23 * *", "2016-12-01", "2016-12-16")]
        [InlineData("* * * 16-23 * *", "2016-12-16", "2016-12-16")]
        [InlineData("* * * 16-23 * *", "2016-12-18", "2016-12-18")]
        [InlineData("* * * 16-23 * *", "2016-12-23", "2016-12-23")]
        [InlineData("* * * 16-23 * *", "2016-12-24", "2017-01-16")]

        [InlineData("* * * 5-8,19,20,28-29 * *", "2016-12-01", "2016-12-05")]
        [InlineData("* * * 5-8,19,20,28-29 * *", "2016-12-05", "2016-12-05")]
        [InlineData("* * * 5-8,19,20,28-29 * *", "2016-12-06", "2016-12-06")]
        [InlineData("* * * 5-8,19,20,28-29 * *", "2016-12-08", "2016-12-08")]
        [InlineData("* * * 5-8,19,20,28-29 * *", "2016-12-09", "2016-12-19")]
        [InlineData("* * * 5-8,19,20,28-29 * *", "2016-12-20", "2016-12-20")]
        [InlineData("* * * 5-8,19,20,28-29 * *", "2016-12-21", "2016-12-28")]
        [InlineData("* * * 5-8,19,20,28-29 * *", "2016-12-30", "2017-01-05")]
        [InlineData("* * * 5-8,19,20,29-30 * *", "2017-02-27", "2017-03-05")]

        [InlineData("* * * 30-31 * *", "2016-02-27", "2016-03-30")]
        [InlineData("* * * 30-31 * *", "2017-02-27", "2017-03-30")]
        [InlineData("* * * 31    * *", "2017-04-27", "2017-05-31")]

        [InlineData("* * * 20-5/5 * *", "2017-05-19", "2017-05-20")]
        [InlineData("* * * 20-5/5 * *", "2017-05-21", "2017-05-25")]
        [InlineData("* * * 20-5/5 * *", "2017-05-26", "2017-05-30")]
        [InlineData("* * * 20-5/5 * *", "2017-06-01", "2017-06-04")]
        [InlineData("* * * 20-5/5 * *", "2017-06-05", "2017-06-20")]

        [InlineData("* * * 20-5/5 * *", "2017-07-01", "2017-07-04")]

        [InlineData("* * * 20-5/5 * *", "2018-02-26", "2018-03-04")]
        
        // Month specified.

        [InlineData("* * * * 11      *", "2016-10-09", "2016-11-01")]
        [InlineData("* * * * 11      *", "2016-11-02", "2016-11-02")]
        [InlineData("* * * * 11      *", "2016-12-02", "2017-11-01")]
        [InlineData("* * * * 3,9     *", "2016-01-09", "2016-03-01")]
        [InlineData("* * * * 3,9     *", "2016-06-09", "2016-09-01")]
        [InlineData("* * * * 3,9     *", "2016-10-09", "2017-03-01")]
        [InlineData("* * * * 5-11    *", "2016-01-01", "2016-05-01")]
        [InlineData("* * * * 5-11    *", "2016-05-07", "2016-05-07")]
        [InlineData("* * * * 5-11    *", "2016-07-12", "2016-07-12")]
        [InlineData("* * * * 05-11   *", "2016-12-13", "2017-05-01")]
        [InlineData("* * * * DEC     *", "2016-08-09", "2016-12-01")]
        [InlineData("* * * * mar-dec *", "2016-02-09", "2016-03-01")]
        [InlineData("* * * * mar-dec *", "2016-04-09", "2016-04-09")]
        [InlineData("* * * * mar-dec *", "2016-12-09", "2016-12-09")]
        [InlineData("* * * * */4     *", "2016-01-09", "2016-01-09")]
        [InlineData("* * * * */4     *", "2016-02-09", "2016-05-01")]
        [InlineData("* * * * */3     *", "2016-12-09", "2017-01-01")]
        [InlineData("* * * * */5     *", "2016-12-09", "2017-01-01")]
        [InlineData("* * * * APR-NOV *", "2016-12-09", "2017-04-01")]    

        [InlineData("* * * * 2-4,JUN,7,SEP-nov *", "2016-01-01", "2016-02-01")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov *", "2016-02-10", "2016-02-10")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov *", "2016-03-01", "2016-03-01")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov *", "2016-05-20", "2016-06-01")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov *", "2016-06-10", "2016-06-10")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov *", "2016-07-05", "2016-07-05")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov *", "2016-08-15", "2016-09-01")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov *", "2016-11-25", "2016-11-25")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov *", "2016-12-01", "2017-02-01")]

        [InlineData("* * * * 12-2 *", "2016-05-19", "2016-12-01")]
        [InlineData("* * * * 12-2 *", "2017-01-19", "2017-01-19")]
        [InlineData("* * * * 12-2 *", "2017-02-19", "2017-02-19")]
        [InlineData("* * * * 12-2 *", "2017-03-19", "2017-12-01")]

        [InlineData("* * * * 9-8/3 *", "2016-07-19", "2016-09-01")]
        [InlineData("* * * * 9-8/3 *", "2016-10-19", "2016-12-01")]
        [InlineData("* * * * 9-8/3 *", "2017-01-19", "2017-03-01")]
        [InlineData("* * * * 9-8/3 *", "2017-04-19", "2017-06-01")]

        // Day of week specified.

        // Monday        Tuesday       Wednesday     Thursday      Friday        Saturday      Sunday
        //                                           2016-12-01    2016-12-02    2016-12-03    2016-12-04
        // 2016-12-05    2016-12-06    2016-12-07    2016-12-08    2016-12-09    2016-12-10    2016-12-11
        // 2016-12-12    2016-12-13    2016-12-14    2016-12-15    2016-12-16    2016-12-17    2016-12-18

        [InlineData("* * * * * 5      ", "2016-12-07", "2016-12-09")]
        [InlineData("* * * * * 5      ", "2016-12-09", "2016-12-09")]
        [InlineData("* * * * * 05     ", "2016-12-10", "2016-12-16")]
        [InlineData("* * * * * 3,5,7  ", "2016-12-09", "2016-12-09")]
        [InlineData("* * * * * 3,5,7  ", "2016-12-10", "2016-12-11")]
        [InlineData("* * * * * 3,5,7  ", "2016-12-12", "2016-12-14")]
        [InlineData("* * * * * 4-7    ", "2016-12-08", "2016-12-08")]
        [InlineData("* * * * * 4-7    ", "2016-12-10", "2016-12-10")]
        [InlineData("* * * * * 4-7    ", "2016-12-11", "2016-12-11")]
        [InlineData("* * * * * 4-07   ", "2016-12-12", "2016-12-15")]
        [InlineData("* * * * * FRI    ", "2016-12-08", "2016-12-09")]
        [InlineData("* * * * * tue/2  ", "2016-12-09", "2016-12-10")]
        [InlineData("* * * * * tue/2  ", "2016-12-11", "2016-12-13")]
        [InlineData("* * * * * FRI/3  ", "2016-12-03", "2016-12-09")]
        [InlineData("* * * * * thu-sat", "2016-12-04", "2016-12-08")]
        [InlineData("* * * * * thu-sat", "2016-12-09", "2016-12-09")]
        [InlineData("* * * * * thu-sat", "2016-12-10", "2016-12-10")]
        [InlineData("* * * * * thu-sat", "2016-12-12", "2016-12-15")]
        [InlineData("* * * * * */5    ", "2016-12-08", "2016-12-09")]
        [InlineData("* * * * * */5    ", "2016-12-10", "2016-12-11")]
        [InlineData("* * * * * */5    ", "2016-12-12", "2016-12-16")]
        [InlineData("* * * ? * thu-sun", "2016-12-09", "2016-12-09")]

        [InlineData("* * * ? * sat-tue", "2016-12-10", "2016-12-10")]
        [InlineData("* * * ? * sat-tue", "2016-12-11", "2016-12-11")]
        [InlineData("* * * ? * sat-tue", "2016-12-12", "2016-12-12")]
        [InlineData("* * * ? * sat-tue", "2016-12-13", "2016-12-13")]
        [InlineData("* * * ? * sat-tue", "2016-12-14", "2016-12-17")]

        [InlineData("* * * ? * sat-tue/2", "2016-12-10", "2016-12-10")]
        [InlineData("* * * ? * sat-tue/2", "2016-12-11", "2016-12-12")]
        [InlineData("* * * ? * sat-tue/2", "2016-12-12", "2016-12-12")]
        [InlineData("* * * ? * sat-tue/2", "2016-12-13", "2016-12-17")]

        [InlineData("00 00 00 11 12 0  ", "2016-12-07", "2016-12-11")]
        [InlineData("00 00 00 11 12 7  ", "2016-12-09", "2016-12-11")]
        [InlineData("00 00 00 11 12 SUN", "2016-12-10", "2016-12-11")]
        [InlineData("00 00 00 11 12 sun", "2016-12-09", "2016-12-11")]

        // All fields are specified.

        [InlineData("54    47    17    09   12    5    ", "2016-10-01 00:00:00", "2016-12-09 17:47:54")]
        [InlineData("54    47    17    09   DEC   FRI  ", "2016-07-05 00:00:00", "2016-12-09 17:47:54")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016-12-01 00:00:00", "2016-12-09 15:40:50")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 15:40:53", "2016-12-09 15:40:53")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 15:40:57", "2016-12-09 15:41:50")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 15:45:56", "2016-12-09 15:45:56")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 15:51:56", "2016-12-09 16:40:50")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 21:50:56", "2016-12-10 15:40:50")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016-12-11 21:50:56", "2017-11-05 15:40:50")]

        // Friday the thirteenth.

        [InlineData("00    05    18    13   01    05   ", "2016-01-01 00:00:00", "2017-01-13 18:05:00")]
        [InlineData("00    05    18    13   *     05   ", "2016-01-01 00:00:00", "2016-05-13 18:05:00")]
        [InlineData("00    05    18    13   *     05   ", "2016-09-01 00:00:00", "2017-01-13 18:05:00")]
        [InlineData("00    05    18    13   *     05   ", "2017-02-01 00:00:00", "2017-10-13 18:05:00")]

        // Handle moving to next second, minute, hour, month, year.

        [InlineData("0 * * * * *", "2017-01-14 12:58:59", "2017-01-14 12:59:00")]

        [InlineData("0 0 * * * *", "2017-01-14 12:59", "2017-01-14 13:00")]
        [InlineData("0 0 0 * * *", "2017-01-14 23:00", "2017-01-15 00:00")]

        [InlineData("0 0 0 1 * *", "2016-02-10 00:00", "2016-03-01 00:00")]
        [InlineData("0 0 0 1 * *", "2017-02-10 00:00", "2017-03-01 00:00")]
        [InlineData("0 0 0 1 * *", "2017-04-10 00:00", "2017-05-01 00:00")]
        [InlineData("0 0 0 1 * *", "2017-01-30 00:00", "2017-02-01 00:00")]
        [InlineData("0 0 0 * * *", "2017-12-31 23:59", "2018-01-01 00:00")]

        // Skip month if day of month is specified and month has less days.

        [InlineData("0 0 0 30 * *", "2017-02-25 00:00", "2017-03-30 00:00")]
        [InlineData("0 0 0 31 * *", "2017-02-25 00:00", "2017-03-31 00:00")]
        [InlineData("0 0 0 31 * *", "2017-04-01 00:00", "2017-05-31 00:00")]

        // Leap year.

        [InlineData("0 0 0 29 2 *", "2016-03-10 00:00", "2020-02-29 00:00")]
        [InlineData("0 0 0 29 2 *", "2096-03-10 00:00", "2104-02-29 00:00")]

        // Support 'L' character in day of month field.

        [InlineData("* * * L * *","2016-01-05", "2016-01-31")]
        [InlineData("* * * L * *","2016-01-31", "2016-01-31")]
        [InlineData("* * * L * *","2016-02-05", "2016-02-29")]
        [InlineData("* * * L * *","2016-02-29", "2016-02-29")]
        [InlineData("* * * L 2 *","2016-02-29", "2016-02-29")]
        [InlineData("* * * L * *","2017-02-28", "2017-02-28")]
        [InlineData("* * * L * *","2100-02-05", "2100-02-28")]
        [InlineData("* * * L * *","2016-03-05", "2016-03-31")]
        [InlineData("* * * L * *","2016-03-31", "2016-03-31")]
        [InlineData("* * * L * *","2016-04-05", "2016-04-30")]
        [InlineData("* * * L * *","2016-04-30", "2016-04-30")]
        [InlineData("* * * L * *","2016-05-05", "2016-05-31")]
        [InlineData("* * * L * *","2016-05-31", "2016-05-31")]
        [InlineData("* * * L * *","2016-06-05", "2016-06-30")]
        [InlineData("* * * L * *","2016-06-30", "2016-06-30")]
        [InlineData("* * * L * *","2016-07-05", "2016-07-31")]
        [InlineData("* * * L * *","2016-07-31", "2016-07-31")]
        [InlineData("* * * L * *","2016-08-05", "2016-08-31")]
        [InlineData("* * * L * *","2016-08-31", "2016-08-31")]
        [InlineData("* * * L * *","2016-09-05", "2016-09-30")]
        [InlineData("* * * L * *","2016-09-30", "2016-09-30")]
        [InlineData("* * * L * *","2016-10-05", "2016-10-31")]
        [InlineData("* * * L * *","2016-10-31", "2016-10-31")]
        [InlineData("* * * L * *","2016-11-05", "2016-11-30")]
        [InlineData("* * * L * *","2016-12-05", "2016-12-31")]
        [InlineData("* * * L * *","2016-12-31", "2016-12-31")]
        [InlineData("* * * L * *","2099-12-05", "2099-12-31")]
        [InlineData("* * * L * *","2099-12-31", "2099-12-31")]

        [InlineData("* * * L-1 * *","2016-01-01", "2016-01-30")]
        [InlineData("* * * L-1 * *","2016-01-29", "2016-01-30")]
        [InlineData("* * * L-1 * *","2016-01-30", "2016-01-30")]
        [InlineData("* * * L-1 * *","2016-01-31", "2016-02-28")]
        [InlineData("* * * L-1 * *","2016-02-01", "2016-02-28")]
        [InlineData("* * * L-1 * *","2016-02-28", "2016-02-28")]
        [InlineData("* * * L-1 * *","2017-02-01", "2017-02-27")]
        [InlineData("* * * L-1 * *","2017-02-27", "2017-02-27")]
        [InlineData("* * * L-1 * *","2016-04-01", "2016-04-29")]
        [InlineData("* * * L-1 * *","2016-04-29", "2016-04-29")]
        [InlineData("* * * L-1 * *","2016-12-01", "2016-12-30")]

        [InlineData("* * * L-2 * *", "2016-01-05", "2016-01-29")]
        [InlineData("* * * L-2 * *", "2016-01-30", "2016-02-27")]
        [InlineData("* * * L-2 * *", "2016-02-01", "2016-02-27")]
        [InlineData("* * * L-2 * *", "2017-02-01", "2017-02-26")]
        [InlineData("* * * L-2 * *", "2016-04-01", "2016-04-28")]
        [InlineData("* * * L-2 * *", "2016-12-01", "2016-12-29")]
        [InlineData("* * * L-2 * *", "2016-12-29", "2016-12-29")]
        [InlineData("* * * L-2 * *", "2016-12-30", "2017-01-29")]

        [InlineData("* * * L-28 * *", "2016-01-01", "2016-01-03")]
        [InlineData("* * * L-28 * *", "2016-04-01", "2016-04-02")]
        [InlineData("* * * L-28 * *", "2016-02-01", "2016-02-01")]
        [InlineData("* * * L-28 * *", "2017-02-01", "2017-03-03")]

        [InlineData("* * * L-29 * *", "2016-01-01", "2016-01-02")]
        [InlineData("* * * L-29 * *", "2016-04-01", "2016-04-01")]
        [InlineData("* * * L-29 * *", "2016-02-01", "2016-03-02")]
        [InlineData("* * * L-29 * *", "2017-02-01", "2017-03-02")]

        [InlineData("* * * L-30 * *", "2016-01-01", "2016-01-01")]
        [InlineData("* * * L-30 * *", "2016-04-01", "2016-05-01")]
        [InlineData("* * * L-30 * *", "2016-02-01", "2016-03-01")]
        [InlineData("* * * L-30 * *", "2017-02-01", "2017-03-01")]

        // Support 'L' character in day of week field.

        // Monday        Tuesday       Wednesday     Thursday      Friday        Saturday      Sunday
        // 2016-01-23    2016-01-24    2016-01-25    2016-01-26    2016-01-27    2016-01-28    2016-01-29
        // 2016-01-30    2016-01-31

        [InlineData("* * * * * 0L", "2017-01-29", "2017-01-29")]
        [InlineData("* * * * * 0L", "2017-01-01", "2017-01-29")]
        [InlineData("* * * * * 1L", "2017-01-30", "2017-01-30")]
        [InlineData("* * * * * 1L", "2017-01-01", "2017-01-30")]
        [InlineData("* * * * * 2L", "2017-01-31", "2017-01-31")]
        [InlineData("* * * * * 2L", "2017-01-01", "2017-01-31")]
        [InlineData("* * * * * 3L", "2017-01-25", "2017-01-25")]
        [InlineData("* * * * * 3L", "2017-01-01", "2017-01-25")]
        [InlineData("* * * * * 4L", "2017-01-26", "2017-01-26")]
        [InlineData("* * * * * 4L", "2017-01-01", "2017-01-26")]
        [InlineData("* * * * * 5L", "2017-01-27", "2017-01-27")]
        [InlineData("* * * * * 5L", "2017-01-01", "2017-01-27")]
        [InlineData("* * * * * 6L", "2017-01-28", "2017-01-28")]
        [InlineData("* * * * * 6L", "2017-01-01", "2017-01-28")]
        [InlineData("* * * * * 7L", "2017-01-29", "2017-01-29")]
        [InlineData("* * * * * 7L", "2016-12-31", "2017-01-29")]

        // Support '#' in day of week field.

        [InlineData("* * * * * SUN#1", "2017-01-01", "2017-01-01")]
        [InlineData("* * * * * 0#1  ", "2017-01-01", "2017-01-01")]
        [InlineData("* * * * * 0#1  ", "2016-12-10", "2017-01-01")]
        [InlineData("* * * * * 0#1  ", "2017-02-01", "2017-02-05")]
        [InlineData("* * * * * 0#2  ", "2017-01-01", "2017-01-08")]
        [InlineData("* * * * * 0#2  ", "2017-01-08", "2017-01-08")]
        [InlineData("* * * * * 5#3  ", "2017-01-01", "2017-01-20")]
        [InlineData("* * * * * 5#3  ", "2017-01-21", "2017-02-17")]
        [InlineData("* * * * * 3#2  ", "2017-01-01", "2017-01-11")]
        [InlineData("* * * * * 2#5  ", "2017-02-01", "2017-05-30")]

        // Support 'W' in day of month field.

        [InlineData("* * * 1W * *", "2017-01-01", "2017-01-02")]
        [InlineData("* * * 2W * *", "2017-01-02", "2017-01-02")]
        [InlineData("* * * 6W * *", "2017-01-02", "2017-01-06")]
        [InlineData("* * * 7W * *", "2017-01-02", "2017-01-06")]
        [InlineData("* * * 7W * *", "2017-01-07", "2017-02-07")]
        [InlineData("* * * 8W * *", "2017-01-02", "2017-01-09")]

        [InlineData("* * * 30W * *", "2017-04-27", "2017-04-28")]
        [InlineData("* * * 30W * *", "2017-04-28", "2017-04-28")]
        [InlineData("* * * 30W * *", "2017-04-29", "2017-05-30")]

        [InlineData("* * * 1W * *", "2017-04-01", "2017-04-03")]

        [InlineData("0 30    17 7W * *", "2017-01-06 17:45", "2017-02-07 17:30")]
        [InlineData("0 30,45 17 7W * *", "2017-01-06 17:45", "2017-01-06 17:45")]
        [InlineData("0 30,55 17 7W * *", "2017-01-06 17:45", "2017-01-06 17:55")]

        [InlineData("0 30    17 30W * *", "2017-04-28 17:45", "2017-05-30 17:30")]
        [InlineData("0 30,45 17 30W * *", "2017-04-28 17:45", "2017-04-28 17:45")]
        [InlineData("0 30,55 17 30W * *", "2017-04-28 17:45", "2017-04-28 17:55")]

        [InlineData("0 30    17 30W * *", "2017-02-06 00:00", "2017-03-30 17:30")]

        [InlineData("0 30    17 31W * *", "2018-03-30 17:45", "2018-05-31 17:30")]
        [InlineData("0 30    17 15W * *", "2016-12-30 17:45", "2017-01-16 17:30")]

        // Support 'LW' in day of month field.

        [InlineData("* * * LW * *", "2017-01-01", "2017-01-31")]
        [InlineData("* * * LW * *", "2017-09-01", "2017-09-29")]
        [InlineData("* * * LW * *", "2017-09-29", "2017-09-29")]
        [InlineData("* * * LW * *", "2017-09-30", "2017-10-31")]
        [InlineData("* * * LW * *", "2017-04-01", "2017-04-28")]
        [InlineData("* * * LW * *", "2017-04-28", "2017-04-28")]
        [InlineData("* * * LW * *", "2017-04-29", "2017-05-31")]
        [InlineData("* * * LW * *", "2017-05-30", "2017-05-31")]

        [InlineData("0 30 17 LW * *", "2017-09-29 17:45", "2017-10-31 17:30")]

        [InlineData("* * * L-1W * *", "2017-01-01", "2017-01-30")]
        [InlineData("* * * L-2W * *", "2017-01-01", "2017-01-30")]
        [InlineData("* * * L-3W * *", "2017-01-01", "2017-01-27")]
        [InlineData("* * * L-4W * *", "2017-01-01", "2017-01-27")]

        [InlineData("* * * L-0W * *", "2016-02-01", "2016-02-29")]
        [InlineData("* * * L-0W * *", "2017-02-01", "2017-02-28")]
        [InlineData("* * * L-1W * *", "2016-02-01", "2016-02-29")]
        [InlineData("* * * L-1W * *", "2017-02-01", "2017-02-27")]
        [InlineData("* * * L-2W * *", "2016-02-01", "2016-02-26")]
        [InlineData("* * * L-2W * *", "2017-02-01", "2017-02-27")]
        [InlineData("* * * L-3W * *", "2016-02-01", "2016-02-26")]
        [InlineData("* * * L-3W * *", "2017-02-01", "2017-02-24")]

        // Support '?'.

        [InlineData("* * * ? 11 *", "2016-10-09", "2016-11-01")]

        [InlineData("* * * * * ?", "2016-12-09 16:46", "2016-12-09 16:46")]
        [InlineData("* * * ? * *", "2016-03-09 16:46", "2016-03-09 16:46")]
        [InlineData("* * * * * ?", "2016-12-30 16:46", "2016-12-30 16:46")]
        [InlineData("* * * ? * *", "2016-12-09 02:46", "2016-12-09 02:46")]
        [InlineData("* * * * * ?", "2016-12-09 16:09", "2016-12-09 16:09")]
        [InlineData("* * * ? * *", "2099-12-09 16:46", "2099-12-09 16:46")]

        // Last day of 400-year.
        [InlineData("* * * * * *", "2000-12-31 16:46", "2000-12-31 16:46")]
        public void GetOccurrence_ReturnsCorrectDate(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var startInstant = GetInstantFromLocalTime(startTime, EasternTimeZone);
            var endInstant = startInstant.AddYears(100);

            var occurrence = expression.GetOccurrence(startInstant, endInstant, EasternTimeZone);

            Assert.Equal(GetInstantFromLocalTime(expectedTime, EasternTimeZone), occurrence);
        }

        [Theory]

        // 2016-03-13 is date when the clock jumps forward from 1:59 am -05:00 standard time (ST) to 3:00 am -04:00 DST in Eastern Time Zone.
        // ________1:59 ST///invalid///3:00 DST________

        // Run missed.

        [InlineData("0 */30 *      *  *  *    ", "2016-03-13 01:45 -05:00", "2016-03-13 03:00 -04:00")]
        [InlineData("0 */30 */2    *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00")]
        [InlineData("0 1-58 */2    *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00")]
        [InlineData("0 0,30 0-23/2 *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00")]
        [InlineData("0 */30 2      *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00")]
        [InlineData("0 0,30 2      *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00")]
        [InlineData("0 */30 2      13 03 *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00")]
        [InlineData("0 0,30 02     13 03 *    ", "2016-03-13 01:45 -05:00", "2016-03-13 03:00 -04:00")]
        [InlineData("0 30   2      *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00")]
        [InlineData("0 0    */2    *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00")]
        [InlineData("0 30   0-23/2 *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00")]
                                                                                               
        [InlineData("0 0,59 *      *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 01:59 -05:00")]
        [InlineData("0 0,59 *      *  *  *    ", "2016-03-13 03:00 -04:00", "2016-03-13 03:00 -04:00")]
                                                                                               
        [InlineData("0 30   *      *  3  SUN#2", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00")]
        public void GetOccurrence_HandleDST_WhenTheClockJumpsForward_And_TimeZoneIsEst(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var startInstant = GetInstant(startTime);
            var endInstant = startInstant.AddYears(100);
            var expectedInstant = GetInstant(expectedTime);

            var executed = expression.GetOccurrence(startInstant, endInstant, EasternTimeZone);

            Assert.Equal(expectedInstant, executed);
            Assert.Equal(expectedInstant.Offset, executed?.Offset);
        }

        [Theory]

        // 2016-11-06 is date when the clock jumps backward from 2:00 am -04:00 DST to 1:00 am -05:00 ST in Eastern Time Zone.
        // _______1:00 DST____1:59 DST -> 1:00 ST____2:00 ST_______

        // Run at 2:00 ST because 2:00 DST is unreachable.
        [InlineData("0 */30 */2 * * *", "2016-11-06 01:30 -04:00", "2016-11-06 02:00 -05:00")]
        [InlineData("0 0    */2 * * *", "2016-11-06 00:30 -04:00", "2016-11-06 02:00 -05:00")]

        // Run twice due to intervals.
        [InlineData("0 */30 *   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00")]
        [InlineData("0 */30 *   * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -04:00")]
        [InlineData("0 */30 *   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:00 -05:00")]
        [InlineData("0 */30 *   * * *", "2016-11-06 01:15 -05:00", "2016-11-06 01:30 -05:00")]
                                    
        [InlineData("0 */30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00")]
        [InlineData("0 */30 1   * * *", "2016-11-06 01:20 -04:00", "2016-11-06 01:30 -04:00")]
        [InlineData("0 */30 1   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:00 -05:00")]
        [InlineData("0 */30 1   * * *", "2016-11-06 01:20 -05:00", "2016-11-06 01:30 -05:00")]
                                    
        [InlineData("0 30   *   * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -04:00")]
        [InlineData("0 30   *   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:30 -05:00")]

        // Duplicates skipped due to certain time.
        [InlineData("0 0,30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00")]
        [InlineData("0 0,30 1   * * *", "2016-11-06 01:20 -04:00", "2016-11-06 01:30 -04:00")]
        [InlineData("0 0,30 1   * * *", "2016-11-06 01:00 -05:00", "2016-11-07 01:00 -05:00")]

        [InlineData("0 0    1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00")]
        [InlineData("0 0    1   * * *", "2016-11-06 01:00 -05:00", "2016-11-07 01:00 -05:00")]

        [InlineData("0 0    1   6 11 *", "2015-11-07 01:00 -05:00", "2016-11-06 01:00 -04:00")]

        [InlineData("0 0    1   * 11 SUN#1", "2015-11-01 01:00 -05:00", "2016-11-06 01:00 -04:00")]
        public void GetOccurrence_HandleDST_WhenTheClockJumpsBackward(string cronExpression, string startTimeWithOffset, string expectedTimeWithOffset)
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var startInstant = GetInstant(startTimeWithOffset);
            var endInstant = startInstant.AddYears(100);
            var expectedInstant = GetInstant(expectedTimeWithOffset);

            var executed = expression.GetOccurrence(startInstant, endInstant, EasternTimeZone);

            Assert.Equal(expectedInstant, executed);
            Assert.Equal(expectedInstant.Offset, executed?.Offset);
        }

        [Theory]
        [InlineData("30 * * * *", "2016-11-06 00:50 -04:00", "2016-11-06 01:20 -04:00", null)]
        [InlineData("30 * * * *", "2016-11-06 00:50 -04:00", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -04:00")]
        [InlineData("30 * * * *", "2016-11-06 00:50 -04:00", "2016-11-06 01:20 -05:00", "2016-11-06 01:30 -04:00")]

        [InlineData("30 * * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:20 -04:00", null)]
        [InlineData("30 * * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -04:00")]
        [InlineData("30 * * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:20 -05:00", "2016-11-06 01:30 -04:00")]

        [InlineData("30 * * * *", "2016-11-06 01:40 -04:00", "2016-11-06 01:20 -05:00", null)]
        [InlineData("30 * * * *", "2016-11-06 01:40 -04:00", "2016-11-06 01:30 -05:00", "2016-11-06 01:30 -05:00")]
        [InlineData("30 * * * *", "2016-11-06 01:40 -04:00", "2016-11-06 01:40 -05:00", "2016-11-06 01:30 -05:00")]
        [InlineData("30 * * * *", "2016-11-06 01:20 -05:00", "2016-11-06 01:40 -05:00", "2016-11-06 01:30 -05:00")]
        public void GetOccurrence_ReturnsCorrectDate_WhenEndTimeIsOnDstTransition(string cronExpression, string startTime, string endTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var startInstant = GetInstant(startTime);
            var endInstant = GetInstant(endTime);

            var expextedInstant = expectedTime != null ? GetInstant(expectedTime) : (DateTimeOffset?)null;

            var occurrence = expression.GetOccurrence(startInstant, endInstant, EasternTimeZone);

            Assert.Equal(expextedInstant, occurrence);
        }

        [Fact]
        public void GetOccurrence_HandleBorderConditions_WhenDSTEnds()
        {
            var expression = CronExpression.Parse("59 59 01 * * *", CronFormat.IncludeSeconds);

            var startInstant = new DateTimeOffset(2016, 11, 06, 02, 00, 00, 00, TimeSpan.FromHours(-5)).AddTicks(-1);

            var executed = expression.GetOccurrence(startInstant, startInstant.AddYears(100), EasternTimeZone);

            Assert.Equal(new DateTimeOffset(2016, 11, 07, 01, 59, 59, 00, TimeSpan.FromHours(-5)), executed);
            Assert.Equal(TimeSpan.FromHours(-5), executed?.Offset);
        }

        [Theory]
        [InlineData("* * * * * *", "15:30", "15:30")]
        [InlineData("0 5 * * * *", "00:00", "00:05")]

        // Dst doesn't affect result.

        [InlineData("0 */30 * * * *", "2016-03-12 23:15", "2016-03-12 23:30")]
        [InlineData("0 */30 * * * *", "2016-03-12 23:45", "2016-03-13 00:00")]
        [InlineData("0 */30 * * * *", "2016-03-13 00:15", "2016-03-13 00:30")]
        [InlineData("0 */30 * * * *", "2016-03-13 00:45", "2016-03-13 01:00")]
        [InlineData("0 */30 * * * *", "2016-03-13 01:45", "2016-03-13 02:00")]
        [InlineData("0 */30 * * * *", "2016-03-13 02:15", "2016-03-13 02:30")]
        [InlineData("0 */30 * * * *", "2016-03-13 02:45", "2016-03-13 03:00")]
        [InlineData("0 */30 * * * *", "2016-03-13 03:15", "2016-03-13 03:30")]
        [InlineData("0 */30 * * * *", "2016-03-13 03:45", "2016-03-13 04:00")]

        [InlineData("0 */30 * * * *", "2016-11-05 23:10", "2016-11-05 23:30")]
        [InlineData("0 */30 * * * *", "2016-11-05 23:50", "2016-11-06 00:00")]
        [InlineData("0 */30 * * * *", "2016-11-06 00:10", "2016-11-06 00:30")]
        [InlineData("0 */30 * * * *", "2016-11-06 00:50", "2016-11-06 01:00")]
        [InlineData("0 */30 * * * *", "2016-11-06 01:10", "2016-11-06 01:30")]
        [InlineData("0 */30 * * * *", "2016-11-06 01:50", "2016-11-06 02:00")]
        [InlineData("0 */30 * * * *", "2016-11-06 02:10", "2016-11-06 02:30")]
        [InlineData("0 */30 * * * *", "2016-11-06 02:50", "2016-11-06 03:00")]
        [InlineData("0 */30 * * * *", "2016-11-06 03:10", "2016-11-06 03:30")]
        [InlineData("0 */30 * * * *", "2016-11-06 03:50", "2016-11-06 04:00")]
        public void GetOccurrence_ReturnsCorrectUtcDate(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var startInstant = GetInstantFromLocalTime(startTime, TimeZoneInfo.Utc);
            var endInstant = startInstant.AddYears(100);
            var expectedInstant = GetInstantFromLocalTime(expectedTime, TimeZoneInfo.Utc);

            var occurrence = expression.GetOccurrence(startInstant, endInstant, TimeZoneInfo.Utc);

            Assert.Equal(expectedInstant, occurrence);
            Assert.Equal(expectedInstant.Offset, occurrence?.Offset);
        }

        [Fact]
        public void GetOccurrence_ReturnsNull_WhenArgsAreDateTime_And_NextOccurrenceIsBeyondEndTime()
        {
            var expression = CronExpression.Parse("* * * 4 *");

            var startInstant = new DateTime(2017, 03, 13, 0, 0, 0, DateTimeKind.Utc);
            var endInstant = new DateTime(2017, 03, 30, 0, 0, 0, DateTimeKind.Utc);

            var occurrence = expression.GetOccurrence(startInstant, endInstant, TimeZoneInfo.Utc);

            Assert.Equal(null, occurrence);
        }

        [Fact]
        public void GetOccurrence_ReturnsNull_WhenArgsAreDateTimeOffset_And_NextOccurrenceIsBeyondEndTime()
        {
            var expression = CronExpression.Parse("* * * 4 *");

            var startInstant = new DateTimeOffset(2017, 03, 13, 0, 0, 0, TimeSpan.Zero);
            var endInstant = new DateTimeOffset(2017, 03, 30, 0, 0, 0, TimeSpan.Zero);

            var occurrence = expression.GetOccurrence(startInstant, endInstant, TimeZoneInfo.Utc);

            Assert.Equal(null, occurrence);
        }

        [Theory]
        [InlineData("30 0 L  * *", "2017-03-30 23:59 +02:00", "2017-03-31 01:00 +03:00")]
        [InlineData("30 0 L  * *", "2017-03-31 01:00 +03:00", "2017-04-30 00:30 +03:00")]
        [InlineData("30 0 LW * *", "2018-03-29 23:59 +02:00", "2018-03-30 01:00 +03:00")]
        [InlineData("30 0 LW * *", "2018-03-30 01:00 +03:00", "2018-04-30 00:30 +03:00")]
        public void GetOccurrence_HandleDifficultDSTCases_WhenTheClockJumpsForwardOnFriday(string cronExpression, string startTimeWithOffset, string expectedTimeWithOffset)
        {
            var expression = CronExpression.Parse(cronExpression);

            var startInstant = GetInstant(startTimeWithOffset);
            var endInstant = startInstant.AddYears(100);
            var expectedInstant = GetInstant(expectedTimeWithOffset);

            var executed = expression.GetOccurrence(startInstant, endInstant, JordanTimeZone);

            // TODO: Rounding error.
            if (executed?.Millisecond == 999)
            {
                executed = executed.Value.AddMilliseconds(1);
            }

            Assert.Equal(expectedInstant, executed);
            Assert.Equal(expectedInstant.Offset, executed?.Offset);
        }

        [Theory]
        [InlineData("30 0 L  * *", "2014-10-31 00:30 +02:00", "2014-11-30 00:30 +02:00")]
        [InlineData("30 0 L  * *", "2014-10-31 00:30 +03:00", "2014-10-31 00:30 +03:00")]
        [InlineData("30 0 LW * *", "2015-10-30 00:30 +02:00", "2015-11-30 00:30 +02:00")]
        [InlineData("30 0 LW * *", "2015-10-30 00:30 +03:00", "2015-10-30 00:30 +03:00")]
        public void GetOccurrence_HandleDifficultDSTCases_WhenTheClockJumpsBackwardOnFriday(string cronExpression, string startTimeWithOffset, string expectedTimeWithOffset)
        {
            var expression = CronExpression.Parse(cronExpression);

            var startInstant = GetInstant(startTimeWithOffset);
            var endInstant = startInstant.AddYears(100);
            var expectedInstant = GetInstant(expectedTimeWithOffset);

            var executed = expression.GetOccurrence(startInstant, endInstant, JordanTimeZone);

            Assert.Equal(expectedInstant, executed);
            Assert.Equal(expectedInstant.Offset, executed?.Offset);
        }

        [Theory]
        [MemberData(nameof(GetTimeZones))]
        public void GetOccurrence_ReturnsTheSameDateTimeWithGivenTimeZoneOffset(TimeZoneInfo zone)
        {
            var expression = CronExpression.Parse("* * * * *");

            var startInstant = new DateTimeOffset(2017, 03, 04, 00, 00, 00, new TimeSpan(12, 30, 00));
            var endInstant = new DateTimeOffset(2019, 03, 04, 00, 00, 00, new TimeSpan(-12, 30, 00));
            var expectedInstant = startInstant;

            var expectedOffset = zone.GetUtcOffset(expectedInstant);

            var executed = expression.GetOccurrence(startInstant, endInstant, zone);

            Assert.Equal(expectedInstant, executed);
            Assert.Equal(expectedOffset, executed?.Offset);
        }

        [Theory]
        [MemberData(nameof(GetTimeZones))]
        public void GetOccurrence_ReturnsDateTimeWithUtcKind_WhenUsingDateTimeArguments(TimeZoneInfo zone)
        {
            var expression = CronExpression.Parse("* * * * *");

            var startInstant = new DateTime(2017, 03, 06, 00, 00, 00, DateTimeKind.Utc);
            var endInstant = DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc);

            var executed = expression.GetOccurrence(startInstant, endInstant, zone);

            Assert.Equal(startInstant, executed);
            Assert.Equal(DateTimeKind.Utc, executed.Value.Kind);
        }

        [Theory]

        [InlineData("* * 30    2    *    ", "1970-01-01")]
        [InlineData("* * 30-31 2    *    ", "1970-01-01")]
        [InlineData("* * 31    2    *    ", "1970-01-01")]
        [InlineData("* * 31    4    *    ", "1970-01-01")]
        [InlineData("* * 31    6    *    ", "1970-01-01")]
        [InlineData("* * 31    9    *    ", "1970-01-01")]
        [InlineData("* * 31    11   *    ", "1970-01-01")]
        [InlineData("* * L-30  11   *    ", "1970-01-01")]
        [InlineData("* * L-29  2    *    ", "1970-01-01")]
        [InlineData("* * L-30  2    *    ", "1970-01-01")]

        [InlineData("* * 1     *    SUN#2", "1970-01-01")]
        [InlineData("* * 7     *    SUN#2", "1970-01-01")]
        [InlineData("* * 1     *    SUN#3", "1970-01-01")]
        [InlineData("* * 14    *    SUN#3", "1970-01-01")]
        [InlineData("* * 1     *    SUN#4", "1970-01-01")]
        [InlineData("* * 21    *    SUN#4", "1970-01-01")]
        [InlineData("* * 1     *    SUN#5", "1970-01-01")]
        [InlineData("* * 28    *    SUN#5", "1970-01-01")]
                                    
        [InlineData("* * 8     *    MON#1", "1970-01-01")]
        [InlineData("* * 31    *    MON#1", "1970-01-01")]
        [InlineData("* * 15    *    TUE#2", "1970-01-01")]
        [InlineData("* * 31    *    TUE#2", "1970-01-01")]
        [InlineData("* * 22    *    WED#3", "1970-01-01")]
        [InlineData("* * 31    *    WED#3", "1970-01-01")]
        [InlineData("* * 29    *    THU#4", "1970-01-01")]
        [InlineData("* * 31    *    THU#4", "1970-01-01")]
                                    
        [InlineData("* * 21    *    7L   ", "1970-01-01")]
        [InlineData("* * 21    *    0L   ", "1970-01-01")]
        [InlineData("* * 11    *    0L   ", "1970-01-01")]
        [InlineData("* * 1     *    0L   ", "1970-01-01")]
                                    
        [InlineData("* * L     *    SUN#1", "1970-01-01")]
        [InlineData("* * L     *    SUN#2", "1970-01-01")]
        [InlineData("* * L     *    SUN#3", "1970-01-01")]
        [InlineData("* * L     1    SUN#4", "1970-01-01")]
        [InlineData("* * L     3-12 SUN#4", "1970-01-01")]
                               
        [InlineData("* * L-1   2    SUN#5", "1970-01-01")]
        [InlineData("* * L-2   4    SUN#5", "1970-01-01")]
        [InlineData("* * L-3   *    SUN#5", "1970-01-01")]
        [InlineData("* * L-10  *    SUN#4", "1970-01-01")]
                               
        [InlineData("* * 1W    *    SUN  ", "1970-01-01")]
        [InlineData("* * 4W    *    0    ", "1970-01-01")]
        [InlineData("* * 7W    *    7    ", "1970-01-01")]
        [InlineData("* * 5W    *    SAT  ", "1970-01-01")]
                               
        [InlineData("* * 14W   *    6#2  ", "1970-01-01")]
                               
        [InlineData("* * 7W    *    FRI#2", "1970-01-01")]
        [InlineData("* * 14W   *    TUE#3", "1970-01-01")]
        [InlineData("* * 11W   *    MON#3", "1970-01-01")]
        [InlineData("* * 21W   *    TUE#4", "1970-01-01")]
        [InlineData("* * 28W   *    SAT#5", "1970-01-01")]
                               
        [InlineData("* * 21W   *    0L   ", "1970-01-01")]
        [InlineData("* * 19W   *    1L   ", "1970-01-01")]
        [InlineData("* * 1W    *    1L   ", "1970-01-01")]
        [InlineData("* * 21W   *    2L   ", "1970-01-01")]
        [InlineData("* * 2W    *    2L   ", "1970-01-01")]
        [InlineData("* * 21W   *    3L   ", "1970-01-01")]
        [InlineData("* * 3W    *    3L   ", "1970-01-01")]
        [InlineData("* * 21W   *    4L   ", "1970-01-01")]
        [InlineData("* * 4W    *    4L   ", "1970-01-01")]
        [InlineData("* * 21W   *    5L   ", "1970-01-01")]
        [InlineData("* * 5W    *    5L   ", "1970-01-01")]
        [InlineData("* * 21W   *    6L   ", "1970-01-01")]
        [InlineData("* * 21W   *    7L   ", "1970-01-01")]
                               
        [InlineData("* * LW    *    SUN  ", "1970-01-01")]
        [InlineData("* * LW    *    0    ", "1970-01-01")]
        [InlineData("* * LW    *    0L   ", "1970-01-01")]
        [InlineData("* * LW    *    SAT  ", "1970-01-01")]
        [InlineData("* * LW    *    6    ", "1970-01-01")]
        [InlineData("* * LW    *    6L   ", "1970-01-01")]
                               
        [InlineData("* * LW    *    1#1  ", "1970-01-01")]
        [InlineData("* * LW    *    2#2  ", "1970-01-01")]
        [InlineData("* * LW    *    3#3  ", "1970-01-01")]
        [InlineData("* * LW    1    4#4  ", "1970-01-01")]
        [InlineData("* * LW    3-12 4#4  ", "1970-01-01")]
        public void GetOccurrence_ReturnNull_WhenCronExpressionIsUnreachable(string cronExpression, string startTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var startInstant = GetInstantFromLocalTime(startTime, EasternTimeZone);
            var endInstant = startInstant.AddYears(200);

            var occurrence = expression.GetOccurrence(startInstant, endInstant, EasternTimeZone);

            Assert.Null(occurrence);
        }

        [Theory]

        // Basic facts.

        [InlineData("* * * * *", "17:35", "17:35")]

        [InlineData("* * * * *", "17:35:01", "17:36:00")]
        [InlineData("* * * * *", "17:35:59", "17:36:00")]
        [InlineData("* * * * *", "17:36:00", "17:36:00")]

        // Minute specified.

        [InlineData("12    * * * *", "15:05", "15:12")]
        [InlineData("12    * * * *", "15:12", "15:12")]
        [InlineData("12    * * * *", "15:59", "16:12")]
        [InlineData("31-39 * * * *", "15:00", "15:31")]
        [InlineData("31-39 * * * *", "15:30", "15:31")]
        [InlineData("31-39 * * * *", "15:31", "15:31")]
        [InlineData("31-39 * * * *", "15:39", "15:39")]
        [InlineData("31-39 * * * *", "15:59", "16:31")]
        [InlineData("*/20  * * * *", "15:00", "15:00")]
        [InlineData("*/20  * * * *", "15:10", "15:20")]
        [InlineData("*/20  * * * *", "15:59", "16:00")]
        [InlineData("10/5  * * * *", "15:00", "15:10")]
        [InlineData("10/5  * * * *", "15:14", "15:15")]
        [InlineData("10/5  * * * *", "15:59", "16:10")]
        [InlineData("0     * * * *", "15:59", "16:00")]

        [InlineData("44 * * * *", "19:44:01", "20:44:00")]
        [InlineData("44 * * * *", "19:44:30", "20:44:00")]
        [InlineData("44 * * * *", "19:44:59", "20:44:00")]
        [InlineData("44 * * * *", "19:45:00", "20:44:00")]

        [InlineData("5-8,19,20,35-41 * * * *", "15:01", "15:05")]
        [InlineData("5-8,19,20,35-41 * * * *", "15:06", "15:06")]
        [InlineData("5-8,19,20,35-41 * * * *", "15:18", "15:19")]
        [InlineData("5-8,19,20,35-41 * * * *", "15:19", "15:19")]
        [InlineData("5-8,19,20,35-41 * * * *", "15:20", "15:20")]
        [InlineData("5-8,19,20,35-41 * * * *", "15:21", "15:35")]
        [InlineData("5-8,19,20,35-41 * * * *", "15:36", "15:36")]
        [InlineData("5-8,19,20,35-41 * * * *", "15:42", "16:05")]

        [InlineData("51-4 * * * *", "17:35", "17:51")]
        [InlineData("51-4 * * * *", "17:51", "17:51")]
        [InlineData("51-4 * * * *", "17:55", "17:55")]
        [InlineData("51-4 * * * *", "17:59", "17:59")]
        [InlineData("51-4 * * * *", "18:00", "18:00")]
        [InlineData("51-4 * * * *", "18:04", "18:04")]
        [InlineData("51-4 * * * *", "18:05", "18:51")]

        [InlineData("56-4/4 * * * *", "17:55", "17:56")]
        [InlineData("56-4/4 * * * *", "17:57", "18:00")]
        [InlineData("56-4/4 * * * *", "18:01", "18:04")]
        [InlineData("56-4/4 * * * *", "18:05", "18:56")]

        [InlineData("45-44 * * * *", "18:45", "18:45")]
        [InlineData("45-44 * * * *", "18:55", "18:55")]
        [InlineData("45-44 * * * *", "18:59", "18:59")]
        [InlineData("45-44 * * * *", "19:00", "19:00")]
        [InlineData("45-44 * * * *", "19:44", "19:44")]

        // Hour specified.

        [InlineData("* 11   * * *", "10:59", "11:00")]
        [InlineData("* 11   * * *", "11:30", "11:30")]
        [InlineData("* 3-22 * * *", "01:40", "03:00")]
        [InlineData("* 3-22 * * *", "11:40", "11:40")]
        [InlineData("* */2  * * *", "00:00", "00:00")]
        [InlineData("* */2  * * *", "01:00", "02:00")]
        [InlineData("* 4/5  * * *", "00:45", "04:00")]
        [InlineData("* 4/5  * * *", "04:14", "04:14")]
        [InlineData("* 4/5  * * *", "05:00", "09:00")]

        [InlineData("* 3-5,10,11,13-17 * * *", "01:55", "03:00")]
        [InlineData("* 3-5,10,11,13-17 * * *", "04:55", "04:55")]
        [InlineData("* 3-5,10,11,13-17 * * *", "06:10", "10:00")]
        [InlineData("* 3-5,10,11,13-17 * * *", "10:55", "10:55")]
        [InlineData("* 3-5,10,11,13-17 * * *", "11:25", "11:25")]
        [InlineData("* 3-5,10,11,13-17 * * *", "12:30", "13:00")]
        [InlineData("* 3-5,10,11,13-17 * * *", "17:30", "17:30")]

        [InlineData("* 23-3/2 * * *", "17:30", "23:00")]
        [InlineData("* 23-3/2 * * *", "00:30", "01:00")]
        [InlineData("* 23-3/2 * * *", "02:00", "03:00")]
        [InlineData("* 23-3/2 * * *", "04:00", "23:00")]

        [InlineData("* 23-22 * * *", "22:10", "22:10")]
        [InlineData("* 23-22 * * *", "23:10", "23:10")]
        [InlineData("* 23-22 * * *", "00:10", "00:10")]
        [InlineData("* 23-22 * * *", "07:10", "07:10")]

        // Day of month specified.

        [InlineData("* * 9     * *", "2016-11-01", "2016-11-09")]
        [InlineData("* * 9     * *", "2016-11-09", "2016-11-09")]
        [InlineData("* * 09    * *", "2016-11-10", "2016-12-09")]
        [InlineData("* * */4   * *", "2016-12-01", "2016-12-01")]
        [InlineData("* * */4   * *", "2016-12-02", "2016-12-05")]
        [InlineData("* * */4   * *", "2016-12-06", "2016-12-09")]
        [InlineData("* * */3   * *", "2016-12-02", "2016-12-04")]
        [InlineData("* * 10,20 * *", "2016-12-09", "2016-12-10")]
        [InlineData("* * 10,20 * *", "2016-12-12", "2016-12-20")]
        [InlineData("* * 16-23 * *", "2016-12-01", "2016-12-16")]
        [InlineData("* * 16-23 * *", "2016-12-16", "2016-12-16")]
        [InlineData("* * 16-23 * *", "2016-12-18", "2016-12-18")]
        [InlineData("* * 16-23 * *", "2016-12-23", "2016-12-23")]
        [InlineData("* * 16-23 * *", "2016-12-24", "2017-01-16")]

        [InlineData("* * 5-8,19,20,28-29 * *", "2016-12-01", "2016-12-05")]
        [InlineData("* * 5-8,19,20,28-29 * *", "2016-12-05", "2016-12-05")]
        [InlineData("* * 5-8,19,20,28-29 * *", "2016-12-06", "2016-12-06")]
        [InlineData("* * 5-8,19,20,28-29 * *", "2016-12-08", "2016-12-08")]
        [InlineData("* * 5-8,19,20,28-29 * *", "2016-12-09", "2016-12-19")]
        [InlineData("* * 5-8,19,20,28-29 * *", "2016-12-20", "2016-12-20")]
        [InlineData("* * 5-8,19,20,28-29 * *", "2016-12-21", "2016-12-28")]
        [InlineData("* * 5-8,19,20,28-29 * *", "2016-12-30", "2017-01-05")]
        [InlineData("* * 5-8,19,20,29-30 * *", "2017-02-27", "2017-03-05")]

        [InlineData("* * 30-31 * *", "2016-02-27", "2016-03-30")]
        [InlineData("* * 30-31 * *", "2017-02-27", "2017-03-30")]
        [InlineData("* * 31    * *", "2017-04-27", "2017-05-31")]

        [InlineData("* * 20-5/5 * *", "2017-05-19", "2017-05-20")]
        [InlineData("* * 20-5/5 * *", "2017-05-21", "2017-05-25")]
        [InlineData("* * 20-5/5 * *", "2017-05-26", "2017-05-30")]
        [InlineData("* * 20-5/5 * *", "2017-06-01", "2017-06-04")]
        [InlineData("* * 20-5/5 * *", "2017-06-05", "2017-06-20")]

        [InlineData("* * 20-5/5 * *", "2017-07-01", "2017-07-04")]

        [InlineData("* * 20-5/5 * *", "2018-02-26", "2018-03-04")]

        // Month specified.

        [InlineData("* * * 11      *", "2016-10-09", "2016-11-01")]
        [InlineData("* * * 11      *", "2016-11-02", "2016-11-02")]
        [InlineData("* * * 11      *", "2016-12-02", "2017-11-01")]
        [InlineData("* * * 3,9     *", "2016-01-09", "2016-03-01")]
        [InlineData("* * * 3,9     *", "2016-06-09", "2016-09-01")]
        [InlineData("* * * 3,9     *", "2016-10-09", "2017-03-01")]
        [InlineData("* * * 5-11    *", "2016-01-01", "2016-05-01")]
        [InlineData("* * * 5-11    *", "2016-05-07", "2016-05-07")]
        [InlineData("* * * 5-11    *", "2016-07-12", "2016-07-12")]
        [InlineData("* * * 05-11   *", "2016-12-13", "2017-05-01")]
        [InlineData("* * * DEC     *", "2016-08-09", "2016-12-01")]
        [InlineData("* * * mar-dec *", "2016-02-09", "2016-03-01")]
        [InlineData("* * * mar-dec *", "2016-04-09", "2016-04-09")]
        [InlineData("* * * mar-dec *", "2016-12-09", "2016-12-09")]
        [InlineData("* * * */4     *", "2016-01-09", "2016-01-09")]
        [InlineData("* * * */4     *", "2016-02-09", "2016-05-01")]
        [InlineData("* * * */3     *", "2016-12-09", "2017-01-01")]
        [InlineData("* * * */5     *", "2016-12-09", "2017-01-01")]
        [InlineData("* * * APR-NOV *", "2016-12-09", "2017-04-01")]

        [InlineData("* * * 2-4,JUN,7,SEP-nov *", "2016-01-01", "2016-02-01")]
        [InlineData("* * * 2-4,JUN,7,SEP-nov *", "2016-02-10", "2016-02-10")]
        [InlineData("* * * 2-4,JUN,7,SEP-nov *", "2016-03-01", "2016-03-01")]
        [InlineData("* * * 2-4,JUN,7,SEP-nov *", "2016-05-20", "2016-06-01")]
        [InlineData("* * * 2-4,JUN,7,SEP-nov *", "2016-06-10", "2016-06-10")]
        [InlineData("* * * 2-4,JUN,7,SEP-nov *", "2016-07-05", "2016-07-05")]
        [InlineData("* * * 2-4,JUN,7,SEP-nov *", "2016-08-15", "2016-09-01")]
        [InlineData("* * * 2-4,JUN,7,SEP-nov *", "2016-11-25", "2016-11-25")]
        [InlineData("* * * 2-4,JUN,7,SEP-nov *", "2016-12-01", "2017-02-01")]

        [InlineData("* * * 12-2 *", "2016-05-19", "2016-12-01")]
        [InlineData("* * * 12-2 *", "2017-01-19", "2017-01-19")]
        [InlineData("* * * 12-2 *", "2017-02-19", "2017-02-19")]
        [InlineData("* * * 12-2 *", "2017-03-19", "2017-12-01")]

        [InlineData("* * * 9-8/3 *", "2016-07-19", "2016-09-01")]
        [InlineData("* * * 9-8/3 *", "2016-10-19", "2016-12-01")]
        [InlineData("* * * 9-8/3 *", "2017-01-19", "2017-03-01")]
        [InlineData("* * * 9-8/3 *", "2017-04-19", "2017-06-01")]

        // Day of week specified.

        // Monday        Tuesday       Wednesday     Thursday      Friday        Saturday      Sunday
        //                                           2016-12-01    2016-12-02    2016-12-03    2016-12-04
        // 2016-12-05    2016-12-06    2016-12-07    2016-12-08    2016-12-09    2016-12-10    2016-12-11
        // 2016-12-12    2016-12-13    2016-12-14    2016-12-15    2016-12-16    2016-12-17    2016-12-18

        [InlineData("* * * * 5      ", "2016-12-07", "2016-12-09")]
        [InlineData("* * * * 5      ", "2016-12-09", "2016-12-09")]
        [InlineData("* * * * 05     ", "2016-12-10", "2016-12-16")]
        [InlineData("* * * * 3,5,7  ", "2016-12-09", "2016-12-09")]
        [InlineData("* * * * 3,5,7  ", "2016-12-10", "2016-12-11")]
        [InlineData("* * * * 3,5,7  ", "2016-12-12", "2016-12-14")]
        [InlineData("* * * * 4-7    ", "2016-12-08", "2016-12-08")]
        [InlineData("* * * * 4-7    ", "2016-12-10", "2016-12-10")]
        [InlineData("* * * * 4-7    ", "2016-12-11", "2016-12-11")]
        [InlineData("* * * * 4-07   ", "2016-12-12", "2016-12-15")]
        [InlineData("* * * * FRI    ", "2016-12-08", "2016-12-09")]
        [InlineData("* * * * tue/2  ", "2016-12-09", "2016-12-10")]
        [InlineData("* * * * tue/2  ", "2016-12-11", "2016-12-13")]
        [InlineData("* * * * FRI/3  ", "2016-12-03", "2016-12-09")]
        [InlineData("* * * * thu-sat", "2016-12-04", "2016-12-08")]
        [InlineData("* * * * thu-sat", "2016-12-09", "2016-12-09")]
        [InlineData("* * * * thu-sat", "2016-12-10", "2016-12-10")]
        [InlineData("* * * * thu-sat", "2016-12-12", "2016-12-15")]
        [InlineData("* * * * */5    ", "2016-12-08", "2016-12-09")]
        [InlineData("* * * * */5    ", "2016-12-10", "2016-12-11")]
        [InlineData("* * * * */5    ", "2016-12-12", "2016-12-16")]
        [InlineData("* * ? * thu-sun", "2016-12-09", "2016-12-09")]

        [InlineData("* * ? * sat-tue", "2016-12-10", "2016-12-10")]
        [InlineData("* * ? * sat-tue", "2016-12-11", "2016-12-11")]
        [InlineData("* * ? * sat-tue", "2016-12-12", "2016-12-12")]
        [InlineData("* * ? * sat-tue", "2016-12-13", "2016-12-13")]
        [InlineData("* * ? * sat-tue", "2016-12-14", "2016-12-17")]

        [InlineData("* * ? * sat-tue/2", "2016-12-10", "2016-12-10")]
        [InlineData("* * ? * sat-tue/2", "2016-12-11", "2016-12-12")]
        [InlineData("* * ? * sat-tue/2", "2016-12-12", "2016-12-12")]
        [InlineData("* * ? * sat-tue/2", "2016-12-13", "2016-12-17")]

        [InlineData("00 00 11 12 0  ", "2016-12-07", "2016-12-11")]
        [InlineData("00 00 11 12 7  ", "2016-12-09", "2016-12-11")]
        [InlineData("00 00 11 12 SUN", "2016-12-10", "2016-12-11")]
        [InlineData("00 00 11 12 sun", "2016-12-09", "2016-12-11")]

        // All fields are specified.

        [InlineData("47    17    09   12    5    ", "2016-10-01 00:00", "2016-12-09 17:47")]
        [InlineData("47    17    09   DEC   FRI  ", "2016-07-05 00:00", "2016-12-09 17:47")]
        [InlineData("40-50 15-20 5-10 11,12 5,6,7", "2016-12-01 00:00", "2016-12-09 15:40")]
        [InlineData("40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 15:40", "2016-12-09 15:40")]
        [InlineData("40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 15:45", "2016-12-09 15:45")]
        [InlineData("40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 15:51", "2016-12-09 16:40")]
        [InlineData("40-50 15-20 5-10 11,12 5,6,7", "2016-12-09 21:50", "2016-12-10 15:40")]
        [InlineData("40-50 15-20 5-10 11,12 5,6,7", "2016-12-11 21:50", "2017-11-05 15:40")]

        // Friday the thirteenth.

        [InlineData("05    18    13   01    05   ", "2016-01-01 00:00", "2017-01-13 18:05")]
        [InlineData("05    18    13   *     05   ", "2016-01-01 00:00", "2016-05-13 18:05")]
        [InlineData("05    18    13   *     05   ", "2016-09-01 00:00", "2017-01-13 18:05")]
        [InlineData("05    18    13   *     05   ", "2017-02-01 00:00", "2017-10-13 18:05")]

        // Handle moving to next second, minute, hour, month, year.

        [InlineData("0 * * * *", "2017-01-14 12:59", "2017-01-14 13:00")]
        [InlineData("0 0 * * *", "2017-01-14 23:00", "2017-01-15 00:00")]

        [InlineData("0 0 1 * *", "2016-02-10 00:00", "2016-03-01 00:00")]
        [InlineData("0 0 1 * *", "2017-02-10 00:00", "2017-03-01 00:00")]
        [InlineData("0 0 1 * *", "2017-04-10 00:00", "2017-05-01 00:00")]
        [InlineData("0 0 1 * *", "2017-01-30 00:00", "2017-02-01 00:00")]
        [InlineData("0 0 * * *", "2017-12-31 23:59", "2018-01-01 00:00")]

        // Skip month if day of month is specified and month has less days.

        [InlineData("0 0 30 * *", "2017-02-25 00:00", "2017-03-30 00:00")]
        [InlineData("0 0 31 * *", "2017-02-25 00:00", "2017-03-31 00:00")]
        [InlineData("0 0 31 * *", "2017-04-01 00:00", "2017-05-31 00:00")]

        // Leap year.

        [InlineData("0 0 29 2 *", "2016-03-10 00:00", "2020-02-29 00:00")]
        [InlineData("0 0 29 2 *", "2096-03-10 00:00", "2104-02-29 00:00")]

        // Support 'L' character in day of month field.

        [InlineData("* * L * *", "2016-01-05", "2016-01-31")]
        [InlineData("* * L * *", "2016-01-31", "2016-01-31")]
        [InlineData("* * L * *", "2016-02-05", "2016-02-29")]
        [InlineData("* * L * *", "2016-02-29", "2016-02-29")]
        [InlineData("* * L 2 *", "2016-02-29", "2016-02-29")]
        [InlineData("* * L * *", "2017-02-28", "2017-02-28")]
        [InlineData("* * L * *", "2100-02-05", "2100-02-28")]
        [InlineData("* * L * *", "2016-03-05", "2016-03-31")]
        [InlineData("* * L * *", "2016-03-31", "2016-03-31")]
        [InlineData("* * L * *", "2016-04-05", "2016-04-30")]
        [InlineData("* * L * *", "2016-04-30", "2016-04-30")]
        [InlineData("* * L * *", "2016-05-05", "2016-05-31")]
        [InlineData("* * L * *", "2016-05-31", "2016-05-31")]
        [InlineData("* * L * *", "2016-06-05", "2016-06-30")]
        [InlineData("* * L * *", "2016-06-30", "2016-06-30")]
        [InlineData("* * L * *", "2016-07-05", "2016-07-31")]
        [InlineData("* * L * *", "2016-07-31", "2016-07-31")]
        [InlineData("* * L * *", "2016-08-05", "2016-08-31")]
        [InlineData("* * L * *", "2016-08-31", "2016-08-31")]
        [InlineData("* * L * *", "2016-09-05", "2016-09-30")]
        [InlineData("* * L * *", "2016-09-30", "2016-09-30")]
        [InlineData("* * L * *", "2016-10-05", "2016-10-31")]
        [InlineData("* * L * *", "2016-10-31", "2016-10-31")]
        [InlineData("* * L * *", "2016-11-05", "2016-11-30")]
        [InlineData("* * L * *", "2016-12-05", "2016-12-31")]
        [InlineData("* * L * *", "2016-12-31", "2016-12-31")]
        [InlineData("* * L * *", "2099-12-05", "2099-12-31")]
        [InlineData("* * L * *", "2099-12-31", "2099-12-31")]

        [InlineData("* * L-1 * *", "2016-01-01", "2016-01-30")]
        [InlineData("* * L-1 * *", "2016-01-29", "2016-01-30")]
        [InlineData("* * L-1 * *", "2016-01-30", "2016-01-30")]
        [InlineData("* * L-1 * *", "2016-01-31", "2016-02-28")]
        [InlineData("* * L-1 * *", "2016-02-01", "2016-02-28")]
        [InlineData("* * L-1 * *", "2016-02-28", "2016-02-28")]
        [InlineData("* * L-1 * *", "2017-02-01", "2017-02-27")]
        [InlineData("* * L-1 * *", "2017-02-27", "2017-02-27")]
        [InlineData("* * L-1 * *", "2016-04-01", "2016-04-29")]
        [InlineData("* * L-1 * *", "2016-04-29", "2016-04-29")]
        [InlineData("* * L-1 * *", "2016-12-01", "2016-12-30")]

        [InlineData("* * L-2 * *", "2016-01-05", "2016-01-29")]
        [InlineData("* * L-2 * *", "2016-01-30", "2016-02-27")]
        [InlineData("* * L-2 * *", "2016-02-01", "2016-02-27")]
        [InlineData("* * L-2 * *", "2017-02-01", "2017-02-26")]
        [InlineData("* * L-2 * *", "2016-04-01", "2016-04-28")]
        [InlineData("* * L-2 * *", "2016-12-01", "2016-12-29")]
        [InlineData("* * L-2 * *", "2016-12-29", "2016-12-29")]
        [InlineData("* * L-2 * *", "2016-12-30", "2017-01-29")]

        [InlineData("* * L-28 * *", "2016-01-01", "2016-01-03")]
        [InlineData("* * L-28 * *", "2016-04-01", "2016-04-02")]
        [InlineData("* * L-28 * *", "2016-02-01", "2016-02-01")]
        [InlineData("* * L-28 * *", "2017-02-01", "2017-03-03")]

        [InlineData("* * L-29 * *", "2016-01-01", "2016-01-02")]
        [InlineData("* * L-29 * *", "2016-04-01", "2016-04-01")]
        [InlineData("* * L-29 * *", "2016-02-01", "2016-03-02")]
        [InlineData("* * L-29 * *", "2017-02-01", "2017-03-02")]

        [InlineData("* * L-30 * *", "2016-01-01", "2016-01-01")]
        [InlineData("* * L-30 * *", "2016-04-01", "2016-05-01")]
        [InlineData("* * L-30 * *", "2016-02-01", "2016-03-01")]
        [InlineData("* * L-30 * *", "2017-02-01", "2017-03-01")]

        // Support 'L' character in day of week field.

        // Monday        Tuesday       Wednesday     Thursday      Friday        Saturday      Sunday
        // 2016-01-23    2016-01-24    2016-01-25    2016-01-26    2016-01-27    2016-01-28    2016-01-29
        // 2016-01-30    2016-01-31

        [InlineData("* * * * 0L", "2017-01-29", "2017-01-29")]
        [InlineData("* * * * 0L", "2017-01-01", "2017-01-29")]
        [InlineData("* * * * 1L", "2017-01-30", "2017-01-30")]
        [InlineData("* * * * 1L", "2017-01-01", "2017-01-30")]
        [InlineData("* * * * 2L", "2017-01-31", "2017-01-31")]
        [InlineData("* * * * 2L", "2017-01-01", "2017-01-31")]
        [InlineData("* * * * 3L", "2017-01-25", "2017-01-25")]
        [InlineData("* * * * 3L", "2017-01-01", "2017-01-25")]
        [InlineData("* * * * 4L", "2017-01-26", "2017-01-26")]
        [InlineData("* * * * 4L", "2017-01-01", "2017-01-26")]
        [InlineData("* * * * 5L", "2017-01-27", "2017-01-27")]
        [InlineData("* * * * 5L", "2017-01-01", "2017-01-27")]
        [InlineData("* * * * 6L", "2017-01-28", "2017-01-28")]
        [InlineData("* * * * 6L", "2017-01-01", "2017-01-28")]
        [InlineData("* * * * 7L", "2017-01-29", "2017-01-29")]
        [InlineData("* * * * 7L", "2016-12-31", "2017-01-29")]

        // Support '#' in day of week field.

        [InlineData("* * * * SUN#1", "2017-01-01", "2017-01-01")]
        [InlineData("* * * * 0#1  ", "2017-01-01", "2017-01-01")]
        [InlineData("* * * * 0#1  ", "2016-12-10", "2017-01-01")]
        [InlineData("* * * * 0#1  ", "2017-02-01", "2017-02-05")]
        [InlineData("* * * * 0#2  ", "2017-01-01", "2017-01-08")]
        [InlineData("* * * * 0#2  ", "2017-01-08", "2017-01-08")]
        [InlineData("* * * * 5#3  ", "2017-01-01", "2017-01-20")]
        [InlineData("* * * * 5#3  ", "2017-01-21", "2017-02-17")]
        [InlineData("* * * * 3#2  ", "2017-01-01", "2017-01-11")]
        [InlineData("* * * * 2#5  ", "2017-02-01", "2017-05-30")]

        // Support 'W' in day of month field.

        [InlineData("* * 1W * *", "2017-01-01", "2017-01-02")]
        [InlineData("* * 2W * *", "2017-01-02", "2017-01-02")]
        [InlineData("* * 6W * *", "2017-01-02", "2017-01-06")]
        [InlineData("* * 7W * *", "2017-01-02", "2017-01-06")]
        [InlineData("* * 7W * *", "2017-01-07", "2017-02-07")]
        [InlineData("* * 8W * *", "2017-01-02", "2017-01-09")]

        [InlineData("* * 30W * *", "2017-04-27", "2017-04-28")]
        [InlineData("* * 30W * *", "2017-04-28", "2017-04-28")]
        [InlineData("* * 30W * *", "2017-04-29", "2017-05-30")]

        [InlineData("* * 1W * *", "2017-04-01", "2017-04-03")]

        [InlineData("30    17 7W * *", "2017-01-06 17:45", "2017-02-07 17:30")]
        [InlineData("30,45 17 7W * *", "2017-01-06 17:45", "2017-01-06 17:45")]
        [InlineData("30,55 17 7W * *", "2017-01-06 17:45", "2017-01-06 17:55")]

        [InlineData("30    17 30W * *", "2017-04-28 17:45", "2017-05-30 17:30")]
        [InlineData("30,45 17 30W * *", "2017-04-28 17:45", "2017-04-28 17:45")]
        [InlineData("30,55 17 30W * *", "2017-04-28 17:45", "2017-04-28 17:55")]

        [InlineData("30    17 30W * *", "2017-02-06 00:00", "2017-03-30 17:30")]

        [InlineData("30    17 31W * *", "2018-03-30 17:45", "2018-05-31 17:30")]
        [InlineData("30    17 15W * *", "2016-12-30 17:45", "2017-01-16 17:30")]

        // Support 'LW' in day of month field.

        [InlineData("* * LW * *", "2017-01-01", "2017-01-31")]
        [InlineData("* * LW * *", "2017-09-01", "2017-09-29")]
        [InlineData("* * LW * *", "2017-09-29", "2017-09-29")]
        [InlineData("* * LW * *", "2017-09-30", "2017-10-31")]
        [InlineData("* * LW * *", "2017-04-01", "2017-04-28")]
        [InlineData("* * LW * *", "2017-04-28", "2017-04-28")]
        [InlineData("* * LW * *", "2017-04-29", "2017-05-31")]
        [InlineData("* * LW * *", "2017-05-30", "2017-05-31")]

        [InlineData("30 17 LW * *", "2017-09-29 17:45", "2017-10-31 17:30")]

        [InlineData("* * L-1W * *", "2017-01-01", "2017-01-30")]
        [InlineData("* * L-2W * *", "2017-01-01", "2017-01-30")]
        [InlineData("* * L-3W * *", "2017-01-01", "2017-01-27")]
        [InlineData("* * L-4W * *", "2017-01-01", "2017-01-27")]

        [InlineData("* * L-0W * *", "2016-02-01", "2016-02-29")]
        [InlineData("* * L-0W * *", "2017-02-01", "2017-02-28")]
        [InlineData("* * L-1W * *", "2016-02-01", "2016-02-29")]
        [InlineData("* * L-1W * *", "2017-02-01", "2017-02-27")]
        [InlineData("* * L-2W * *", "2016-02-01", "2016-02-26")]
        [InlineData("* * L-2W * *", "2017-02-01", "2017-02-27")]
        [InlineData("* * L-3W * *", "2016-02-01", "2016-02-26")]
        [InlineData("* * L-3W * *", "2017-02-01", "2017-02-24")]

        // Support '?'.

        [InlineData("* * ? 11 *", "2016-10-09", "2016-11-01")]

        [InlineData("* * * * ?", "2016-12-09 16:46", "2016-12-09 16:46")]
        [InlineData("* * ? * *", "2016-03-09 16:46", "2016-03-09 16:46")]
        [InlineData("* * * * ?", "2016-12-30 16:46", "2016-12-30 16:46")]
        [InlineData("* * ? * *", "2016-12-09 02:46", "2016-12-09 02:46")]
        [InlineData("* * * * ?", "2016-12-09 16:09", "2016-12-09 16:09")]
        [InlineData("* * ? * *", "2099-12-09 16:46", "2099-12-09 16:46")]
        public void GetOccurrence_ReturnsCorrectDate_WhenExpressionContains5Fields(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var startInstant = GetInstantFromLocalTime(startTime, EasternTimeZone);
            var endInstant = startInstant.AddYears(100);

            var occurrence = expression.GetOccurrence(startInstant, endInstant, EasternTimeZone);

            Assert.Equal(GetInstantFromLocalTime(expectedTime, EasternTimeZone), occurrence);
        }

        [Theory]
        [InlineData("55 *   *  *  *  *  ", "2017-02-21 12:00:00", "2017-02-21 12:00:54")]
        [InlineData("0  50  *  *  *  *  ", "2017-02-21 12:01   ", "2017-02-21 12:49   ")]
        [InlineData("0  10  *  *  *  *  ", "2017-02-21 12:11   ", "2017-02-21 13:05   ")]
        [InlineData("0  0   22 *  *  *  ", "2017-02-21 12:11   ", "2017-02-21 20:05   ")]
        [InlineData("0  0   11 *  *  *  ", "2017-02-21 12:11   ", "2017-02-22 10:05   ")]
        [InlineData("0  0   0  1  *  *  ", "2017-02-21 12:11   ", "2017-02-28 23:05   ")]
        [InlineData("0  0   0  12 *  *  ", "2017-02-21 12:11   ", "2017-03-11 23:59   ")]
        [InlineData("0  0   0  1  3  *  ", "2017-02-21 12:11   ", "2017-02-28 23:59   ")]
        [InlineData("0  0   0  1  12 *  ", "2017-02-21 12:11   ", "2017-11-30 23:59   ")]
        [InlineData("0  0   0  *  2  *  ", "2017-03-21 12:11   ", "2018-01-30 23:59   ")]
        [InlineData("0  0   0  *  *  SUN", "2017-02-21 12:11   ", "2017-01-25 23:59   ")]
        [InlineData("0  0   0  *  *  TUE", "2017-02-22 12:11   ", "2017-01-28 23:59   ")]
        [InlineData("0  0   0  5W *  *  ", "2017-02-03 12:11   ", "2017-02-05 23:59   ")]

        [InlineData("0  0,5 17 4W *  *  ", "2017-02-02 17:01   ", "2017-02-03 16:00   ")]
        [InlineData("0  0,5 17 4W *  *  ", "2017-02-03 17:01   ", "2017-02-03 17:02   ")]
        [InlineData("0  0,5 17 4W *  *  ", "2017-02-03 17:06   ", "2017-02-05 17:06   ")]
        public void GetOccurrence_ReturnsNull_WhenNextOccurrenceIsAfterEndTime(string cronExpression, string startTime, string endTime)
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var startInstant = GetInstantFromLocalTime(startTime, EasternTimeZone);
            var endInstant = GetInstantFromLocalTime(endTime, EasternTimeZone);

            var occurrence = expression.GetOccurrence(startInstant, endInstant, EasternTimeZone);

            Assert.Equal(null, occurrence);
        }

        private static IEnumerable<object[]> GetTimeZones()
        {
            yield return new object[] {EasternTimeZone};
            yield return new object[] {JordanTimeZone};
            yield return new object[] {TimeZoneInfo.Utc};
        }

        private static DateTimeOffset GetInstantFromLocalTime(string localDateTimeString, TimeZoneInfo zone)
        {
            localDateTimeString = localDateTimeString.Trim();

            var dateTime = DateTime.ParseExact(
                localDateTimeString,
                new[]
                {
                    "HH:mm:ss",
                    "HH:mm",
                    "yyyy-MM-dd HH:mm:ss",
                    "yyyy-MM-dd HH:mm",
                    "yyyy-MM-dd"
                },
                CultureInfo.InvariantCulture,
                DateTimeStyles.NoCurrentDateDefault);

            var localDateTime = new DateTime(
                dateTime.Year != 1 ? dateTime.Year : Today.Year,
                dateTime.Year != 1 ? dateTime.Month : Today.Month,
                dateTime.Year != 1 ? dateTime.Day : Today.Day,
                dateTime.Hour,
                dateTime.Minute,
                dateTime.Second);

            return new DateTimeOffset(localDateTime, zone.GetUtcOffset(localDateTime));
        }

        private static DateTimeOffset GetInstant(string dateTimeOffsetString)
        {
            dateTimeOffsetString = dateTimeOffsetString.Trim();

            var dateTime = DateTimeOffset.ParseExact(
                dateTimeOffsetString,
                new[]
                {
                    "yyyy-MM-dd HH:mm:ss zzz",
                    "yyyy-MM-dd HH:mm zzz",
                },
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);

            return dateTime;
        }
    }
}
// The MIT License(MIT)
// 
// Copyright (c) 2017 Sergey Odinokov
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Xunit;

namespace Cronos.Tests
{
    public class CronExpressionFacts
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

        private static readonly TimeZoneInfo EasternTimeZone = TimeZoneInfo.FindSystemTimeZoneById(EasternTimeZoneId);
        private static readonly TimeZoneInfo JordanTimeZone = TimeZoneInfo.FindSystemTimeZoneById(JordanTimeZoneId);
        private static readonly TimeZoneInfo LordHoweTimeZone = TimeZoneInfo.FindSystemTimeZoneById(LordHoweTimeZoneId);

        private static readonly DateTime Today = new DateTime(2016, 12, 09);

        private static readonly CronExpression MinutelyExpression = CronExpression.Parse("* * * * *");

        [Theory]

        // Handle tabs.
        [InlineData("*	*	* * * *")]

        // Handle white spaces at the beginning and end of expression.
        [InlineData(" 	*	*	* * * *    ")]

        // Handle white spaces for macros.
        [InlineData("  @every_second ")]
        public void HandleWhiteSpaces(string cronExpression)
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var from = new DateTime(2016, 03, 18, 12, 0, 0, DateTimeKind.Utc);

            var result = expression.GetNextOccurrence(from, inclusive: true);

            Assert.Equal(from, result);
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

        [InlineData("-1   * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("-    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("5-   * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData(",    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData(",1   * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("/    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("*/   * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("1/   * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("1/0  * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("1/60 * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("1/k  * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("1k   * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("#    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("*#1  * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("0#2  * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("L    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("l    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("W    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("w    * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("LW   * * * * *", CronFormat.IncludeSeconds, "Seconds")]
        [InlineData("lw   * * * * *", CronFormat.IncludeSeconds, "Seconds")]

        // 2147483648 = Int32.MaxValue + 1

        [InlineData("1/2147483648 * * * * *", CronFormat.IncludeSeconds, "Seconds")]

        // Minute field is invalid.
        [InlineData("60    * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("-1    * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("-     * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("7-    * * * *", CronFormat.Standard, "Minutes")]
        [InlineData(",     * * * *", CronFormat.Standard, "Minutes")]
        [InlineData(",1    * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("*/    * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("/     * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("1/    * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("1/0   * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("1/60  * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("1/k   * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("1k    * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("#     * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("*#1   * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("5#3   * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("L     * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("l     * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("W     * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("w     * * * *", CronFormat.Standard, "Minutes")]
        [InlineData("lw    * * * *", CronFormat.Standard, "Minutes")]

        [InlineData("* 60    * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* -1    * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* -     * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* 7-    * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* ,     * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* ,1    * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* */    * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* /     * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* 1/    * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* 1/0   * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* 1/60  * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* 1/k   * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* 1k    * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* #     * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* *#1   * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* 5#3   * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* L     * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* l     * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* W     * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* w     * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* LW    * * * *", CronFormat.IncludeSeconds, "Minutes")]
        [InlineData("* lw    * * * *", CronFormat.IncludeSeconds, "Minutes")]

        // Hour field is invalid.
        [InlineData("* 25   * * *", CronFormat.Standard, "Hours")]
        [InlineData("* -1   * * *", CronFormat.Standard, "Hours")]
        [InlineData("* -    * * *", CronFormat.Standard, "Hours")]
        [InlineData("* 0-   * * *", CronFormat.Standard, "Hours")]
        [InlineData("* ,    * * *", CronFormat.Standard, "Hours")]
        [InlineData("* ,1   * * *", CronFormat.Standard, "Hours")]
        [InlineData("* /    * * *", CronFormat.Standard, "Hours")]
        [InlineData("* 1/   * * *", CronFormat.Standard, "Hours")]
        [InlineData("* 1/0  * * *", CronFormat.Standard, "Hours")]
        [InlineData("* 1/24 * * *", CronFormat.Standard, "Hours")]
        [InlineData("* 1/k  * * *", CronFormat.Standard, "Hours")]
        [InlineData("* 1k   * * *", CronFormat.Standard, "Hours")]
        [InlineData("* #    * * *", CronFormat.Standard, "Hours")]
        [InlineData("* *#2  * * *", CronFormat.Standard, "Hours")]
        [InlineData("* 10#1 * * *", CronFormat.Standard, "Hours")]
        [InlineData("* L    * * *", CronFormat.Standard, "Hours")]
        [InlineData("* l    * * *", CronFormat.Standard, "Hours")]
        [InlineData("* W    * * *", CronFormat.Standard, "Hours")]
        [InlineData("* w    * * *", CronFormat.Standard, "Hours")]
        [InlineData("* LW   * * *", CronFormat.Standard, "Hours")]
        [InlineData("* lw   * * *", CronFormat.Standard, "Hours")]

        [InlineData("* * 25   * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * -1   * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * -    * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * 0-   * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * ,    * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * ,1   * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * /    * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * 1/   * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * 1/0  * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * 1/24 * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * 1/k  * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * 1k   * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * #    * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * *#2  * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * 10#1 * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * L    * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * l    * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * W    * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * w    * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * LW   * * *", CronFormat.IncludeSeconds, "Hours")]
        [InlineData("* * lw   * * *", CronFormat.IncludeSeconds, "Hours")]

        // Day of month field is invalid.
        [InlineData("* * 32     *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 10-32  *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 31-32  *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * -1     *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * -      *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 8-     *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * ,      *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * ,1     *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * /      *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1/     *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1/0    *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1/32   *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1/k    *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1m     *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * T      *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * MON    *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * mon    *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * #      *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * *#3    *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 4#1    *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * W      *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * w      *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1-2W   *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1-2w   *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1,2W   *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1,2w   *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1/2W   *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1/2w   *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1-2/2W *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1-2/2w *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1LW    *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * 1lw    *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * L-31   *  *", CronFormat.Standard, "Days of month")]
        [InlineData("* * l-31   *  *", CronFormat.Standard, "Days of month")]

        [InlineData("* * * 32     *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 10-32  *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 31-32  *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * -1     *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * -      *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 8-     *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * ,      *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * ,1     *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * /      *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1/     *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1/0    *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1/32   *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1/k    *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1m     *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * T      *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * MON    *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * mon    *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * #      *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * *#3    *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 4#1    *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * W      *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * w      *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1-2W   *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1-2w   *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1,2W   *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1,2w   *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1/2W   *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1/2w   *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1-2/2W *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1-2/2w *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1LW    *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * 1lw    *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * L-31   *  *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * l-31   *  *", CronFormat.IncludeSeconds, "Days of month")]

        // Month field is invalid.
        [InlineData("* * * 13   *", CronFormat.Standard, "Months")]
        [InlineData("* * * -1   *", CronFormat.Standard, "Months")]
        [InlineData("* * * -    *", CronFormat.Standard, "Months")]
        [InlineData("* * * 2-   *", CronFormat.Standard, "Months")]
        [InlineData("* * * ,    *", CronFormat.Standard, "Months")]
        [InlineData("* * * ,1   *", CronFormat.Standard, "Months")]
        [InlineData("* * * /    *", CronFormat.Standard, "Months")]
        [InlineData("* * * */   *", CronFormat.Standard, "Months")]
        [InlineData("* * * 1/   *", CronFormat.Standard, "Months")]
        [InlineData("* * * 1/0  *", CronFormat.Standard, "Months")]
        [InlineData("* * * 1/13 *", CronFormat.Standard, "Months")]
        [InlineData("* * * 1/k  *", CronFormat.Standard, "Months")]
        [InlineData("* * * 1k   *", CronFormat.Standard, "Months")]
        [InlineData("* * * #    *", CronFormat.Standard, "Months")]
        [InlineData("* * * *#1  *", CronFormat.Standard, "Months")]
        [InlineData("* * * */2# *", CronFormat.Standard, "Months")]
        [InlineData("* * * 2#2  *", CronFormat.Standard, "Months")]
        [InlineData("* * * L    *", CronFormat.Standard, "Months")]
        [InlineData("* * * l    *", CronFormat.Standard, "Months")]
        [InlineData("* * * W    *", CronFormat.Standard, "Months")]
        [InlineData("* * * w    *", CronFormat.Standard, "Months")]
        [InlineData("* * * LW   *", CronFormat.Standard, "Months")]
        [InlineData("* * * lw   *", CronFormat.Standard, "Months")]

        [InlineData("* * * * 13   *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * -1   *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * -    *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * 2-   *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * ,    *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * ,1   *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * /    *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * */   *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * 1/   *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * 1/0  *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * 1/13 *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * 1/k  *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * 1k   *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * #    *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * *#1  *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * */2# *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * 2#2  *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * L    *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * l    *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * W    *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * w    *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * LW   *", CronFormat.IncludeSeconds, "Months")]
        [InlineData("* * * * lw   *", CronFormat.IncludeSeconds, "Months")]

        // Day of week field is invalid.
        [InlineData("* * * * 8      ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * -1     ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * -      ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * 3-     ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * ,      ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * ,1     ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * /      ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * */     ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * 1/     ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * 1/0    ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * 1/8    ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * #      ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * 0#     ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * 5#6    ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * SUN#6  ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * sun#6  ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * SUN#050", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * sun#050", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * 0#0    ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * SUT    ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * sut    ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * SU0    ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * SUNDAY ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * L      ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * l      ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * W      ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * w      ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * LW     ", CronFormat.Standard, "Days of week")]
        [InlineData("* * * * lw     ", CronFormat.Standard, "Days of week")]

        [InlineData("* * * * * 8      ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * -1     ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * -      ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * 3-     ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * ,      ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * ,1     ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * /      ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * */     ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * 1/     ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * 1/0    ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * 1/8    ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * #      ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * 0#     ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * 5#6    ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * SUN#6  ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * sun#6  ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * SUN#050", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * sun#050", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * 0#0    ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * SUT    ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * sut    ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * SU0    ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * SUNDAY ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * L      ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * l      ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * W      ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * w      ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * LW     ", CronFormat.IncludeSeconds, "Days of week")]
        [InlineData("* * * * * lw     ", CronFormat.IncludeSeconds, "Days of week")]

        // Fields count is invalid.
        [InlineData("* * *        ", CronFormat.Standard, "Months")]
        [InlineData("* * * * * * *", CronFormat.Standard, "")]

        [InlineData("* * * *", CronFormat.IncludeSeconds, "Days of month")]
        [InlineData("* * * * * * *", CronFormat.IncludeSeconds, "")]

        // Macro is invalid.
        [InlineData("@", CronFormat.Standard, "")]

        // ReSharper disable StringLiteralTypo
        [InlineData("@invalid        ", CronFormat.Standard, "")]
        [InlineData("          @yearl", CronFormat.Standard, "")]
        [InlineData("@yearl          ", CronFormat.Standard, "")]
        [InlineData("@yearly !       ", CronFormat.Standard, "")]
        [InlineData("@every_hour     ", CronFormat.Standard, "")]
        [InlineData("@@daily         ", CronFormat.Standard, "")]
        [InlineData("@yeannually     ", CronFormat.Standard, "")]
        [InlineData("@yweekly        ", CronFormat.Standard, "")]
        [InlineData("@ymonthly       ", CronFormat.Standard, "")]
        [InlineData("@ydaily         ", CronFormat.Standard, "")]
        [InlineData("@ymidnight      ", CronFormat.Standard, "")]
        [InlineData("@yhourly        ", CronFormat.Standard, "")]
        [InlineData("@yevery_second  ", CronFormat.Standard, "")]
        [InlineData("@yevery_minute  ", CronFormat.Standard, "")]
        [InlineData("@every_minsecond", CronFormat.Standard, "")]
        [InlineData("@annuall        ", CronFormat.Standard, "")]
        [InlineData("@dail           ", CronFormat.Standard, "")]
        [InlineData("@hour           ", CronFormat.Standard, "")]
        [InlineData("@midn           ", CronFormat.Standard, "")]
        [InlineData("@week           ", CronFormat.Standard, "")]

        [InlineData("@", CronFormat.IncludeSeconds, "")]

        [InlineData("@invalid        ", CronFormat.IncludeSeconds, "")]
        [InlineData("          @yearl", CronFormat.IncludeSeconds, "")]
        [InlineData("@yearl          ", CronFormat.IncludeSeconds, "")]
        [InlineData("@yearly !       ", CronFormat.IncludeSeconds, "")]
        [InlineData("@dai            ", CronFormat.IncludeSeconds, "")]
        [InlineData("@a              ", CronFormat.IncludeSeconds, "")]
        [InlineData("@every_hour     ", CronFormat.IncludeSeconds, "")]
        [InlineData("@everysecond    ", CronFormat.IncludeSeconds, "")]
        [InlineData("@@daily         ", CronFormat.IncludeSeconds, "")]
        [InlineData("@yeannually     ", CronFormat.IncludeSeconds, "")]
        [InlineData("@yweekly        ", CronFormat.IncludeSeconds, "")]
        [InlineData("@ymonthly       ", CronFormat.IncludeSeconds, "")]
        [InlineData("@ydaily         ", CronFormat.IncludeSeconds, "")]
        [InlineData("@ymidnight      ", CronFormat.IncludeSeconds, "")]
        [InlineData("@yhourly        ", CronFormat.IncludeSeconds, "")]
        [InlineData("@yevery_second  ", CronFormat.IncludeSeconds, "")]
        [InlineData("@yevery_minute  ", CronFormat.IncludeSeconds, "")]
        [InlineData("@every_minsecond", CronFormat.IncludeSeconds, "")]
        [InlineData("@annuall        ", CronFormat.IncludeSeconds, "")]
        [InlineData("@dail           ", CronFormat.IncludeSeconds, "")]
        [InlineData("@hour           ", CronFormat.IncludeSeconds, "")]
        [InlineData("@midn           ", CronFormat.IncludeSeconds, "")]
        [InlineData("@week           ", CronFormat.IncludeSeconds, "")]
        
        [InlineData("60 * * * *", CronFormat.Standard, "between 0 and 59")]
        [InlineData("*/60 * * * *", CronFormat.Standard, "between 1 and 59")]
        // ReSharper restore StringLiteralTypo
        public void Parse_ThrowsCronFormatException_WhenCronExpressionIsInvalid(string cronExpression, CronFormat format, string invalidField)
        {
            var exception = Assert.Throws<CronFormatException>(() => CronExpression.Parse(cronExpression, format));

            Assert.Contains(invalidField, exception.Message);
        }

        [Theory]
        [InlineData("  @yearly      ", CronFormat.Standard)]
        [InlineData("  @YEARLY      ", CronFormat.Standard)]
        [InlineData("  @annually    ", CronFormat.Standard)]
        [InlineData("  @ANNUALLY    ", CronFormat.Standard)]
        [InlineData("  @monthly     ", CronFormat.Standard)]
        [InlineData("  @MONTHLY     ", CronFormat.Standard)]
        [InlineData("  @weekly      ", CronFormat.Standard)]
        [InlineData("  @WEEKLY      ", CronFormat.Standard)]
        [InlineData("  @daily       ", CronFormat.Standard)]
        [InlineData("  @DAILY       ", CronFormat.Standard)]
        [InlineData("  @midnight    ", CronFormat.Standard)]
        [InlineData("  @MIDNIGHT    ", CronFormat.Standard)]
        [InlineData("  @every_minute", CronFormat.Standard)]
        [InlineData("  @EVERY_MINUTE", CronFormat.Standard)]
        [InlineData("  @every_second", CronFormat.Standard)]
        [InlineData("  @EVERY_SECOND", CronFormat.Standard)]

        [InlineData("  @yearly      ", CronFormat.IncludeSeconds)]
        [InlineData("  @YEARLY      ", CronFormat.IncludeSeconds)]
        [InlineData("  @annually    ", CronFormat.IncludeSeconds)]
        [InlineData("  @ANNUALLY    ", CronFormat.IncludeSeconds)]
        [InlineData("  @monthly     ", CronFormat.IncludeSeconds)]
        [InlineData("  @MONTHLY     ", CronFormat.IncludeSeconds)]
        [InlineData("  @weekly      ", CronFormat.IncludeSeconds)]
        [InlineData("  @WEEKLY      ", CronFormat.IncludeSeconds)]
        [InlineData("  @daily       ", CronFormat.IncludeSeconds)]
        [InlineData("  @DAILY       ", CronFormat.IncludeSeconds)]
        [InlineData("  @midnight    ", CronFormat.IncludeSeconds)]
        [InlineData("  @MIDNIGHT    ", CronFormat.IncludeSeconds)]
        [InlineData("  @every_minute", CronFormat.IncludeSeconds)]
        [InlineData("  @EVERY_MINUTE", CronFormat.IncludeSeconds)]
        [InlineData("  @every_second", CronFormat.IncludeSeconds)]
        [InlineData("  @EVERY_SECOND", CronFormat.IncludeSeconds)]
        public void Parse_DoesNotThrowAnException_WhenExpressionIsMacro(string cronExpression, CronFormat format)
        {
            CronExpression.Parse(cronExpression, format);
        }

        [Theory]
        [InlineData(DateTimeKind.Unspecified, false)]
        [InlineData(DateTimeKind.Unspecified, true)]
        [InlineData(DateTimeKind.Local,       false)]
        [InlineData(DateTimeKind.Local,       true)]
        public void GetNextOccurrence_ThrowsAnException_WhenFromHasAWrongKind(DateTimeKind kind, bool inclusive)
        {
            var from = new DateTime(2017, 03, 22, 0, 0, 0, kind);
            
            var exception = Assert.Throws<ArgumentException>(() => MinutelyExpression.GetNextOccurrence(from, TimeZoneInfo.Local, inclusive));

            Assert.Equal("fromUtc", exception.ParamName);
        }

        [Theory]
        [InlineData(DateTimeKind.Unspecified, false)]
        [InlineData(DateTimeKind.Unspecified, true)]
        [InlineData(DateTimeKind.Local, false)]
        [InlineData(DateTimeKind.Local, true)]
        public void GetNextOccurrence_ThrowsAnException_WhenFromDoesNotHaveUtcKind(DateTimeKind kind, bool inclusive)
        {
            var from = new DateTime(2017, 03, 15, 0, 0, 0, kind);
            var exception = Assert.Throws<ArgumentException>(() => MinutelyExpression.GetNextOccurrence(from, inclusive));

            Assert.Equal("fromUtc", exception.ParamName);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GetNextOccurrence_ReturnsDateTimeWithUtcKind(bool inclusive)
        {
            var from = new DateTime(2017, 03, 22, 9, 32, 0, DateTimeKind.Utc);
            var occurrence = MinutelyExpression.GetNextOccurrence(from, inclusive);

            Assert.Equal(DateTimeKind.Utc, occurrence?.Kind);
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

        // Support 'L' character in day of month field.

        [InlineData("* * * L * *","2016-01-05", "2016-01-31")]
        [InlineData("* * * L * *","2016-01-31", "2016-01-31")]
        [InlineData("* * * L * *","2016-02-05", "2016-02-29")]
        [InlineData("* * * L * *","2016-02-29", "2016-02-29")]
        [InlineData("* * * L 2 *","2016-02-29", "2016-02-29")]
        [InlineData("* * * L * *","2017-02-28", "2017-02-28")]
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

        // ReSharper disable StringLiteralTypo
        [InlineData("* * * * * 0L  ", "2017-01-29", "2017-01-29")]
        [InlineData("* * * * * 0L  ", "2017-01-01", "2017-01-29")]
        [InlineData("* * * * * SUNL", "2017-01-01", "2017-01-29")]
        [InlineData("* * * * * 1L  ", "2017-01-30", "2017-01-30")]
        [InlineData("* * * * * 1L  ", "2017-01-01", "2017-01-30")]
        [InlineData("* * * * * MONL", "2017-01-01", "2017-01-30")]
        [InlineData("* * * * * 2L  ", "2017-01-31", "2017-01-31")]
        [InlineData("* * * * * 2L  ", "2017-01-01", "2017-01-31")]
        [InlineData("* * * * * TUEL", "2017-01-01", "2017-01-31")]
        [InlineData("* * * * * 3L  ", "2017-01-25", "2017-01-25")]
        [InlineData("* * * * * 3L  ", "2017-01-01", "2017-01-25")]
        [InlineData("* * * * * WEDL", "2017-01-01", "2017-01-25")]
        [InlineData("* * * * * 4L  ", "2017-01-26", "2017-01-26")]
        [InlineData("* * * * * 4L  ", "2017-01-01", "2017-01-26")]
        [InlineData("* * * * * THUL", "2017-01-01", "2017-01-26")]
        [InlineData("* * * * * 5L  ", "2017-01-27", "2017-01-27")]
        [InlineData("* * * * * 5L  ", "2017-01-01", "2017-01-27")]
        [InlineData("* * * * * FRIL", "2017-01-01", "2017-01-27")]
        [InlineData("* * * * * 6L  ", "2017-01-28", "2017-01-28")]
        [InlineData("* * * * * 6L  ", "2017-01-01", "2017-01-28")]
        [InlineData("* * * * * SATL", "2017-01-01", "2017-01-28")]
        [InlineData("* * * * * 7L  ", "2017-01-29", "2017-01-29")]
        [InlineData("* * * * * 7L  ", "2016-12-31", "2017-01-29")]
        // ReSharper restore StringLiteralTypo

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

        [InlineData("0 30    10 1W * *", "2017-04-01 00:00", "2017-04-03 10:30")]
        [InlineData("0 30    10 1W * *", "2017-04-01 12:00", "2017-04-03 10:30")]
        [InlineData("0 30    10 1W * *", "2017-04-02 00:00", "2017-04-03 10:30")]
        [InlineData("0 30    10 1W * *", "2017-04-02 12:00", "2017-04-03 10:30")]
        [InlineData("0 30    10 1W * *", "2017-04-03 00:00", "2017-04-03 10:30")]
        [InlineData("0 30    10 1W * *", "2017-04-03 12:00", "2017-05-01 10:30")]

        [InlineData("0 30    10 2W * *", "2017-04-01 00:00", "2017-04-03 10:30")]
        [InlineData("0 30    10 2W * *", "2017-04-01 12:00", "2017-04-03 10:30")]
        [InlineData("0 30    10 2W * *", "2017-04-02 00:00", "2017-04-03 10:30")]
        [InlineData("0 30    10 2W * *", "2017-04-02 12:00", "2017-04-03 10:30")]
        [InlineData("0 30    10 2W * *", "2017-04-03 00:00", "2017-04-03 10:30")]
        [InlineData("0 30    10 2W * *", "2017-04-03 12:00", "2017-05-02 10:30")]

        [InlineData("0 30    17 7W * *", "2017-01-06 17:45", "2017-02-07 17:30")]
        [InlineData("0 30,45 17 7W * *", "2017-01-06 17:45", "2017-01-06 17:45")]
        [InlineData("0 30,55 17 7W * *", "2017-01-06 17:45", "2017-01-06 17:55")]

        [InlineData("0 30    17 8W * *", "2017-01-08 19:45", "2017-01-09 17:30")]

        [InlineData("0 30    17 30W * *", "2017-04-28 17:45", "2017-05-30 17:30")]
        [InlineData("0 30,45 17 30W * *", "2017-04-28 17:45", "2017-04-28 17:45")]
        [InlineData("0 30,55 17 30W * *", "2017-04-28 17:45", "2017-04-28 17:55")]

        [InlineData("0 30    17 30W * *", "2017-02-06 00:00", "2017-03-30 17:30")]

        [InlineData("0 30    17 31W * *", "2018-03-30 17:45", "2018-05-31 17:30")]
        [InlineData("0 30    17 15W * *", "2016-12-30 17:45", "2017-01-16 17:30")]

        [InlineData("0 30    17 27W * 1L ", "2017-03-10 17:45", "2017-03-27 17:30")]
        [InlineData("0 30    17 27W * 1#4", "2017-03-10 17:45", "2017-03-27 17:30")]

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

        [InlineData("? ? ? ? ? ?", "2016-12-09 16:46", "2016-12-09 16:46")]
        [InlineData("* * * * * ?", "2016-12-09 16:46", "2016-12-09 16:46")]
        [InlineData("* * * ? * *", "2016-03-09 16:46", "2016-03-09 16:46")]
        [InlineData("* * * * * ?", "2016-12-30 16:46", "2016-12-30 16:46")]
        [InlineData("* * * ? * *", "2016-12-09 02:46", "2016-12-09 02:46")]
        [InlineData("* * * * * ?", "2016-12-09 16:09", "2016-12-09 16:09")]
        [InlineData("* * * ? * *", "2099-12-09 16:46", "2099-12-09 16:46")]

        // Day of 100-year and not 400-year.
        [InlineData("* * * * * *", "1900-02-20 16:46", "1900-02-20 16:46")]

        // Day of 400-year
        [InlineData("* * * * * *", "2000-02-28 16:46", "2000-02-28 16:46")]

        // Last day of 400-year.
        [InlineData("* * * * * *", "2000-12-31 16:46", "2000-12-31 16:46")]

        // Case insensitive.
        [InlineData("* *  *  lw   * *   ", "2017-05-30", "2017-05-31")]
        [InlineData("* *  *  l-0w * *   ", "2016-02-01", "2016-02-29")]
        [InlineData("0 30 17 27w  * 1l  ", "2017-03-10 17:45", "2017-03-27 17:30")]
        [InlineData("0 30 17 27w  * mOnL", "2017-03-10 17:45", "2017-03-27 17:30")]

        // Complex expressions
        [InlineData("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:00", "2017-04-17 17:20")]
        [InlineData("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:21", "2017-04-17 17:30")]
        [InlineData("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:31", "2017-04-17 17:32")]
        [InlineData("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:33", "2017-04-17 17:34")]
        [InlineData("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:35", "2017-04-17 17:40")]
        [InlineData("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:41", "2017-04-17 17:50")]
        [InlineData("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:51", "2017-04-17 17:57")]
        [InlineData("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:58", "2017-04-17 17:58")]
        [InlineData("0 57,20/20,30/20,32-34/2,58 * * * * ", "2017-04-17 17:59", "2017-04-17 18:20")]
        public void GetNextOccurrence_ReturnsCorrectDate(string cronExpression, string fromString, string expectedString)
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var fromInstant = GetInstantFromLocalTime(fromString, EasternTimeZone);

            var occurrence = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive: true);

            Assert.Equal(GetInstantFromLocalTime(expectedString, EasternTimeZone), occurrence);
        }

        [Theory]
        [InlineData(true, 00001)]
        [InlineData(true, 09999)]
        [InlineData(false, 0001)]
        [InlineData(false, 9999)]
        public void GetNextOccurrence_RoundsFromUtcUpToTheSecond(bool inclusiveFrom, int extraTicks)
        {
            var expression = CronExpression.Parse("* * * * * *", CronFormat.IncludeSeconds);
            var fromUtc = new DateTime(2017, 07, 20, 11, 59, 59, DateTimeKind.Utc).AddTicks(extraTicks);

            var occurrence = expression.GetNextOccurrence(fromUtc, inclusive: inclusiveFrom);

            Assert.Equal(new DateTime(2017, 07, 20, 12, 0, 0, DateTimeKind.Utc), occurrence);
        }

        [Theory]

        // 2016-03-13 is date when the clock jumps forward from 1:59 am -05:00 standard time (ST) to 3:00 am -04:00 DST in Eastern Time Zone.
        // ________1:59 ST///invalid///3:00 DST________

        // Run missed.

        [InlineData("0 */30 *      *  *  *    ", "2016-03-13 01:45 -05:00", "2016-03-13 03:00 -04:00", true)]
        [InlineData("0 */30 */2    *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
        [InlineData("0 1-58 */2    *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
        [InlineData("0 0,30 0-23/2 *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
        [InlineData("0 */30 2      *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
        [InlineData("0 0,30 2      *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
        [InlineData("0 */30 2      13 03 *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
        [InlineData("0 0,30 02     13 03 *    ", "2016-03-13 01:45 -05:00", "2016-03-13 03:00 -04:00", true)]
        [InlineData("0 30   2      *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
        [InlineData("0 0    */2    *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]
        [InlineData("0 30   0-23/2 *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]

        [InlineData("0 0,59 *      *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 01:59 -05:00", true)]
        [InlineData("0 0,59 *      *  *  *    ", "2016-03-13 03:00 -04:00", "2016-03-13 03:00 -04:00", true)]
                                                                                               
        [InlineData("0 30   *      *  3  SUN#2", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", true)]

        [InlineData("0 */30 *      *  *  *    ", "2016-03-13 01:30 -05:00", "2016-03-13 03:00 -04:00", false)]
        [InlineData("0 */30 */2    *  *  *    ", "2016-03-13 01:30 -05:00", "2016-03-13 03:00 -04:00", false)]
        [InlineData("0 1-58 */2    *  *  *    ", "2016-03-13 01:58 -05:00", "2016-03-13 03:00 -04:00", false)]
        [InlineData("0 0,30 0-23/2 *  *  *    ", "2016-03-13 00:30 -05:00", "2016-03-13 03:00 -04:00", false)]
        [InlineData("0 0,30 2      *  *  *    ", "2016-03-12 02:30 -05:00", "2016-03-13 03:00 -04:00", false)]
        [InlineData("0 */30 2      13 03 *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", false)]
        [InlineData("0 0,30 02     13 03 *    ", "2016-03-13 01:45 -05:00", "2016-03-13 03:00 -04:00", false)]
        [InlineData("0 30   2      *  *  *    ", "2016-03-12 02:30 -05:00", "2016-03-13 03:00 -04:00", false)]
        [InlineData("0 0    */2    *  *  *    ", "2016-03-13 00:00 -05:00", "2016-03-13 03:00 -04:00", false)]
        [InlineData("0 30   0-23/2 *  *  *    ", "2016-03-13 00:30 -05:00", "2016-03-13 03:00 -04:00", false)]

        [InlineData("0 0,59 *      *  *  *    ", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", false)]

        [InlineData("0 30   *      *  3  SUN#2", "2016-03-13 01:59 -05:00", "2016-03-13 03:00 -04:00", false)]
        public void GetNextOccurrence_HandleDST_WhenTheClockJumpsForward_And_TimeZoneIsEst(string cronExpression, string fromString, string expectedString, bool inclusive)
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var fromInstant = GetInstant(fromString);
            var expectedInstant = GetInstant(expectedString);

            var executed = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive);

            Assert.Equal(expectedInstant, executed);
            Assert.Equal(expectedInstant.Offset, executed?.Offset);
        }

        [Theory]

        // 2017-10-01 is date when the clock jumps forward from 1:59 am +10:30 standard time (ST) to 2:30 am +11:00 DST on Lord Howe.
        // ________1:59 ST///invalid///2:30 DST________

        // Run missed.

        [InlineData("0 */30 *      *  *  *    ", "2017-10-01 01:45 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 */30 */2    *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 1-58 */2    *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 0,30 0-23/2 *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 */30 2      *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 0,30 2      *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 */30 2      01 10 *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 0,30 02     01 10 *    ", "2017-10-01 01:45 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 30   2      *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 0,30 */2    *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        [InlineData("0 30   0-23/2 *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]

        [InlineData("0 0,30,59 *      *  *  *    ", "2017-10-01 01:59 +10:30", "2017-10-01 01:59 +10:30")]
        [InlineData("0 0,30,59 *      *  *  *    ", "2017-10-01 02:30 +11:00", "2017-10-01 02:30 +11:00")]

        [InlineData("0 30   *      *  10 SUN#1", "2017-10-01 01:59 +10:30", "2017-10-01 02:30 +11:00")]
        public void GetNextOccurrence_HandleDST_WhenTheClockTurnForwardHalfHour(string cronExpression, string fromString, string expectedString)
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var fromInstant = GetInstant(fromString);
            var expectedInstant = GetInstant(expectedString);

            var executed = expression.GetNextOccurrence(fromInstant, LordHoweTimeZone, inclusive: true);

            Assert.Equal(expectedInstant, executed);
            Assert.Equal(expectedInstant.Offset, executed?.Offset);
        }

        [Theory]

        // 2016-11-06 is date when the clock jumps backward from 2:00 am -04:00 DST to 1:00 am -05:00 ST in Eastern Time Zone.
        // _______1:00 DST____1:59 DST -> 1:00 ST____2:00 ST_______

        // Run at 2:00 ST because 2:00 DST is invalid.
        [InlineData("0 */30 */2 * * *", "2016-11-06 01:30 -04:00", "2016-11-06 02:00 -05:00", true)]
        [InlineData("0 0    */2 * * *", "2016-11-06 00:30 -04:00", "2016-11-06 02:00 -05:00", true)]
        [InlineData("0 0    0/2 * * *", "2016-11-06 00:30 -04:00", "2016-11-06 02:00 -05:00", true)]
        [InlineData("0 0    2-3 * * *", "2016-11-06 00:30 -04:00", "2016-11-06 02:00 -05:00", true)]

        // Run twice due to intervals.
        [InlineData("0 */30 *   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
        [InlineData("0 */30 *   * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -04:00", true)]
        [InlineData("0 */30 *   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:00 -05:00", true)]
        [InlineData("0 */30 *   * * *", "2016-11-06 01:15 -05:00", "2016-11-06 01:30 -05:00", true)]
        [InlineData("0 */30 *   * * *", "2016-11-06 01:30 -05:00", "2016-11-06 01:30 -05:00", true)]
        [InlineData("0 */30 *   * * *", "2016-11-06 01:45 -05:00", "2016-11-06 02:00 -05:00", true)]
        [InlineData("0 */30 *   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:30 -04:00", false)]
        [InlineData("0 */30 *   * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:00 -05:00", false)]
        [InlineData("0 */30 *   * * *", "2016-11-06 01:00 -05:00", "2016-11-06 01:30 -05:00", false)]
        [InlineData("0 */30 *   * * *", "2016-11-06 01:30 -05:00", "2016-11-06 02:00 -05:00", false)]

        [InlineData("0 30   *   * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -04:00", true)]
        [InlineData("0 30   *   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:30 -05:00", true)]
        [InlineData("0 30   *   * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -05:00", false)]
        [InlineData("0 30   *   * * *", "2016-11-06 01:30 -05:00", "2016-11-06 02:30 -05:00", false)]

        [InlineData("0 30   */1  * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -04:00", true)]
        [InlineData("0 30   */1  * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:30 -05:00", true)]
        [InlineData("0 30   0/1  * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -04:00", true)]
        [InlineData("0 30   0/1  * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:30 -05:00", true)]
        [InlineData("0 30   */1  * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -05:00", false)]
        [InlineData("0 30   0/1  * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -05:00", false)]

        [InlineData("0 30   1-9 * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -04:00", true)]
        [InlineData("0 30   1-9 * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:30 -05:00", true)]
        [InlineData("0 30   1-9 * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:30 -05:00", false)]

        [InlineData("0 */30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
        [InlineData("0 */30 1   * * *", "2016-11-06 01:20 -04:00", "2016-11-06 01:30 -04:00", true)]
        [InlineData("0 */30 1   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:00 -05:00", true)]
        [InlineData("0 */30 1   * * *", "2016-11-06 01:20 -05:00", "2016-11-06 01:30 -05:00", true)]
        [InlineData("0 */30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:30 -04:00", false)]
        [InlineData("0 */30 1   * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:00 -05:00", false)]

        [InlineData("0 0/30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
        [InlineData("0 0/30 1   * * *", "2016-11-06 01:20 -04:00", "2016-11-06 01:30 -04:00", true)]
        [InlineData("0 0/30 1   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:00 -05:00", true)]
        [InlineData("0 0/30 1   * * *", "2016-11-06 01:20 -05:00", "2016-11-06 01:30 -05:00", true)]
        [InlineData("0 0/30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:30 -04:00", false)]
        [InlineData("0 0/30 1   * * *", "2016-11-06 01:30 -04:00", "2016-11-06 01:00 -05:00", false)]
        [InlineData("0 0/30 1   * * *", "2016-11-06 01:00 -05:00", "2016-11-06 01:30 -05:00", false)]

        [InlineData("0 0-30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
        [InlineData("0 0-30 1   * * *", "2016-11-06 01:20 -04:00", "2016-11-06 01:20 -04:00", true)]
        [InlineData("0 0-30 1   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:00 -05:00", true)]
        [InlineData("0 0-30 1   * * *", "2016-11-06 01:20 -05:00", "2016-11-06 01:20 -05:00", true)]
        [InlineData("0 0-30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:01 -04:00", false)]
        [InlineData("0 0-30 1   * * *", "2016-11-06 01:20 -04:00", "2016-11-06 01:21 -04:00", false)]
        [InlineData("0 0-30 1   * * *", "2016-11-06 01:59 -04:00", "2016-11-06 01:00 -05:00", false)]
        [InlineData("0 0-30 1   * * *", "2016-11-06 01:20 -05:00", "2016-11-06 01:21 -05:00", false)]

        [InlineData("*/30 0 1 * * *", "2016-11-06 00:30:00 -04:00", "2016-11-06 01:00:00 -04:00", true)]
        [InlineData("*/30 0 1 * * *", "2016-11-06 01:00:01 -04:00", "2016-11-06 01:00:30 -04:00", true)]
        [InlineData("*/30 0 1 * * *", "2016-11-06 01:00:31 -04:00", "2016-11-06 01:00:00 -05:00", true)]
        [InlineData("*/30 0 1 * * *", "2016-11-06 01:00:01 -05:00", "2016-11-06 01:00:30 -05:00", true)]
        [InlineData("*/30 0 1 * * *", "2016-11-06 01:00:31 -05:00", "2016-11-07 01:00:00 -05:00", true)]
        [InlineData("*/30 0 1 * * *", "2016-11-06 00:30:00 -04:00", "2016-11-06 01:00:00 -04:00", false)]
        [InlineData("*/30 0 1 * * *", "2016-11-06 01:00:00 -04:00", "2016-11-06 01:00:30 -04:00", false)]
        [InlineData("*/30 0 1 * * *", "2016-11-06 01:00:30 -04:00", "2016-11-06 01:00:00 -05:00", false)]
        [InlineData("*/30 0 1 * * *", "2016-11-06 01:00:00 -05:00", "2016-11-06 01:00:30 -05:00", false)]
        [InlineData("*/30 0 1 * * *", "2016-11-06 01:00:30 -05:00", "2016-11-07 01:00:00 -05:00", false)]

        [InlineData("0/30 0 1 * * *", "2016-11-06 00:30:00 -04:00", "2016-11-06 01:00:00 -04:00", true)]
        [InlineData("0/30 0 1 * * *", "2016-11-06 01:00:01 -04:00", "2016-11-06 01:00:30 -04:00", true)]
        [InlineData("0/30 0 1 * * *", "2016-11-06 01:00:31 -04:00", "2016-11-06 01:00:00 -05:00", true)]
        [InlineData("0/30 0 1 * * *", "2016-11-06 01:00:01 -05:00", "2016-11-06 01:00:30 -05:00", true)]
        [InlineData("0/30 0 1 * * *", "2016-11-06 01:00:31 -05:00", "2016-11-07 01:00:00 -05:00", true)]
        [InlineData("0/30 0 1 * * *", "2016-11-06 00:30:00 -04:00", "2016-11-06 01:00:00 -04:00", false)]
        [InlineData("0/30 0 1 * * *", "2016-11-06 01:00:00 -04:00", "2016-11-06 01:00:30 -04:00", false)]
        [InlineData("0/30 0 1 * * *", "2016-11-06 01:00:30 -04:00", "2016-11-06 01:00:00 -05:00", false)]
        [InlineData("0/30 0 1 * * *", "2016-11-06 01:00:00 -05:00", "2016-11-06 01:00:30 -05:00", false)]
        [InlineData("0/30 0 1 * * *", "2016-11-06 01:00:30 -05:00", "2016-11-07 01:00:00 -05:00", false)]

        [InlineData("0-30 0 1 * * *", "2016-11-06 00:30:00 -04:00", "2016-11-06 01:00:00 -04:00", true)]
        [InlineData("0-30 0 1 * * *", "2016-11-06 01:00:01 -04:00", "2016-11-06 01:00:01 -04:00", true)]
        [InlineData("0-30 0 1 * * *", "2016-11-06 01:00:31 -04:00", "2016-11-06 01:00:00 -05:00", true)]
        [InlineData("0-30 0 1 * * *", "2016-11-06 01:00:01 -05:00", "2016-11-06 01:00:01 -05:00", true)]
        [InlineData("0-30 0 1 * * *", "2016-11-06 01:00:31 -05:00", "2016-11-07 01:00:00 -05:00", true)]
        [InlineData("0-30 0 1 * * *", "2016-11-06 00:30:00 -04:00", "2016-11-06 01:00:00 -04:00", false)]
        [InlineData("0-30 0 1 * * *", "2016-11-06 01:00:00 -04:00", "2016-11-06 01:00:01 -04:00", false)]
        [InlineData("0-30 0 1 * * *", "2016-11-06 01:00:30 -04:00", "2016-11-06 01:00:00 -05:00", false)]
        [InlineData("0-30 0 1 * * *", "2016-11-06 01:00:00 -05:00", "2016-11-06 01:00:01 -05:00", false)]
        [InlineData("0-30 0 1 * * *", "2016-11-06 01:00:30 -05:00", "2016-11-07 01:00:00 -05:00", false)]

        // Duplicates skipped due to certain time.
        [InlineData("0 0,30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
        [InlineData("0 0,30 1   * * *", "2016-11-06 01:20 -04:00", "2016-11-06 01:30 -04:00", true)]
        [InlineData("0 0,30 1   * * *", "2016-11-06 01:00 -05:00", "2016-11-07 01:00 -05:00", true)]
        [InlineData("0 0,30 1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:30 -04:00", false)]
        [InlineData("0 0,30 1   * * *", "2016-11-06 01:30 -04:00", "2016-11-07 01:00 -05:00", false)]

        [InlineData("0 0,30 1   * 1/2 *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
        [InlineData("0 0,30 1   * 1/2 *", "2016-11-06 01:20 -04:00", "2016-11-06 01:30 -04:00", true)]
        [InlineData("0 0,30 1   * 1/2 *", "2016-11-06 01:00 -05:00", "2016-11-07 01:00 -05:00", true)]
        [InlineData("0 0,30 1   * 1/2 *", "2016-11-06 01:00 -04:00", "2016-11-06 01:30 -04:00", false)]
        [InlineData("0 0,30 1   * 1/2 *", "2016-11-06 01:30 -04:00", "2016-11-07 01:00 -05:00", false)]

        [InlineData("0 0,30 1   6/1 1-12 0/1", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
        [InlineData("0 0,30 1   6/1 1-12 0/1", "2016-11-06 01:20 -04:00", "2016-11-06 01:30 -04:00", true)]
        [InlineData("0 0,30 1   6/1 1-12 0/1", "2016-11-06 01:00 -05:00", "2016-11-07 01:00 -05:00", true)]
        [InlineData("0 0,30 1   6/1 1-12 0/1", "2016-11-06 01:00 -04:00", "2016-11-06 01:30 -04:00", false)]
        [InlineData("0 0,30 1   6/1 1-12 0/1", "2016-11-06 01:30 -04:00", "2016-11-07 01:00 -05:00", false)]

        [InlineData("0 0    1   * * *", "2016-11-06 01:00 -04:00", "2016-11-06 01:00 -04:00", true)]
        [InlineData("0 0    1   * * *", "2016-11-06 01:00 -05:00", "2016-11-07 01:00 -05:00", true)]
        [InlineData("0 0    1   * * *", "2016-11-06 01:00 -04:00", "2016-11-07 01:00 -05:00", false)]

        [InlineData("0 0    1   6 11 *", "2015-11-07 01:00 -05:00", "2016-11-06 01:00 -04:00", true)]
        [InlineData("0 0    1   6 11 *", "2015-11-07 01:00 -05:00", "2016-11-06 01:00 -04:00", false)]

        [InlineData("0 0    1   * 11 SUN#1", "2015-11-01 01:00 -05:00", "2016-11-06 01:00 -04:00", true)]
        [InlineData("0 0    1   * 11 SUN#1", "2015-11-01 01:00 -05:00", "2016-11-06 01:00 -04:00", false)]

        // Run at 02:00 ST because 02:00 doesn't exist in DST.

        [InlineData("0 0 2 * * *", "2016-11-06 01:45 -04:00", "2016-11-06 02:00 -05:00", false)]
        [InlineData("0 0 2 * * *", "2016-11-06 01:45 -05:00", "2016-11-06 02:00 -05:00", false)]
        public void GetNextOccurrence_HandleDST_WhenTheClockJumpsBackward(string cronExpression, string fromString, string expectedString, bool inclusive)
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var fromInstant = GetInstant(fromString);
            var expectedInstant = GetInstant(expectedString);

            var executed = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive);

            Assert.Equal(expectedInstant, executed);
            Assert.Equal(expectedInstant.Offset, executed?.Offset);
        }

        [Fact]
        public void GetNextOccurrence_HandlesBorderConditions_WhenDSTEnds()
        {
            var expression = CronExpression.Parse("59 59 01 * * *", CronFormat.IncludeSeconds);

            var from = new DateTimeOffset(2016, 11, 06, 02, 00, 00, 00, TimeSpan.FromHours(-5)).AddTicks(-1);

            var executed = expression.GetNextOccurrence(from, EasternTimeZone, inclusive: true);

            Assert.Equal(new DateTimeOffset(2016, 11, 07, 01, 59, 59, 00, TimeSpan.FromHours(-5)), executed);
            Assert.Equal(TimeSpan.FromHours(-5), executed?.Offset);
        }

        [Theory]

        // 2017-04-02 is date when the clock jumps backward from 2:00 am -+11:00 DST to 1:30 am +10:30 ST on Lord Howe.
        // _______1:30 DST____1:59 DST -> 1:30 ST____2:00 ST_______

        // Run at 2:00 ST because 2:00 DST is invalid.
        [InlineData("0 */30 */2 * * *", "2017-04-02 01:30 +11:00", "2017-04-02 02:00 +10:30")]
        [InlineData("0 0    */2 * * *", "2017-04-02 00:30 +11:00", "2017-04-02 02:00 +10:30")]
        [InlineData("0 0    0/2 * * *", "2017-04-02 00:30 +11:00", "2017-04-02 02:00 +10:30")]
        [InlineData("0 0    2-3 * * *", "2017-04-02 00:30 +11:00", "2017-04-02 02:00 +10:30")]

        // Run twice due to intervals.
        [InlineData("0 */30 *   * * *", "2017-04-02 01:30 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 */30 *   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]
        [InlineData("0 */30 *   * * *", "2017-04-02 01:15 +10:30", "2017-04-02 01:30 +10:30")]

        [InlineData("0 30   *   * * *", "2017-04-02 01:30 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 30   *   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

        [InlineData("0 30   */1 * * *", "2017-04-02 01:30 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 30   */1 * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]
        [InlineData("0 30   0/1 * * *", "2017-04-02 01:30 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 30   0/1 * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

        [InlineData("0 30   1-9 * * *", "2017-04-02 01:30 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 30   1-9 * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

        [InlineData("0 */30 1   * * *", "2017-04-02 01:00 +11:00", "2017-04-02 01:00 +11:00")]
        [InlineData("0 */30 1   * * *", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 */30 1   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

        [InlineData("0 0/30 1   * * *", "2017-04-02 01:00 +11:00", "2017-04-02 01:00 +11:00")]
        [InlineData("0 0/30 1   * * *", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 0/30 1   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

        [InlineData("0 0-30 1   * * *", "2017-04-02 01:00 +11:00", "2017-04-02 01:00 +11:00")]
        [InlineData("0 0-30 1   * * *", "2017-04-02 01:20 +11:00", "2017-04-02 01:20 +11:00")]
        [InlineData("0 0-30 1   * * *", "2017-04-02 01:59 +11:00", "2017-04-02 01:30 +10:30")]

        [InlineData("*/30 30 1 * * *", "2017-04-02 00:30:00 +11:00", "2017-04-02 01:30:00 +11:00")]
        [InlineData("*/30 30 1 * * *", "2017-04-02 01:30:01 +11:00", "2017-04-02 01:30:30 +11:00")]
        [InlineData("*/30 30 1 * * *", "2017-04-02 01:30:31 +11:00", "2017-04-02 01:30:00 +10:30")]
        [InlineData("*/30 30 1 * * *", "2017-04-02 01:30:01 +10:30", "2017-04-02 01:30:30 +10:30")]
        [InlineData("*/30 30 1 * * *", "2017-04-02 01:30:31 +10:30", "2017-04-03 01:30:00 +10:30")]

        [InlineData("0/30 30 1 * * *", "2017-04-02 00:30:00 +11:00", "2017-04-02 01:30:00 +11:00")]
        [InlineData("0/30 30 1 * * *", "2017-04-02 01:30:01 +11:00", "2017-04-02 01:30:30 +11:00")]
        [InlineData("0/30 30 1 * * *", "2017-04-02 01:30:31 +11:00", "2017-04-02 01:30:00 +10:30")]
        [InlineData("0/30 30 1 * * *", "2017-04-02 01:30:01 +10:30", "2017-04-02 01:30:30 +10:30")]
        [InlineData("0/30 30 1 * * *", "2017-04-02 01:30:31 +10:30", "2017-04-03 01:30:00 +10:30")]

        [InlineData("0-30 30 1 * * *", "2017-04-02 00:30:00 +11:00", "2017-04-02 01:30:00 +11:00")]
        [InlineData("0-30 30 1 * * *", "2017-04-02 01:30:01 +11:00", "2017-04-02 01:30:01 +11:00")]
        [InlineData("0-30 30 1 * * *", "2017-04-02 01:30:31 +11:00", "2017-04-02 01:30:00 +10:30")]
        [InlineData("0-30 30 1 * * *", "2017-04-02 01:30:01 +10:30", "2017-04-02 01:30:01 +10:30")]
        [InlineData("0-30 30 1 * * *", "2017-04-02 01:30:31 +10:30", "2017-04-03 01:30:00 +10:30")]

        // Duplicates skipped due to certain time.
        [InlineData("0 0,30 1   * * *", "2017-04-02 01:00 +11:00", "2017-04-02 01:00 +11:00")]
        [InlineData("0 0,30 1   * * *", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 0,30 1   * * *", "2017-04-02 01:30 +10:30", "2017-04-03 01:00 +10:30")]

        [InlineData("0 0,30 1   * 2/2 *", "2017-04-02 01:00 +11:00", "2017-04-02 01:00 +11:00")]
        [InlineData("0 0,30 1   * 2/2 *", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 0,30 1   * 2/2 *", "2017-04-02 01:30 +10:30", "2017-04-03 01:00 +10:30")]

        [InlineData("0 0,30 1   2/1 1-12 0/1", "2017-04-02 01:00 +11:00", "2017-04-02 01:00 +11:00")]
        [InlineData("0 0,30 1   2/1 1-12 0/1", "2017-04-02 01:20 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 0,30 1   2/1 1-12 0/1", "2017-04-02 01:30 +10:30", "2017-04-03 01:00 +10:30")]

        [InlineData("0 30    1   * * *", "2017-04-02 01:30 +11:00", "2017-04-02 01:30 +11:00")]
        [InlineData("0 30    1   * * *", "2017-04-02 01:30 +10:30", "2017-04-03 01:30 +10:30")]
        public void GetNextOccurrence_HandleDST_WhenTheClockJumpsBackwardAndDeltaIsNotHour(string cronExpression, string fromString, string expectedString)
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var fromInstant = GetInstant(fromString);
            var expectedInstant = GetInstant(expectedString);

            var executed = expression.GetNextOccurrence(fromInstant, LordHoweTimeZone, inclusive: true);

            Assert.Equal(expectedInstant, executed);
            Assert.Equal(expectedInstant.Offset, executed?.Offset);
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
        public void GetNextOccurrence_ReturnsCorrectUtcDateTimeOffset(string cronExpression, string fromString, string expectedString)
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var fromInstant = GetInstantFromLocalTime(fromString, TimeZoneInfo.Utc);
            var expectedInstant = GetInstantFromLocalTime(expectedString, TimeZoneInfo.Utc);

            var occurrence = expression.GetNextOccurrence(fromInstant, TimeZoneInfo.Utc, inclusive: true);

            Assert.Equal(expectedInstant, occurrence);
            Assert.Equal(expectedInstant.Offset, occurrence?.Offset);
        }

        [Theory]

        // Dst doesn't affect result.

        [InlineData("0 */30 * * * *", "2016-03-12 23:15 -05:00", "2016-03-12 23:30 -05:00")]
        [InlineData("0 */30 * * * *", "2016-03-12 23:45 -05:00", "2016-03-13 00:00 -05:00")]
        [InlineData("0 */30 * * * *", "2016-03-13 00:15 -05:00", "2016-03-13 00:30 -05:00")]
        [InlineData("0 */30 * * * *", "2016-03-13 00:45 -05:00", "2016-03-13 01:00 -05:00")]
        [InlineData("0 */30 * * * *", "2016-03-13 01:45 -05:00", "2016-03-13 03:00 -04:00")]
        [InlineData("0 */30 * * * *", "2016-03-13 03:15 -04:00", "2016-03-13 03:30 -04:00")]
        [InlineData("0 */30 * * * *", "2016-03-13 03:45 -04:00", "2016-03-13 04:00 -04:00")]
        [InlineData("0 */30 * * * *", "2016-03-13 04:15 -04:00", "2016-03-13 04:30 -04:00")]
        [InlineData("0 */30 * * * *", "2016-03-13 04:45 -04:00", "2016-03-13 05:00 -04:00")]

        [InlineData("0 */30 * * * *", "2016-11-05 23:10 -04:00", "2016-11-05 23:30 -04:00")]
        [InlineData("0 */30 * * * *", "2016-11-05 23:50 -04:00", "2016-11-06 00:00 -04:00")]
        [InlineData("0 */30 * * * *", "2016-11-06 00:10 -04:00", "2016-11-06 00:30 -04:00")]
        [InlineData("0 */30 * * * *", "2016-11-06 00:50 -04:00", "2016-11-06 01:00 -04:00")]
        [InlineData("0 */30 * * * *", "2016-11-06 01:10 -04:00", "2016-11-06 01:30 -04:00")]
        [InlineData("0 */30 * * * *", "2016-11-06 01:50 -04:00", "2016-11-06 01:00 -05:00")]
        [InlineData("0 */30 * * * *", "2016-11-06 01:10 -05:00", "2016-11-06 01:30 -05:00")]
        [InlineData("0 */30 * * * *", "2016-11-06 01:50 -05:00", "2016-11-06 02:00 -05:00")]
        [InlineData("0 */30 * * * *", "2016-11-06 02:10 -05:00", "2016-11-06 02:30 -05:00")]
        [InlineData("0 */30 * * * *", "2016-11-06 02:50 -05:00", "2016-11-06 03:00 -05:00")]
        public void GetNextOccurrence_ReturnsCorrectDateTimeOffset(string cronExpression, string fromString, string expectedString)
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var fromInstant = GetInstant(fromString);
            var expectedInstant = GetInstant(expectedString);

            var occurrence = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive: true);

            Assert.Equal(expectedInstant, occurrence);
            Assert.Equal(expectedInstant.Offset, occurrence?.Offset);
        }

        [Theory]
        [InlineData("* * * * 4 *", "2099-12-13 00:00:00")]
        public void GetNextOccurrence_ReturnsNull_When_NextOccurrenceIsBeyondMaxValue(string cronExpression, string fromString)
        {
            var expression = CronExpression.Parse(cronExpression, CronFormat.IncludeSeconds);

            var fromWithOffset = GetInstantFromLocalTime(fromString, TimeZoneInfo.Utc);
            var fromUtc = fromWithOffset.UtcDateTime;

            var occurrenceDateTime = expression.GetNextOccurrence(fromUtc, TimeZoneInfo.Utc, inclusive: true);
            Assert.Null(occurrenceDateTime);

            var occurrenceWithOffset = expression.GetNextOccurrence(fromWithOffset, TimeZoneInfo.Utc);
            Assert.Null(occurrenceWithOffset);
        }

        [Theory]
        [InlineData("30 0 L  * *", "2017-03-30 23:59 +02:00", "2017-03-31 01:00 +03:00")]
        [InlineData("30 0 L  * *", "2017-03-31 01:00 +03:00", "2017-04-30 00:30 +03:00")]
        [InlineData("30 0 LW * *", "2018-03-29 23:59 +02:00", "2018-03-30 01:00 +03:00")]
        [InlineData("30 0 LW * *", "2018-03-30 01:00 +03:00", "2018-04-30 00:30 +03:00")]
        public void GetNextOccurrence_HandleDifficultDSTCases_WhenTheClockJumpsForwardOnFriday(string cronExpression, string fromString, string expectedString)
        {
            var expression = CronExpression.Parse(cronExpression);

            var fromInstant = GetInstant(fromString);
            var expectedInstant = GetInstant(expectedString);

            var occurrence = expression.GetNextOccurrence(fromInstant, JordanTimeZone, inclusive: true);

            // TODO: Rounding error.
            if (occurrence?.Millisecond == 999)
            {
                occurrence = occurrence.Value.AddMilliseconds(1);
            }

            Assert.Equal(expectedInstant, occurrence);
            Assert.Equal(expectedInstant.Offset, occurrence?.Offset);
        }

        [Theory]

        [InlineData("30 0 L  * *", "2014-10-31 00:30 +02:00", "2014-11-30 00:30 +02:00")]
        [InlineData("30 0 L  * *", "2014-10-31 00:30 +03:00", "2014-10-31 00:30 +03:00")]
        [InlineData("30 0 LW * *", "2015-10-30 00:30 +02:00", "2015-11-30 00:30 +02:00")]
        [InlineData("30 0 LW * *", "2015-10-30 00:30 +03:00", "2015-10-30 00:30 +03:00")]

        [InlineData("30 0 29 * *", "2019-03-28 23:59 +02:00", "2019-03-29 01:00 +03:00")]
        public void GetNextOccurrence_HandleDifficultDSTCases_WhenTheClockJumpsBackwardOnFriday(string cronExpression, string fromString, string expectedString)
        {
            var expression = CronExpression.Parse(cronExpression);

            var fromInstant = GetInstant(fromString);
            var expectedInstant = GetInstant(expectedString);

            var occurrence = expression.GetNextOccurrence(fromInstant, JordanTimeZone, inclusive: true);

            // TODO: Rounding error.
            if (occurrence?.Millisecond == 999)
            {
                occurrence = occurrence.Value.AddMilliseconds(1);
            }

            Assert.Equal(expectedInstant, occurrence);
            Assert.Equal(expectedInstant.Offset, occurrence?.Offset);
        }

        [Theory]
        [MemberData(nameof(GetTimeZones))]
        public void GetNextOccurrence_ReturnsTheSameDateTimeWithGivenTimeZoneOffset(TimeZoneInfo zone)
        {
            var fromInstant = new DateTimeOffset(2017, 03, 04, 00, 00, 00, new TimeSpan(12, 30, 00));
            var expectedInstant = fromInstant;

            var expectedOffset = zone.GetUtcOffset(expectedInstant);

            var occurrence = MinutelyExpression.GetNextOccurrence(fromInstant, zone, inclusive: true);

            Assert.Equal(expectedInstant, occurrence);
            Assert.Equal(expectedOffset, occurrence?.Offset);
        }

        [Theory]
        [MemberData(nameof(GetTimeZones))]
        public void GetNextOccurrence_ReturnsUtcDateTime(TimeZoneInfo zone)
        {
            var from = new DateTime(2017, 03, 06, 00, 00, 00, DateTimeKind.Utc);

            var occurrence = MinutelyExpression.GetNextOccurrence(from, zone, inclusive: true);

            Assert.Equal(from, occurrence);
            Assert.Equal(DateTimeKind.Utc, occurrence?.Kind);
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
        [InlineData("* * 1-28  *    SUN#5", "1970-01-01")]

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
        public void GetNextOccurrence_ReturnsNull_WhenCronExpressionIsUnreachable(string cronExpression, string fromString)
        {
            var expression = CronExpression.Parse(cronExpression);

            var fromInstant = GetInstantFromLocalTime(fromString, EasternTimeZone);

            var occurrence = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive: true);

            Assert.Null(occurrence);
        }

        [Theory]
        [InlineData("* * 30   2  *", "2080-01-01")]
        [InlineData("* * L-30 11 *", "2080-01-01")]
        public void GetNextOccurrence_ReturnsNull_WhenCronExpressionIsUnreachableAndFromIsDateTime(string cronExpression, string fromString)
        {
            var expression = CronExpression.Parse(cronExpression);

            var fromInstant = GetInstantFromLocalTime(fromString, TimeZoneInfo.Utc);

            var occurrence = expression.GetNextOccurrence(fromInstant.UtcDateTime);

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

        // Support 'L' character in day of month field.

        [InlineData("* * L * *", "2016-01-05", "2016-01-31")]
        [InlineData("* * L * *", "2016-01-31", "2016-01-31")]
        [InlineData("* * L * *", "2016-02-05", "2016-02-29")]
        [InlineData("* * L * *", "2016-02-29", "2016-02-29")]
        [InlineData("* * L 2 *", "2016-02-29", "2016-02-29")]
        [InlineData("* * L * *", "2017-02-28", "2017-02-28")]
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

        [InlineData("? ? ? ? ?", "2016-12-09 16:46", "2016-12-09 16:46")]
        [InlineData("* * * * ?", "2016-12-09 16:46", "2016-12-09 16:46")]
        [InlineData("* * ? * *", "2016-03-09 16:46", "2016-03-09 16:46")]
        [InlineData("* * * * ?", "2016-12-30 16:46", "2016-12-30 16:46")]
        [InlineData("* * ? * *", "2016-12-09 02:46", "2016-12-09 02:46")]
        [InlineData("* * * * ?", "2016-12-09 16:09", "2016-12-09 16:09")]
        [InlineData("* * ? * *", "2099-12-09 16:46", "2099-12-09 16:46")]
        public void GetNextOccurrence_ReturnsCorrectDate_WhenExpressionContains5FieldsAndInclusiveIsTrue(string cronExpression, string fromString, string expectedString)
        {
            var expression = CronExpression.Parse(cronExpression);

            var fromInstant = GetInstantFromLocalTime(fromString, EasternTimeZone);

            var occurrence = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive: true);

            Assert.Equal(GetInstantFromLocalTime(expectedString, EasternTimeZone), occurrence);
        }

        [Theory]

        [InlineData("@every_second", "2017-03-23 16:46:05", "2017-03-23 16:46:05")]

        [InlineData("@every_minute", "2017-03-23 16:46", "2017-03-23 16:46")]
        [InlineData("@hourly      ", "2017-03-23 16:46", "2017-03-23 17:00")]
        [InlineData("@daily       ", "2017-03-23 16:46", "2017-03-24 00:00")]
        [InlineData("@midnight    ", "2017-03-23 16:46", "2017-03-24 00:00")]
        [InlineData("@monthly     ", "2017-03-23 16:46", "2017-04-01 00:00")]
        [InlineData("@yearly      ", "2017-03-23 16:46", "2018-01-01 00:00")]
        [InlineData("@annually    ", "2017-03-23 16:46", "2018-01-01 00:00")]

        // Case-insensitive.
        [InlineData("@EVERY_SECOND", "2017-03-23 16:46:05", "2017-03-23 16:46:05")]

        [InlineData("@EVERY_MINUTE", "2017-03-23 16:46", "2017-03-23 16:46")]
        [InlineData("@HOURLY      ", "2017-03-23 16:46", "2017-03-23 17:00")]
        [InlineData("@DAILY       ", "2017-03-23 16:46", "2017-03-24 00:00")]
        [InlineData("@MIDNIGHT    ", "2017-03-23 16:46", "2017-03-24 00:00")]
        [InlineData("@MONTHLY     ", "2017-03-23 16:46", "2017-04-01 00:00")]
        [InlineData("@YEARLY      ", "2017-03-23 16:46", "2018-01-01 00:00")]
        [InlineData("@ANNUALLY    ", "2017-03-23 16:46", "2018-01-01 00:00")]
        public void GetNextOccurrence_ReturnsCorrectDate_WhenExpressionIsMacros(string cronExpression, string fromString, string expectedString)
        {
            var expression = CronExpression.Parse(cronExpression);

            var fromInstant = GetInstantFromLocalTime(fromString, EasternTimeZone);

            var occurrence = expression.GetNextOccurrence(fromInstant, EasternTimeZone, inclusive: true);

            Assert.Equal(GetInstantFromLocalTime(expectedString, EasternTimeZone), occurrence);
        }

        [Theory]
        [InlineData("* * * * *", "2017-03-16 16:00", "2017-03-16 16:01")]
        [InlineData("5 * * * *", "2017-03-16 16:05", "2017-03-16 17:05")]
        [InlineData("* 5 * * *", "2017-03-16 05:00", "2017-03-16 05:01")]
        [InlineData("* * 5 * *", "2017-03-05 16:00", "2017-03-05 16:01")]
        [InlineData("* * * 5 *", "2017-05-16 16:00", "2017-05-16 16:01")]
        [InlineData("* * * * 5", "2017-03-17 16:00", "2017-03-17 16:01")]
        [InlineData("5 5 * * *", "2017-03-16 05:05", "2017-03-17 05:05")]
        [InlineData("5 5 5 * *", "2017-03-05 05:05", "2017-04-05 05:05")]
        [InlineData("5 5 5 5 *", "2017-05-05 05:05", "2018-05-05 05:05")]
        [InlineData("5 5 5 5 5", "2017-05-05 05:05", "2023-05-05 05:05")]
        public void GetNextOccurrence_ReturnsCorrectDate_WhenFromIsDateTimeOffsetAndInclusiveIsFalse(string expression, string from, string expectedString)
        {
            var cronExpression = CronExpression.Parse(expression);

            var fromInstant = GetInstantFromLocalTime(from, EasternTimeZone);

            var nextOccurrence = cronExpression.GetNextOccurrence(fromInstant, EasternTimeZone);

            Assert.Equal(GetInstantFromLocalTime(expectedString, EasternTimeZone), nextOccurrence);
        }

        [Theory]
        [InlineData("* * * * *", "2017-03-16 16:00", "2017-03-16 16:01")]
        [InlineData("5 * * * *", "2017-03-16 16:05", "2017-03-16 17:05")]
        [InlineData("* 5 * * *", "2017-03-16 05:00", "2017-03-16 05:01")]
        [InlineData("* * 5 * *", "2017-03-05 16:00", "2017-03-05 16:01")]
        [InlineData("* * * 5 *", "2017-05-16 16:00", "2017-05-16 16:01")]
        [InlineData("* * * * 5", "2017-03-17 16:00", "2017-03-17 16:01")]
        [InlineData("5 5 * * *", "2017-03-16 05:05", "2017-03-17 05:05")]
        [InlineData("5 5 5 * *", "2017-03-05 05:05", "2017-04-05 05:05")]
        [InlineData("5 5 5 5 *", "2017-05-05 05:05", "2018-05-05 05:05")]
        [InlineData("5 5 5 5 5", "2017-05-05 05:05", "2023-05-05 05:05")]
        public void GetNextOccurrence_ReturnsCorrectDate_WhenFromIsDateTimeAndZoneIsSpecifiedAndInclusiveIsFalse(string expression, string fromString, string expectedString)
        {
            var cronExpression = CronExpression.Parse(expression);

            var fromInstant = GetInstantFromLocalTime(fromString, EasternTimeZone);
            var expectedInstant = GetInstantFromLocalTime(expectedString, EasternTimeZone);

            var nextOccurrence = cronExpression.GetNextOccurrence(fromInstant.UtcDateTime, EasternTimeZone);

            Assert.Equal(expectedInstant.UtcDateTime, nextOccurrence);
        }

        [Theory]
        [InlineData("* * * * *", "2017-03-16 16:00", "2017-03-16 16:01")]
        [InlineData("5 * * * *", "2017-03-16 16:05", "2017-03-16 17:05")]
        [InlineData("* 5 * * *", "2017-03-16 05:00", "2017-03-16 05:01")]
        [InlineData("* * 5 * *", "2017-03-05 16:00", "2017-03-05 16:01")]
        [InlineData("* * * 5 *", "2017-05-16 16:00", "2017-05-16 16:01")]
        [InlineData("* * * * 5", "2017-03-17 16:00", "2017-03-17 16:01")]
        [InlineData("5 5 * * *", "2017-03-16 05:05", "2017-03-17 05:05")]
        [InlineData("5 5 5 * *", "2017-03-05 05:05", "2017-04-05 05:05")]
        [InlineData("5 5 5 5 *", "2017-05-05 05:05", "2018-05-05 05:05")]
        [InlineData("5 5 5 5 5", "2017-05-05 05:05", "2023-05-05 05:05")]
        public void GetNextOccurrence_ReturnsCorrectDate_WhenFromIsUtcDateTimeAndInclusiveIsFalse(string expression, string fromString, string expectedString)
        {
            var cronExpression = CronExpression.Parse(expression);

            var fromInstant = GetInstantFromLocalTime(fromString, TimeZoneInfo.Utc);
            var expectedInstant = GetInstantFromLocalTime(expectedString, TimeZoneInfo.Utc);

            var nextOccurrence = cronExpression.GetNextOccurrence(fromInstant.UtcDateTime);

            Assert.Equal(expectedInstant.UtcDateTime, nextOccurrence);
        }

        [Theory]
        [InlineData("* * * * * *", "2017-03-16 16:00:00", "2017-03-16 16:00:01")]
        [InlineData("5 * * * * *", "2017-03-16 16:00:05", "2017-03-16 16:01:05")]
        [InlineData("* 5 * * * *", "2017-03-16 16:05:00", "2017-03-16 16:05:01")]
        [InlineData("* * 5 * * *", "2017-03-16 05:00:00", "2017-03-16 05:00:01")]
        [InlineData("* * * 5 * *", "2017-03-05 16:00:00", "2017-03-05 16:00:01")]
        [InlineData("* * * * 5 *", "2017-05-16 16:00:00", "2017-05-16 16:00:01")]
        [InlineData("* * * * * 5", "2017-03-17 16:00:00", "2017-03-17 16:00:01")]
        [InlineData("5 5 * * * *", "2017-03-16 16:05:05", "2017-03-16 17:05:05")]
        [InlineData("5 5 5 * * *", "2017-03-16 05:05:05", "2017-03-17 05:05:05")]
        [InlineData("5 5 5 5 * *", "2017-03-05 05:05:05", "2017-04-05 05:05:05")]
        [InlineData("5 5 5 5 5 *", "2017-05-05 05:05:05", "2018-05-05 05:05:05")]
        [InlineData("5 5 5 5 5 5", "2017-05-05 05:05:05", "2023-05-05 05:05:05")]
        public void GetNextOccurrence_ReturnsCorrectDate_When6fieldsExpressionIsUsedAndInclusiveIsFalse(string expression, string fromString, string expectedString)
        {
            var cronExpression = CronExpression.Parse(expression, CronFormat.IncludeSeconds);

            var from = GetInstantFromLocalTime(fromString, EasternTimeZone);

            var nextOccurrence = cronExpression.GetNextOccurrence(from, EasternTimeZone);

            Assert.Equal(GetInstantFromLocalTime(expectedString, EasternTimeZone), nextOccurrence);
        }

        [Fact]
        public void GetOccurrences_DateTime_ThrowsAnException_WhenFromGreaterThanTo()
        {
            var expression = CronExpression.Parse("* * * * *");
            Assert.Throws<ArgumentException>(
                () => expression.GetOccurrences(DateTime.UtcNow, DateTime.UtcNow.AddHours(-5)).ToArray());
        }

        [Fact]
        public void GetOccurrences_DateTime_ReturnsEmptyEnumerable_WhenNoOccurrencesFound()
        {
            var expression = CronExpression.Parse("* * 30 FEB *");

            var occurrences = expression.GetOccurrences(
                DateTime.UtcNow, 
                DateTime.UtcNow.AddYears(1));

            Assert.Empty(occurrences);
        }

        [Fact]
        public void GetOccurrences_DateTime_ReturnsCollectionThatDoesNotIncludeToByDefault()
        {
            var expression = CronExpression.Parse("* 00 26 04 *");
            var from = new DateTime(2017, 04, 26, 00, 00, 00, 000, DateTimeKind.Utc);

            var occurrences = expression
                .GetOccurrences(from, from.AddMinutes(2))
                .ToArray();

            Assert.Equal(2, occurrences.Length);
            Assert.Equal(from, occurrences[0]);
            Assert.Equal(from.AddMinutes(1), occurrences[1]);
        }

        [Fact]
        public void GetOccurrences_DateTime_HandlesFromExclusiveArgument()
        {
            var expression = CronExpression.Parse("* 00 26 04 *");
            var from = new DateTime(2017, 04, 26, 00, 00, 00, 000, DateTimeKind.Utc);

            var occurrences = expression
                .GetOccurrences(from, from.AddMinutes(2), fromInclusive: false)
                .ToArray();

            Assert.Single(occurrences);
            Assert.Equal(from.AddMinutes(1), occurrences[0]);
        }

        [Fact]
        public void GetOccurrences_DateTime_HandlesToInclusiveArgument()
        {
            var expression = CronExpression.Parse("* 00 26 04 *");
            var from = new DateTime(2017, 04, 26, 00, 00, 00, 000, DateTimeKind.Utc);

            var occurrences = expression
                .GetOccurrences(from, from.AddMinutes(2), toInclusive: true)
                .ToArray();

            Assert.Equal(3, occurrences.Length);
            Assert.Equal(from.AddMinutes(2), occurrences[2]);
        }

        [Fact]
        public void GetOccurrences_DateTimeTimeZone_ThrowsAnException_WhenFromGreaterThanTo()
        {
            var expression = CronExpression.Parse("* * * * *");
            Assert.Throws<ArgumentException>(
                () => expression.GetOccurrences(DateTime.UtcNow, DateTime.UtcNow.AddHours(-5), EasternTimeZone).ToArray());
        }

        [Fact]
        public void GetOccurrences_DateTimeTimeZone_ReturnsEmptyEnumerable_WhenNoOccurrencesFound()
        {
            var expression = CronExpression.Parse("* * 30 FEB *");

            var occurrences = expression.GetOccurrences(
                DateTime.UtcNow, 
                DateTime.UtcNow.AddYears(1), 
                EasternTimeZone);

            Assert.Empty(occurrences);
        }

        [Fact]
        public void GetOccurrences_DateTimeTimeZone_ReturnsCollectionThatDoesNotIncludeToByDefault()
        {
            var expression = CronExpression.Parse("* 20 25 04 *");
            var from = new DateTime(2017, 04, 26, 00, 00, 00, 000, DateTimeKind.Utc);

            var occurrences = expression
                .GetOccurrences(from, from.AddMinutes(2), EasternTimeZone)
                .ToArray();

            Assert.Equal(2, occurrences.Length);
            Assert.Equal(from, occurrences[0]);
            Assert.Equal(from.AddMinutes(1), occurrences[1]);
        }

        [Fact]
        public void GetOccurrences_DateTimeTimeZone_HandlesFromExclusiveArgument()
        {
            var expression = CronExpression.Parse("* 20 25 04 *");
            var from = new DateTime(2017, 04, 26, 00, 00, 00, 000, DateTimeKind.Utc);

            var occurrences = expression
                .GetOccurrences(from, from.AddMinutes(2), EasternTimeZone, fromInclusive: false)
                .ToArray();

            Assert.Single(occurrences);
            Assert.Equal(from.AddMinutes(1), occurrences[0]);
        }

        [Fact]
        public void GetOccurrences_DateTimeTimeZone_HandlesToInclusiveArgument()
        {
            var expression = CronExpression.Parse("* 20 25 04 *");
            var from = new DateTime(2017, 04, 26, 00, 00, 00, 000, DateTimeKind.Utc);

            var occurrences = expression
                .GetOccurrences(from, from.AddMinutes(2), EasternTimeZone, toInclusive: true)
                .ToArray();

            Assert.Equal(3, occurrences.Length);
            Assert.Equal(from.AddMinutes(2), occurrences[2]);
        }

        [Fact]
        public void GetOccurrences_DateTimeOffset_ThrowsAnException_WhenFromGreaterThanTo()
        {
            var expression = CronExpression.Parse("* * * * *");
            Assert.Throws<ArgumentException>(
                () => expression.GetOccurrences(DateTimeOffset.Now, DateTimeOffset.Now.AddHours(-5), EasternTimeZone).ToArray());
        }

        [Fact]
        public void GetOccurrences_DateTimeOffset_ReturnsEmptyEnumerable_WhenNoOccurrencesFound()
        {
            var expression = CronExpression.Parse("* * 30 FEB *");

            var occurrences = expression.GetOccurrences(
                DateTimeOffset.Now, 
                DateTimeOffset.Now.AddYears(1), 
                EasternTimeZone);

            Assert.Empty(occurrences);
        }

        [Fact]
        public void GetOccurrences_DateTimeOffset_ReturnsCollectionThatDoesNotIncludeToByDefault()
        {
            var expression = CronExpression.Parse("* 20 25 04 *");
            var from = new DateTimeOffset(2017, 04, 26, 00, 00, 00, 000, TimeSpan.Zero);

            var occurrences = expression
                .GetOccurrences(from, from.AddMinutes(2), EasternTimeZone)
                .ToArray();

            Assert.Equal(2, occurrences.Length);
            Assert.Equal(from, occurrences[0]);
            Assert.Equal(from.AddMinutes(1), occurrences[1].UtcDateTime);
        }

        [Fact]
        public void GetOccurrences_DateTimeOffset_HandlesFromExclusiveArgument()
        {
            var expression = CronExpression.Parse("* 20 25 04 *");
            var from = new DateTimeOffset(2017, 04, 26, 00, 00, 00, 000, TimeSpan.Zero);

            var occurrences = expression
                .GetOccurrences(from, from.AddMinutes(2), EasternTimeZone, fromInclusive: false)
                .ToArray();

            Assert.Single(occurrences);
            Assert.Equal(from.AddMinutes(1), occurrences[0].UtcDateTime);
        }

        [Fact]
        public void GetOccurrences_DateTimeOffset_HandlesToInclusiveArgument()
        {
            var expression = CronExpression.Parse("* 20 25 04 *");
            var from = new DateTimeOffset(2017, 04, 26, 00, 00, 00, 000, TimeSpan.Zero);

            var occurrences = expression
                .GetOccurrences(from, from.AddMinutes(2), EasternTimeZone, toInclusive: true)
                .ToArray();

            Assert.Equal(3, occurrences.Length);
            Assert.Equal(from.AddMinutes(2), occurrences[2].UtcDateTime);
        }

        [Theory]
        [InlineData("* * * * *", "* * * * *")]

        [InlineData("* * * * *", "0/2,1/2    * * * *")]
        [InlineData("* * * * *", "1/2,0-59/2 * * * *")]
        [InlineData("* * * * *", "0-59       * * * *")]
        [InlineData("* * * * *", "0,1,2-59   * * * *")]
        [InlineData("* * * * *", "0-59/1     * * * *")]
        [InlineData("* * * * *", "50-49      * * * *")]

        [InlineData("* * * * *", "* 0/3,2/3,1/3 * * *")]
        [InlineData("* * * * *", "* 0-23/2,1/2  * * *")]
        [InlineData("* * * * *", "* 0-23        * * *")]
        [InlineData("* * * * *", "* 0-23/1      * * *")]
        [InlineData("* * * * *", "* 12-11       * * *")]

        [InlineData("* * * * *", "* * 1/2,2/2     * *")]
        [InlineData("* * * * *", "* * 1-31/2,2/2  * *")]
        [InlineData("* * * * *", "* * 1-31        * *")]
        [InlineData("* * * * *", "* * 1-31/1      * *")]
        [InlineData("* * * * *", "* * 5-4         * *")]

        [InlineData("* * * * *", "* * * 1/2,2/2    *")]
        [InlineData("* * * * *", "* * * 1-12/2,2/2 *")]
        [InlineData("* * * * *", "* * * 1-12       *")]
        [InlineData("* * * * *", "* * * 1-12/1     *")]
        [InlineData("* * * * *", "* * * 12-11      *")]

        [InlineData("* * * * *", "* * * * 0/2,1/2    ")]
        [InlineData("* * * * *", "* * * * SUN/2,MON/2")]
        [InlineData("* * * * *", "* * * * 0-6/2,1/2  ")]
        [InlineData("* * * * *", "* * * * 0-7/2,1/2  ")]
        [InlineData("* * * * *", "* * * * 0-7/2,MON/2")]
        [InlineData("* * * * *", "* * * * 0-6        ")]
        [InlineData("* * * * *", "* * * * 0-7        ")]
        [InlineData("* * * * *", "* * * * SUN-SAT    ")]
        [InlineData("* * * * *", "* * * * 0-6/1      ")]
        [InlineData("* * * * *", "* * * * 0-7/1      ")]
        [InlineData("* * * * *", "* * * * SUN-SAT/1  ")]
        [InlineData("* * * * *", "* * * * MON-SUN    ")]

        [InlineData("* * *     * 0  ", "* * *     * 7  ")]
        [InlineData("* * *     * 0  ", "* * *     * SUN")]
        [InlineData("* * LW    * *  ", "* * LW    * *  ")]
        [InlineData("* * L-20W * 2  ", "* * L-20W * 2  ")]
        [InlineData("* * *     * 0#1", "* * *     * 0#1")]
        [InlineData("* * *     * 0L ", "* * *     * 7L ")]
        [InlineData("* * L-3W  * 0L ", "* * L-3W  * 0L ")]
        [InlineData("1 1 1     1 1  ", "1 1 1     1 1  ")]
        [InlineData("* * *     * *  ", "* * ?     * *  ")]
        [InlineData("* * *     * *  ", "* * *     * ?  ")]

        [InlineData("1-5 * * * *", "1-5 * * * *")]
        [InlineData("1-5 * * * *", "1-5/1 * * * *")]
        [InlineData("* * * * *", "0/1 * * * *")]
        [InlineData("1 * * * *", "1-1 * * * *")]
        [InlineData("*/4 * * * *", "0-59/4 * * * *")]

        [InlineData("1-5 1-5 1-5 1-5 1-5", "1-5 1-5 1-5 1-5 1-5")]

        [InlineData("50-15 * * * *", "50-15      * * * *")]
        [InlineData("50-15 * * * *", "0-15,50-59 * * * *")]

        [InlineData("* 20-15 * * *", "* 20-15      * * *")]
        [InlineData("* 20-15 * * *", "* 0-15,20-23 * * *")]

        [InlineData("* * 20-15 * *", "* * 20-15      * *")]
        [InlineData("* * 20-15 * *", "* * 1-15,20-31 * *")]

        [InlineData("* * * 10-3 *", "* * * 10-3      *")]
        [InlineData("* * * 10-3 *", "* * * 1-3,10-12 *")]

        [InlineData("* * * * 5-2", "* * * * 5-2    ")]
        [InlineData("* * * * 5-2", "* * * * 0-2,5-7")]
        [InlineData("* * * * 5-2", "* * * * 0-2,5-6")]
        [InlineData("* * * * 5-2", "* * * * 1-2,5-7")]

        [InlineData("* * * * FRI-TUE", "* * * * FRI-TUE        ")]
        [InlineData("* * * * FRI-TUE", "* * * * SUN-TUE,FRI-SUN")]
        [InlineData("* * * * FRI-TUE", "* * * * SUN-TUE,FRI-SAT")]
        [InlineData("* * * * FRI-TUE", "* * * * MON-TUE,FRI-SUN")]
        public void Equals_ReturnsTrue_WhenCronExpressionsAreEqual(string leftExpression, string rightExpression)
        {
            var leftCronExpression = CronExpression.Parse(leftExpression);
            var rightCronExpression = CronExpression.Parse(rightExpression);

            Assert.True(leftCronExpression.Equals(rightCronExpression));
            Assert.True(leftCronExpression == rightCronExpression);
            Assert.False(leftCronExpression != rightCronExpression);
            Assert.True(leftCronExpression.GetHashCode() == rightCronExpression.GetHashCode());
        }

        [Fact]
        public void Equals_ReturnsFalse_WhenOtherIsNull()
        {
            var cronExpression = CronExpression.Parse("* * * * *");

            Assert.False(cronExpression.Equals(null));
            Assert.False(cronExpression == null);
        }

        [Theory]
        [InlineData("1 1 1 1 1", "2 1 1 1 1")]
        [InlineData("1 1 1 1 1", "1 2 1 1 1")]
        [InlineData("1 1 1 1 1", "1 1 2 1 1")]
        [InlineData("1 1 1 1 1", "1 1 1 2 1")]
        [InlineData("1 1 1 1 1", "1 1 1 1 2")]
        [InlineData("* * * * *", "1 1 1 1 1")]

        [InlineData("* * 31 1 *", "* * L    1 *")]
        [InlineData("* * L  * *", "* * LW   * *")]
        [InlineData("* * LW * *", "* * L-1W * *")]
        [InlineData("* * *  * 0", "* * L-1W * 0#1")]
        public void Equals_ReturnsFalse_WhenCronExpressionsAreNotEqual(string leftExpression, string rightExpression)
        {
            var leftCronExpression = CronExpression.Parse(leftExpression);
            var rightCronExpression = CronExpression.Parse(rightExpression);

            Assert.False(leftCronExpression.Equals(rightCronExpression));
            Assert.True(leftCronExpression != rightCronExpression);
            Assert.True(leftCronExpression.GetHashCode() != rightCronExpression.GetHashCode());
        }

        [Theory]

        // Second specified.
        
        [InlineData("*      * * * * *", "*                * * * * *", CronFormat.IncludeSeconds)]
        [InlineData("0      * * * * *", "0                * * * * *", CronFormat.IncludeSeconds)]
        [InlineData("1,2    * * * * *", "1,2              * * * * *", CronFormat.IncludeSeconds)]
        [InlineData("1-3    * * * * *", "1,2,3            * * * * *", CronFormat.IncludeSeconds)]
        [InlineData("57-3   * * * * *", "0,1,2,3,57,58,59 * * * * *", CronFormat.IncludeSeconds)]
        [InlineData("*/10   * * * * *", "0,10,20,30,40,50 * * * * *", CronFormat.IncludeSeconds)]
        [InlineData("0/10   * * * * *", "0,10,20,30,40,50 * * * * *", CronFormat.IncludeSeconds)]
        [InlineData("0-20/5 * * * * *", "0,5,10,15,20     * * * * *", CronFormat.IncludeSeconds)]

        [InlineData("10,56-3/2 * * * * *", "0,2,10,56,58 * * * * *", CronFormat.IncludeSeconds)]

        // Minute specified.
        
        [InlineData("*      * * * *", "0 *                * * * *", CronFormat.Standard)]
        [InlineData("0      * * * *", "0 0                * * * *", CronFormat.Standard)]
        [InlineData("1,2    * * * *", "0 1,2              * * * *", CronFormat.Standard)]
        [InlineData("1-3    * * * *", "0 1,2,3            * * * *", CronFormat.Standard)]
        [InlineData("57-3   * * * *", "0 0,1,2,3,57,58,59 * * * *", CronFormat.Standard)]
        [InlineData("*/10   * * * *", "0 0,10,20,30,40,50 * * * *", CronFormat.Standard)]
        [InlineData("0/10   * * * *", "0 0,10,20,30,40,50 * * * *", CronFormat.Standard)]
        [InlineData("0-20/5 * * * *", "0 0,5,10,15,20     * * * *", CronFormat.Standard)]

        [InlineData("10,56-3/2 * * * *", "0 0,2,10,56,58 * * * *", CronFormat.Standard)]

        [InlineData("* *      * * * *", "* *                * * * *", CronFormat.IncludeSeconds)]
        [InlineData("* 0      * * * *", "* 0                * * * *", CronFormat.IncludeSeconds)]
        [InlineData("* 1,2    * * * *", "* 1,2              * * * *", CronFormat.IncludeSeconds)]
        [InlineData("* 1-3    * * * *", "* 1,2,3            * * * *", CronFormat.IncludeSeconds)]
        [InlineData("* 57-3   * * * *", "* 0,1,2,3,57,58,59 * * * *", CronFormat.IncludeSeconds)]
        [InlineData("* */10   * * * *", "* 0,10,20,30,40,50 * * * *", CronFormat.IncludeSeconds)]
        [InlineData("* 0/10   * * * *", "* 0,10,20,30,40,50 * * * *", CronFormat.IncludeSeconds)]
        [InlineData("* 0-20/5 * * * *", "* 0,5,10,15,20     * * * *", CronFormat.IncludeSeconds)]

        [InlineData("* 10,56-3/2 * * * *", "* 0,2,10,56,58 * * * *", CronFormat.IncludeSeconds)]

        // Hour specified.
        
        [InlineData("* *      * * *", "0 * *             * * *", CronFormat.Standard)]
        [InlineData("* 0      * * *", "0 * 0             * * *", CronFormat.Standard)]
        [InlineData("* 1,2    * * *", "0 * 1,2           * * *", CronFormat.Standard)]
        [InlineData("* 1-3    * * *", "0 * 1,2,3         * * *", CronFormat.Standard)]
        [InlineData("* 22-3   * * *", "0 * 0,1,2,3,22,23 * * *", CronFormat.Standard)]
        [InlineData("* */10   * * *", "0 * 0,10,20       * * *", CronFormat.Standard)]
        [InlineData("* 0/10   * * *", "0 * 0,10,20       * * *", CronFormat.Standard)]
        [InlineData("* 0-20/5 * * *", "0 * 0,5,10,15,20  * * *", CronFormat.Standard)]

        [InlineData("* 10,22-3/2 * * *", "0 * 0,2,10,22    * * *", CronFormat.Standard)]

        [InlineData("* * *      * * *", "* * *             * * *", CronFormat.IncludeSeconds)]
        [InlineData("* * 0      * * *", "* * 0             * * *", CronFormat.IncludeSeconds)]
        [InlineData("* * 1,2    * * *", "* * 1,2           * * *", CronFormat.IncludeSeconds)]
        [InlineData("* * 1-3    * * *", "* * 1,2,3         * * *", CronFormat.IncludeSeconds)]
        [InlineData("* * 22-3   * * *", "* * 0,1,2,3,22,23 * * *", CronFormat.IncludeSeconds)]
        [InlineData("* * */10   * * *", "* * 0,10,20       * * *", CronFormat.IncludeSeconds)]
        [InlineData("* * 0/10   * * *", "* * 0,10,20       * * *", CronFormat.IncludeSeconds)]
        [InlineData("* * 0-20/5 * * *", "* * 0,5,10,15,20  * * *", CronFormat.IncludeSeconds)]

        [InlineData("* * 10,22-3/2 * * *", "* * 0,2,10,22    * * *", CronFormat.IncludeSeconds)]

        // Day specified.
        
        [InlineData("* * *      * *", "0 * * *           * *", CronFormat.Standard)]
        [InlineData("* * 1      * *", "0 * * 1           * *", CronFormat.Standard)]
        [InlineData("* * 1,2    * *", "0 * * 1,2         * *", CronFormat.Standard)]
        [InlineData("* * 1-3    * *", "0 * * 1,2,3       * *", CronFormat.Standard)]
        [InlineData("* * 30-3   * *", "0 * * 1,2,3,30,31 * *", CronFormat.Standard)]
        [InlineData("* * */10   * *", "0 * * 1,11,21,31  * *", CronFormat.Standard)]
        [InlineData("* * 1/10   * *", "0 * * 1,11,21,31  * *", CronFormat.Standard)]
        [InlineData("* * 1-20/5 * *", "0 * * 1,6,11,16   * *", CronFormat.Standard)]

        [InlineData("* * L     * *", "0 * * L     * *", CronFormat.Standard)]
        [InlineData("* * L-0   * *", "0 * * L     * *", CronFormat.Standard)]
        [InlineData("* * L-10  * *", "0 * * L-10  * *", CronFormat.Standard)]
        [InlineData("* * LW    * *", "0 * * LW    * *", CronFormat.Standard)]
        [InlineData("* * L-0W  * *", "0 * * LW    * *", CronFormat.Standard)]
        [InlineData("* * L-10W * *", "0 * * L-10W * *", CronFormat.Standard)]
        [InlineData("* * 10W   * *", "0 * * 10W   * *", CronFormat.Standard)]

        [InlineData("* * 10,29-3/2 * *", "0 * * 2,10,29,31 * *", CronFormat.Standard)]

        [InlineData("* * * *      * *", "* * * *           * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * 1      * *", "* * * 1           * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * 1,2    * *", "* * * 1,2         * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * 1-3    * *", "* * * 1,2,3       * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * 30-3   * *", "* * * 1,2,3,30,31 * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * */10   * *", "* * * 1,11,21,31  * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * 1/10   * *", "* * * 1,11,21,31  * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * 1-20/5 * *", "* * * 1,6,11,16   * *", CronFormat.IncludeSeconds)]

        [InlineData("* * * L     * *", "* * * L     * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * L-0   * *", "* * * L     * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * L-10  * *", "* * * L-10  * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * LW    * *", "* * * LW    * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * L-0W  * *", "* * * LW    * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * L-10W * *", "* * * L-10W * *", CronFormat.IncludeSeconds)]
        [InlineData("* * * 10W   * *", "* * * 10W   * *", CronFormat.IncludeSeconds)]

        [InlineData("* * * 10,29-3/2 * *", "* * * 2,10,29,31 * *", CronFormat.IncludeSeconds)]

        // Month specified.
        
        [InlineData("* * * *      *", "0 * * * *           *", CronFormat.Standard)]
        [InlineData("* * * 1      *", "0 * * * 1           *", CronFormat.Standard)]
        [InlineData("* * * 1,2    *", "0 * * * 1,2         *", CronFormat.Standard)]
        [InlineData("* * * 1-3    *", "0 * * * 1,2,3       *", CronFormat.Standard)]
        [InlineData("* * * 11-3   *", "0 * * * 1,2,3,11,12 *", CronFormat.Standard)]
        [InlineData("* * * */10   *", "0 * * * 1,11        *", CronFormat.Standard)]
        [InlineData("* * * 1/10   *", "0 * * * 1,11        *", CronFormat.Standard)]
        [InlineData("* * * 1-12/5 *", "0 * * * 1,6,11      *", CronFormat.Standard)]
                         
        [InlineData("* * * 10,11-3/2 *", "0 * * * 1,3,10,11 *", CronFormat.Standard)]

        [InlineData("* * * * *      *", "* * * * *           *", CronFormat.IncludeSeconds)]
        [InlineData("* * * * 1      *", "* * * * 1           *", CronFormat.IncludeSeconds)]
        [InlineData("* * * * 1,2    *", "* * * * 1,2         *", CronFormat.IncludeSeconds)]
        [InlineData("* * * * 1-3    *", "* * * * 1,2,3       *", CronFormat.IncludeSeconds)]
        [InlineData("* * * * 11-3   *", "* * * * 1,2,3,11,12 *", CronFormat.IncludeSeconds)]
        [InlineData("* * * * */10   *", "* * * * 1,11        *", CronFormat.IncludeSeconds)]
        [InlineData("* * * * 1/10   *", "* * * * 1,11        *", CronFormat.IncludeSeconds)]
        [InlineData("* * * * 1-12/5 *", "* * * * 1,6,11      *", CronFormat.IncludeSeconds)]

        [InlineData("* * * * 10,11-3/2 *", "* * * * 1,3,10,11 *", CronFormat.IncludeSeconds)]

        // Day of week specified.
        
        [InlineData("* * * * *    ", "0 * * * * *      ", CronFormat.Standard)]
        [InlineData("* * * * MON  ", "0 * * * * 1      ", CronFormat.Standard)]
        [InlineData("* * * * 1    ", "0 * * * * 1      ", CronFormat.Standard)]
        [InlineData("* * * * 1,2  ", "0 * * * * 1,2    ", CronFormat.Standard)]
        [InlineData("* * * * 1-3  ", "0 * * * * 1,2,3  ", CronFormat.Standard)]
        [InlineData("* * * * 6-1  ", "0 * * * * 0,1,6  ", CronFormat.Standard)]
        [InlineData("* * * * */2  ", "0 * * * * 0,2,4,6", CronFormat.Standard)]
        [InlineData("* * * * 0/2  ", "0 * * * * 0,2,4,6", CronFormat.Standard)]
        [InlineData("* * * * 1-6/5", "0 * * * * 1,6    ", CronFormat.Standard)]

        [InlineData("* * * * 0L ", "0 * * * * 0L ", CronFormat.Standard)]
        [InlineData("* * * * 5#1", "0 * * * * 5#1", CronFormat.Standard)]

        // ReSharper disable once StringLiteralTypo
        [InlineData("* * * * SUNL ", "0 * * * * 0L ", CronFormat.Standard)]
        [InlineData("* * * * FRI#1", "0 * * * * 5#1", CronFormat.Standard)]

        [InlineData("* * * * 3,6-2/3", "0 * * * * 2,3,6", CronFormat.Standard)]

        [InlineData("* * * * * *    ", "* * * * * *      ", CronFormat.IncludeSeconds)]
        [InlineData("* * * * * MON  ", "* * * * * 1      ", CronFormat.IncludeSeconds)]
        [InlineData("* * * * * 1    ", "* * * * * 1      ", CronFormat.IncludeSeconds)]
        [InlineData("* * * * * 1,2  ", "* * * * * 1,2    ", CronFormat.IncludeSeconds)]
        [InlineData("* * * * * 1-3  ", "* * * * * 1,2,3  ", CronFormat.IncludeSeconds)]
        [InlineData("* * * * * 6-1  ", "* * * * * 0,1,6  ", CronFormat.IncludeSeconds)]
        [InlineData("* * * * * */2  ", "* * * * * 0,2,4,6", CronFormat.IncludeSeconds)]
        [InlineData("* * * * * 0/2  ", "* * * * * 0,2,4,6", CronFormat.IncludeSeconds)]
        [InlineData("* * * * * 1-6/5", "* * * * * 1,6    ", CronFormat.IncludeSeconds)]

        [InlineData("* * * * * 0L ", "* * * * * 0L ", CronFormat.IncludeSeconds)]
        [InlineData("* * * * * 5#1", "* * * * * 5#1", CronFormat.IncludeSeconds)]

        // ReSharper disable once StringLiteralTypo
        [InlineData("* * * * * SUNL ", "* * * * * 0L ", CronFormat.IncludeSeconds)]
        [InlineData("* * * * * FRI#1", "* * * * * 5#1", CronFormat.IncludeSeconds)]

        [InlineData("* * * * * 3,6-2/3", "* * * * * 2,3,6", CronFormat.IncludeSeconds)]
        public void ToString_ReturnsCorrectString(string cronExpression, string expectedResult, CronFormat format)
        {
            var expression = CronExpression.Parse(cronExpression, format);

            // remove redundant spaces.
            var expectedString = Regex.Replace(expectedResult, @"\s+", " ").Trim();
            
            Assert.Equal(expectedString, expression.ToString());
        }

        public static IEnumerable<object[]> GetTimeZones()
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
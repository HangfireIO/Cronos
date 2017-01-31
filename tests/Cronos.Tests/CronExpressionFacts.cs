using System;
using System.Collections.Generic;
using System.Globalization;
using NodaTime;
using Xunit;

namespace Cronos.Tests
{
    public class CronExpressionFacts
    {
        private static readonly DateTimeZone America = DateTimeZoneProviders.Bcl.GetZoneOrNull("Eastern Standard Time");
        private static readonly DateTimeZone TimeZone = DateTimeZoneProviders.Tzdb.GetZoneOrNull("America/New_York");

        private static readonly LocalDate Today = new LocalDate(2016, 12, 09);

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

        [Theory]

        // Second field is invalid.

        [InlineData("60  * * * * ?")]
        [InlineData("-1  * * * * ?")]
        [InlineData("-   * * * * ?")]
        [InlineData("5-  * * * * ?")]
        [InlineData(",   * * * * ?")]
        [InlineData(",1  * * * * ?")]
        [InlineData("/   * * * * ?")]
        [InlineData("*/  * * * * ?")]
        [InlineData("1/  * * * * ?")]
        [InlineData("#   * * * * ?")]
        [InlineData("*#1 * * * * ?")]
        [InlineData("0#2 * * * * ?")]
        [InlineData("L   * * * * ?")]
        [InlineData("W   * * * * ?")]
        [InlineData("LW  * * * * ?")]
        [InlineData("?   * * * * ?")]

        [InlineData("1/2147483648 * * * * ?")]

        // Minute field is invalid.

        [InlineData("* 60  * * * ?")]
        [InlineData("* -1  * * * ?")]
        [InlineData("* -   * * * ?")]
        [InlineData("* 7-  * * * ?")]
        [InlineData("* ,   * * * ?")]
        [InlineData("* ,1  * * * ?")]
        [InlineData("* /   * * * ?")]
        [InlineData("* #   * * * ?")]
        [InlineData("* *#1 * * * ?")]
        [InlineData("* 5#3 * * * ?")]
        [InlineData("* L   * * * ?")]
        [InlineData("* W   * * * ?")]
        [InlineData("* LW  * * * ?")]
        [InlineData("* ?   * * * ?")]

        // Hour field is invalid.

        [InlineData("* * 25   * * ?")]
        [InlineData("* * -1   * * ?")]
        [InlineData("* * -    * * ?")]
        [InlineData("* * 0-   * * ?")]
        [InlineData("* * ,    * * ?")]
        [InlineData("* * ,1   * * ?")]
        [InlineData("* * /    * * ?")]
        [InlineData("* * #    * * ?")]
        [InlineData("* * *#2  * * ?")]
        [InlineData("* * 10#1 * * ?")]
        [InlineData("* * L    * * ?")]
        [InlineData("* * W    * * ?")]
        [InlineData("* * LW   * * ?")]
        [InlineData("* * ?    * * ?")]

        // Day of month field is invalid.

        [InlineData("* * * 32    *  ?")]
        [InlineData("* * * 31    4  ?")]
        [InlineData("* * * 31    6  ?")]
        [InlineData("* * * 31    9  ?")]
        [InlineData("* * * 31    11 ?")]
        [InlineData("* * * 30    2  ?")]
        [InlineData("* * * 10-32 *  ?")]
        [InlineData("* * * 31-32 *  ?")]
        [InlineData("* * * 30-31 2  ?")]
        [InlineData("* * * 30-31 2  ?")]
        [InlineData("* * * -1    *  ?")]
        [InlineData("* * * -     *  ?")]
        [InlineData("* * * 8-    *  ?")]
        [InlineData("* * * ,     *  ?")]
        [InlineData("* * * ,1    *  ?")]
        [InlineData("* * * /     *  ?")]
        [InlineData("* * * #     *  ?")]
        [InlineData("* * * *#3   *  ?")]
        [InlineData("* * * 4#1   *  ?")]
        [InlineData("* * * W     *  ?")]
        [InlineData("* * * ?/2   *  ?")]

        // Month field is invalid.

        [InlineData("* * * * 13  *")]
        [InlineData("* * * * -1  *")]
        [InlineData("* * * * -   *")]
        [InlineData("* * * * 2-  *")]
        [InlineData("* * * * ,   *")]
        [InlineData("* * * * ,1  *")]
        [InlineData("* * * * /   *")]
        [InlineData("* * * * */  *")]
        [InlineData("* * * * 1/  *")]
        [InlineData("* * * * #   *")]
        [InlineData("* * * * *#1 *")]
        [InlineData("* * * * 2#2 *")]
        [InlineData("* * * * L   *")]
        [InlineData("* * * * W   *")]
        [InlineData("* * * * LW  *")]
        [InlineData("? * * * ?   *")]

        // Day of week field is invalid.

        [InlineData("* * * ? * 8     ")]
        [InlineData("* * * ? * -1    ")]
        [InlineData("* * * ? * -     ")]
        [InlineData("* * * ? * 3-    ")]
        [InlineData("* * * ? * ,     ")]
        [InlineData("* * * ? * ,1    ")]
        [InlineData("* * * ? * /     ")]
        [InlineData("* * * ? * */    ")]
        [InlineData("* * * ? * 1/    ")]
        [InlineData("* * * ? * #     ")]
        [InlineData("* * * ? * 0#    ")]
        [InlineData("* * * ? * 5#6   ")]
        [InlineData("* * * ? * SUN#6 ")]
        [InlineData("* * * ? * 0#0   ")]
        [InlineData("* * * ? * SUT   ")]
        [InlineData("* * * ? * SU0   ")]
        [InlineData("* * * ? * SUNDAY")]
        [InlineData("* * * ? * L     ")]
        [InlineData("* * * ? * W     ")]
        [InlineData("* * * ? * LW    ")]

        // Day of month and day of week must be specified both or one of them must be specified as '?'.

        [InlineData("* * * * * *")]
        [InlineData("* * * * * 2")]
        [InlineData("* * * 2 * *")]

        // '?' can be specfied only for day of month or day of week.

        [InlineData("? * * * * ?")]
        [InlineData("* ? * * * ?")]
        [InlineData("* * ? * * ?")]
        [InlineData("* * * * ? ?")]
        public void Parse_ThrowsAnException_WhenCronExpressionIsInvalid(string cronExpression)
        {
            var exception = Assert.Throws<ArgumentException>(() => CronExpression.Parse(cronExpression));

            Assert.Equal("cronExpression", exception.ParamName);
        }

        [Theory]

        // Basic facts.

        [InlineData("* * * * * ?", "17:35:00", "17:35:00")]

        [InlineData("* * * * * ?", "2016/12/09 16:46", "2016/12/09 16:46")]
        [InlineData("* * * ? * *", "2016/03/09 16:46", "2016/03/09 16:46")]
        [InlineData("* * * * * ?", "2016/12/30 16:46", "2016/12/30 16:46")]
        [InlineData("* * * ? * *", "2016/12/09 02:46", "2016/12/09 02:46")]
        [InlineData("* * * * * ?", "2016/12/09 16:09", "2016/12/09 16:09")]
        [InlineData("* * * ? * *", "2099/12/09 16:46", "2099/12/09 16:46")]

        // Second specified.

        [InlineData("20    * * * * ?", "17:35:00", "17:35:20")]
        [InlineData("20    * * * * ?", "17:35:20", "17:35:20")]
        [InlineData("20    * * * * ?", "17:35:40", "17:36:20")]
        [InlineData("10-30 * * * * ?", "17:35:09", "17:35:10")]
        [InlineData("10-30 * * * * ?", "17:35:10", "17:35:10")]
        [InlineData("10-30 * * * * ?", "17:35:20", "17:35:20")]
        [InlineData("10-30 * * * * ?", "17:35:30", "17:35:30")]
        [InlineData("10-30 * * * * ?", "17:35:31", "17:36:10")]
        [InlineData("*/20  * * * * ?", "17:35:00", "17:35:00")]
        [InlineData("*/20  * * * * ?", "17:35:11", "17:35:20")]
        [InlineData("*/20  * * * * ?", "17:35:20", "17:35:20")]
        [InlineData("*/20  * * * * ?", "17:35:25", "17:35:40")]
        [InlineData("*/20  * * * * ?", "17:35:59", "17:36:00")]
        [InlineData("10/5  * * * * ?", "17:35:00", "17:35:10")]
        [InlineData("10/5  * * * * ?", "17:35:12", "17:35:15")]
        [InlineData("10/5  * * * * ?", "17:35:59", "17:36:10")]
        [InlineData("0     * * * * ?", "17:35:59", "17:36:00")]
        [InlineData("0     * * * * ?", "17:59:59", "18:00:00")]

        [InlineData("5-8,19,20,35-41 * * * * ?", "17:35:01", "17:35:05")]
        [InlineData("5-8,19,20,35-41 * * * * ?", "17:35:06", "17:35:06")]
        [InlineData("5-8,19,20,35-41 * * * * ?", "17:35:18", "17:35:19")]
        [InlineData("5-8,19,20,35-41 * * * * ?", "17:35:19", "17:35:19")]
        [InlineData("5-8,19,20,35-41 * * * * ?", "17:35:20", "17:35:20")]
        [InlineData("5-8,19,20,35-41 * * * * ?", "17:35:21", "17:35:35")]
        [InlineData("5-8,19,20,35-41 * * * * ?", "17:35:36", "17:35:36")]
        [InlineData("5-8,19,20,35-41 * * * * ?", "17:35:42", "17:36:05")]

        // Minute specified.

        [InlineData("* 12    * * * ?", "15:05", "15:12")]
        [InlineData("* 12    * * * ?", "15:12", "15:12")]
        [InlineData("* 12    * * * ?", "15:59", "16:12")]
        [InlineData("* 31-39 * * * ?", "15:00", "15:31")]
        [InlineData("* 31-39 * * * ?", "15:30", "15:31")]
        [InlineData("* 31-39 * * * ?", "15:31", "15:31")]
        [InlineData("* 31-39 * * * ?", "15:39", "15:39")]
        [InlineData("* 31-39 * * * ?", "15:59", "16:31")]
        [InlineData("* */20  * * * ?", "15:00", "15:00")]
        [InlineData("* */20  * * * ?", "15:10", "15:20")]
        [InlineData("* */20  * * * ?", "15:59", "16:00")]
        [InlineData("* 10/5  * * * ?", "15:00", "15:10")]
        [InlineData("* 10/5  * * * ?", "15:14", "15:15")]
        [InlineData("* 10/5  * * * ?", "15:59", "16:10")]
        [InlineData("* 0     * * * ?", "15:59", "16:00")]

        [InlineData("* 5-8,19,20,35-41 * * * ?", "15:01", "15:05")]
        [InlineData("* 5-8,19,20,35-41 * * * ?", "15:06", "15:06")]
        [InlineData("* 5-8,19,20,35-41 * * * ?", "15:18", "15:19")]
        [InlineData("* 5-8,19,20,35-41 * * * ?", "15:19", "15:19")]
        [InlineData("* 5-8,19,20,35-41 * * * ?", "15:20", "15:20")]
        [InlineData("* 5-8,19,20,35-41 * * * ?", "15:21", "15:35")]
        [InlineData("* 5-8,19,20,35-41 * * * ?", "15:36", "15:36")]
        [InlineData("* 5-8,19,20,35-41 * * * ?", "15:42", "16:05")]

        // Hour specified.

        [InlineData("* * 11   * * ?", "10:59", "11:00")]
        [InlineData("* * 11   * * ?", "11:30", "11:30")]
        [InlineData("* * 3-22 * * ?", "01:40", "03:00")]
        [InlineData("* * 3-22 * * ?", "11:40", "11:40")]
        [InlineData("* * */2  * * ?", "00:00", "00:00")]
        [InlineData("* * */2  * * ?", "01:00", "02:00")]
        [InlineData("* * 4/5  * * ?", "00:45", "04:00")]
        [InlineData("* * 4/5  * * ?", "04:14", "04:14")]
        [InlineData("* * 4/5  * * ?", "05:00", "09:00")]

        [InlineData("* * 3-5,10,11,13-17 * * ?", "01:55", "03:00")]
        [InlineData("* * 3-5,10,11,13-17 * * ?", "04:55", "04:55")]
        [InlineData("* * 3-5,10,11,13-17 * * ?", "06:10", "10:00")]
        [InlineData("* * 3-5,10,11,13-17 * * ?", "10:55", "10:55")]
        [InlineData("* * 3-5,10,11,13-17 * * ?", "11:25", "11:25")]
        [InlineData("* * 3-5,10,11,13-17 * * ?", "12:30", "13:00")]
        [InlineData("* * 3-5,10,11,13-17 * * ?", "17:30", "17:30")]

        // Day of month specified.

        [InlineData("* * * 9     * ?", "2016/11/01", "2016/11/09")]
        [InlineData("* * * 9     * ?", "2016/11/09", "2016/11/09")]
        [InlineData("* * * 09    * ?", "2016/11/10", "2016/12/09")]
        [InlineData("* * * */4   * ?", "2016/12/01", "2016/12/01")]
        [InlineData("* * * */4   * ?", "2016/12/02", "2016/12/05")]
        [InlineData("* * * */4   * ?", "2016/12/06", "2016/12/09")]
        [InlineData("* * * */3   * ?", "2016/12/02", "2016/12/04")]
        [InlineData("* * * 10,20 * ?", "2016/12/09", "2016/12/10")]
        [InlineData("* * * 10,20 * ?", "2016/12/12", "2016/12/20")]
        [InlineData("* * * 16-23 * ?", "2016/12/01", "2016/12/16")]
        [InlineData("* * * 16-23 * ?", "2016/12/16", "2016/12/16")]
        [InlineData("* * * 16-23 * ?", "2016/12/18", "2016/12/18")]
        [InlineData("* * * 16-23 * ?", "2016/12/23", "2016/12/23")]
        [InlineData("* * * 16-23 * ?", "2016/12/24", "2017/01/16")]

        [InlineData("* * * 5-8,19,20,28-29 * ?", "2016/12/01", "2016/12/05")]
        [InlineData("* * * 5-8,19,20,28-29 * ?", "2016/12/05", "2016/12/05")]
        [InlineData("* * * 5-8,19,20,28-29 * ?", "2016/12/06", "2016/12/06")]
        [InlineData("* * * 5-8,19,20,28-29 * ?", "2016/12/08", "2016/12/08")]
        [InlineData("* * * 5-8,19,20,28-29 * ?", "2016/12/09", "2016/12/19")]
        [InlineData("* * * 5-8,19,20,28-29 * ?", "2016/12/20", "2016/12/20")]
        [InlineData("* * * 5-8,19,20,28-29 * ?", "2016/12/21", "2016/12/28")]
        [InlineData("* * * 5-8,19,20,28-29 * ?", "2016/12/30", "2017/01/05")]
        [InlineData("* * * 5-8,19,20,29-30 * ?", "2017/02/27", "2017/03/05")]

        // Month specified.

        [InlineData("* * * * 11      ?", "2016/10/09", "2016/11/01")]
        [InlineData("* * * * 11      ?", "2016/11/02", "2016/11/02")]
        [InlineData("* * * * 11      ?", "2016/12/02", "2017/11/01")]
        [InlineData("* * * * 3,9     ?", "2016/01/09", "2016/03/01")]
        [InlineData("* * * * 3,9     ?", "2016/06/09", "2016/09/01")]
        [InlineData("* * * * 3,9     ?", "2016/10/09", "2017/03/01")]
        [InlineData("* * * * 5-11    ?", "2016/01/01", "2016/05/01")]
        [InlineData("* * * * 5-11    ?", "2016/05/07", "2016/05/07")]
        [InlineData("* * * * 5-11    ?", "2016/07/12", "2016/07/12")]
        [InlineData("* * * * 05-11   ?", "2016/12/13", "2017/05/01")]
        [InlineData("* * * * DEC     ?", "2016/08/09", "2016/12/01")]
        [InlineData("* * * * mar-dec ?", "2016/02/09", "2016/03/01")]
        [InlineData("* * * * mar-dec ?", "2016/04/09", "2016/04/09")]
        [InlineData("* * * * mar-dec ?", "2016/12/09", "2016/12/09")]
        [InlineData("* * * * */4     ?", "2016/01/09", "2016/01/09")]
        [InlineData("* * * * */4     ?", "2016/02/09", "2016/05/01")]
        [InlineData("* * * * */3     ?", "2016/12/09", "2017/01/01")]
        [InlineData("* * * * */5     ?", "2016/12/09", "2017/01/01")]
        [InlineData("* * * * APR-NOV ?", "2016/12/09", "2017/04/01")]    

        [InlineData("* * * * 2-4,JUN,7,SEP-nov ?", "2016/01/01", "2016/02/01")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov ?", "2016/02/10", "2016/02/10")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov ?", "2016/03/01", "2016/03/01")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov ?", "2016/05/20", "2016/06/01")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov ?", "2016/06/10", "2016/06/10")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov ?", "2016/07/05", "2016/07/05")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov ?", "2016/08/15", "2016/09/01")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov ?", "2016/11/25", "2016/11/25")]
        [InlineData("* * * * 2-4,JUN,7,SEP-nov ?", "2016/12/01", "2017/02/01")]

        // Day of week specified.

        // Monday        Tuesday       Wednesday     Thursday      Friday        Saturday      Sunday
        //                                           2016/12/01    2016/12/02    2016/12/03    2016/12/04
        // 2016/12/05    2016/12/06    2016/12/07    2016/12/08    2016/12/09    2016/12/10    2016/12/11
        // 2016/12/12    2016/12/13    2016/12/14    2016/12/15    2016/12/16    2016/12/17    2016/12/18

        [InlineData("* * * ? * 5      ", "2016/12/07", "2016/12/09")]
        [InlineData("* * * ? * 5      ", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? * 05     ", "2016/12/10", "2016/12/16")]
        [InlineData("* * * ? * 3,5,7  ", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? * 3,5,7  ", "2016/12/10", "2016/12/11")]
        [InlineData("* * * ? * 3,5,7  ", "2016/12/12", "2016/12/14")]
        [InlineData("* * * ? * 4-7    ", "2016/12/08", "2016/12/08")]
        [InlineData("* * * ? * 4-7    ", "2016/12/10", "2016/12/10")]
        [InlineData("* * * ? * 4-7    ", "2016/12/11", "2016/12/11")]
        [InlineData("* * * ? * 4-07   ", "2016/12/12", "2016/12/15")]
        [InlineData("* * * ? * FRI    ", "2016/12/08", "2016/12/09")]
        [InlineData("* * * ? * tue/2  ", "2016/12/09", "2016/12/10")]
        [InlineData("* * * ? * tue/2  ", "2016/12/11", "2016/12/13")]
        [InlineData("* * * ? * FRI/3  ", "2016/12/03", "2016/12/09")]
        [InlineData("* * * ? * thu-sat", "2016/12/04", "2016/12/08")]
        [InlineData("* * * ? * thu-sat", "2016/12/09", "2016/12/09")]
        [InlineData("* * * ? * thu-sat", "2016/12/10", "2016/12/10")]
        [InlineData("* * * ? * thu-sat", "2016/12/12", "2016/12/15")]
        [InlineData("* * * ? * */5    ", "2016/12/08", "2016/12/09")]
        [InlineData("* * * ? * */5    ", "2016/12/10", "2016/12/11")]
        [InlineData("* * * ? * */5    ", "2016/12/12", "2016/12/16")]
        //[InlineData("* * * ? * thu-sun", "2016/12/09", "2016/12/09")] // TODO: that's bad

        [InlineData("00 00 00 11 12 0  ", "2016/12/07", "2016/12/11")]
        [InlineData("00 00 00 11 12 7  ", "2016/12/09", "2016/12/11")]
        [InlineData("00 00 00 11 12 SUN", "2016/12/10", "2016/12/11")]
        [InlineData("00 00 00 11 12 sun", "2016/12/09", "2016/12/11")]

        // All fields are specified.

        [InlineData("54    47    17    09   12    5    ", "2016/10/01 00:00:00", "2016/12/09 17:47:54")]
        [InlineData("54    47    17    09   DEC   FRI  ", "2016/07/05 00:00:00", "2016/12/09 17:47:54")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016/12/01 00:00:00", "2016/12/09 15:40:50")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016/12/09 15:40:53", "2016/12/09 15:40:53")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016/12/09 15:40:57", "2016/12/09 15:41:50")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016/12/09 15:45:56", "2016/12/09 15:45:56")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016/12/09 15:51:56", "2016/12/09 16:40:50")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016/12/09 21:50:56", "2016/12/10 15:40:50")]
        [InlineData("50-56 40-50 15-20 5-10 11,12 5,6,7", "2016/12/11 21:50:56", "2017/11/05 15:40:50")]

        // Friday the thirteenth.

        [InlineData("00    05    18    13   01    05   ", "2016/01/01 00:00:00", "2017/01/13 18:05:00")]
        [InlineData("00    05    18    13   *     05   ", "2016/01/01 00:00:00", "2016/05/13 18:05:00")]
        [InlineData("00    05    18    13   *     05   ", "2016/09/01 00:00:00", "2017/01/13 18:05:00")]
        [InlineData("00    05    18    13   *     05   ", "2017/02/01 00:00:00", "2017/10/13 18:05:00")]

        // Handle moving to next second, minute, hour, month, year.

        [InlineData("0 * * * * ?", "2017/01/14 12:58:59", "2017/01/14 12:59:00")]

        [InlineData("0 0 * * * ?", "2017/01/14 12:59", "2017/01/14 13:00")]
        [InlineData("0 0 0 * * ?", "2017/01/14 23:00", "2017/01/15 00:00")]
        [InlineData("0 0 0 1 * ?", "2017/01/31 00:00", "2017/02/01 00:00")]
        [InlineData("0 0 0 * * ?", "2017/12/31 23:59", "2018/01/01 00:00")]
        [InlineData("0 0 0 * * ?", "2017/12/31 00:00", "2018/01/01 00:00")]

        // Skip month if day of month is specified and month has less days.

        [InlineData("0 0 0 30 * ?", "2017/02/25 00:00", "2017/03/30 00:00")]
        [InlineData("0 0 0 31 * ?", "2017/02/25 00:00", "2017/03/31 00:00")]
        [InlineData("0 0 0 31 * ?", "2017/04/01 00:00", "2017/05/31 00:00")]

        // Leap year.

        [InlineData("0 0 0 29 2 ?", "2017/02/25 00:00", "2020/02/29 00:00")]

        // Support 'L' character in day of month field.

        [InlineData("* * * L * ?","2016/01/05", "2016/01/31")]
        [InlineData("* * * L * ?","2016/01/31", "2016/01/31")]
        [InlineData("* * * L * ?","2016/02/05", "2016/02/29")]
        [InlineData("* * * L * ?","2016/02/29", "2016/02/29")]
        [InlineData("* * * L * ?","2017/02/28", "2017/02/28")]
        [InlineData("* * * L * ?","2100/02/05", "2100/02/28")]
        [InlineData("* * * L * ?","2016/03/05", "2016/03/31")]
        [InlineData("* * * L * ?","2016/03/31", "2016/03/31")]
        [InlineData("* * * L * ?","2016/04/05", "2016/04/30")]
        [InlineData("* * * L * ?","2016/04/30", "2016/04/30")]
        [InlineData("* * * L * ?","2016/05/05", "2016/05/31")]
        [InlineData("* * * L * ?","2016/05/31", "2016/05/31")]
        [InlineData("* * * L * ?","2016/06/05", "2016/06/30")]
        [InlineData("* * * L * ?","2016/06/30", "2016/06/30")]
        [InlineData("* * * L * ?","2016/07/05", "2016/07/31")]
        [InlineData("* * * L * ?","2016/07/31", "2016/07/31")]
        [InlineData("* * * L * ?","2016/08/05", "2016/08/31")]
        [InlineData("* * * L * ?","2016/08/31", "2016/08/31")]
        [InlineData("* * * L * ?","2016/09/05", "2016/09/30")]
        [InlineData("* * * L * ?","2016/09/30", "2016/09/30")]
        [InlineData("* * * L * ?","2016/10/05", "2016/10/31")]
        [InlineData("* * * L * ?","2016/10/31", "2016/10/31")]
        [InlineData("* * * L * ?","2016/11/05", "2016/11/30")]
        [InlineData("* * * L * ?","2016/12/05", "2016/12/31")]
        [InlineData("* * * L * ?","2016/12/31", "2016/12/31")]
        [InlineData("* * * L * ?","2099/12/05", "2099/12/31")]
        [InlineData("* * * L * ?","2099/12/31", "2099/12/31")]

        // Support 'L' character in day of week field.

        // Monday        Tuesday       Wednesday     Thursday      Friday        Saturday      Sunday
        // 2016/01/23    2016/01/24    2016/01/25    2016/01/26    2016/01/27    2016/01/28    2016/01/29
        // 2016/01/30    2016/01/31

        [InlineData("* * * ? * 0L", "2017/01/29", "2017/01/29")]
        [InlineData("* * * ? * 0L", "2017/01/01", "2017/01/29")]
        [InlineData("* * * ? * 1L", "2017/01/30", "2017/01/30")]
        [InlineData("* * * ? * 1L", "2017/01/01", "2017/01/30")]
        [InlineData("* * * ? * 2L", "2017/01/31", "2017/01/31")]
        [InlineData("* * * ? * 2L", "2017/01/01", "2017/01/31")]
        [InlineData("* * * ? * 3L", "2017/01/25", "2017/01/25")]
        [InlineData("* * * ? * 3L", "2017/01/01", "2017/01/25")]
        [InlineData("* * * ? * 4L", "2017/01/26", "2017/01/26")]
        [InlineData("* * * ? * 4L", "2017/01/01", "2017/01/26")]
        [InlineData("* * * ? * 5L", "2017/01/27", "2017/01/27")]
        [InlineData("* * * ? * 5L", "2017/01/01", "2017/01/27")]
        [InlineData("* * * ? * 6L", "2017/01/28", "2017/01/28")]
        [InlineData("* * * ? * 6L", "2017/01/01", "2017/01/28")]
        [InlineData("* * * ? * 7L", "2017/01/29", "2017/01/29")]
        [InlineData("* * * ? * 7L", "2016/12/31", "2017/01/29")]

        // Support '#' in day of week field.

        [InlineData("* * * ? * SUN#1", "2017/01/01", "2017/01/01")]
        [InlineData("* * * ? * 0#1  ", "2017/01/01", "2017/01/01")]
        [InlineData("* * * ? * 0#1  ", "2016/12/10", "2017/01/01")]
        [InlineData("* * * ? * 0#1  ", "2017/02/01", "2017/02/05")]
        [InlineData("* * * ? * 0#2  ", "2017/01/01", "2017/01/08")]
        [InlineData("* * * ? * 0#2  ", "2017/01/08", "2017/01/08")]
        [InlineData("* * * ? * 5#3  ", "2017/01/01", "2017/01/20")]
        [InlineData("* * * ? * 5#3  ", "2017/01/21", "2017/02/17")]
        [InlineData("* * * ? * 3#2  ", "2017/01/01", "2017/01/11")]
        [InlineData("* * * ? * 2#5  ", "2017/02/01", "2017/05/30")]

        // Handle '?'.

        [InlineData("* * * ? 11 *", "2016/10/09", "2016/11/01")]
        public void Next_ReturnsCorrectDate(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var nextExecuting = expression.Next(GetZonedDateTime(startTime));

            var expectedDateTime = expectedTime != null ? GetZonedDateTime(expectedTime) : (ZonedDateTime?)null;

            Assert.Equal(expectedDateTime, nextExecuting);
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

        [Theory]
        [InlineData("* * * * * ?", "15:30", "15:30")]
        [InlineData("0 5 * * * ?", "00:00", "00:05")]
        public void Next_ReturnsCorrectUtcDate(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var nextExecuting = expression.Next(GetZonedDateTime(startTime));

            Assert.Equal(GetZonedDateTime(expectedTime), nextExecuting);
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

        // 2016/03/13 is date when the clock jumps forward from 1:59 ST to 3:00 DST in America.
        // Period from 2016/03/13 2:00 am to 2016/03/13 2:59 am is invalid.

        // Skipped due to intervals, no problems here
        [InlineData("0 */30 * * * ?"     , "2016/03/13 01:45 ST", "2016/03/13 03:00 DST")]

        // Skipped due to intervals, can be avoided by enumerating hours and minutes
        // "0,30 0-23/2 * * *"
        [InlineData("0 */30 */2 * * ?"   , "2016/03/13 01:59 ST", "2016/03/13 04:00 DST")]

        // Run missed, strict
        [InlineData("0 0,30 0-23/2 * * ?", "2016/03/13 01:59 ST", "2016/03/13 03:00 DST")]

        // TODO: may be confusing!
        // Skipped due to intervals, can be avoided by using "0,30 02 * * *"
        [InlineData("0 */30 2 * * ?"     , "2016/03/13 01:59 ST", "2016/03/14 02:00 DST")]

        // Run missed
        [InlineData("0 0,30 2 * * ?"     , "2016/03/13 01:59 ST", "2016/03/13 03:00 DST")]
        [InlineData("0 30 02 13 03 ?"    , "2016/03/13 01:45 ST", "2016/03/13 03:00 DST")]

        // Run missed, delay
        [InlineData("0 30 2 * * ?"       , "2016/03/13 01:59 ST", "2016/03/13 03:00 DST")]

        // Skipped due to intervals, "0 0-23/2 * * *" can be used to avoid skipping
        // TODO: differ from Linux Cron
        [InlineData("0 0 */2 * * ?"      , "2016/03/13 01:59 ST", "2016/03/13 03:00 DST")]

        // Run missed
        [InlineData("0 0 0-23/2 * * ?"   , "2016/03/13 01:59 ST", "2016/03/13 03:00 DST")]

        [InlineData("0 */30 2 * * ?", "2016/03/12 23:59", "2016/03/14 02:00")]
        [InlineData("0 30 2 13 03 ?", "2016/03/13 23:59", "2017/03/13 02:30")]
        [InlineData("0 */30 2 13 3 ?", "2016/03/13 23:59", "2017/03/13 02:00")]
        public void Next_HandleDST_WhenTheClockJumpsForward(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var executed = expression.Next(GetZonedDateTime(startTime));

            Assert.Equal(GetZonedDateTime(expectedTime), executed);
        }

        [Theory]

        // As usual due to intervals
        [InlineData("0 */30 * * * ?", "2016/11/06 01:30 DST", "2016/11/06 01:30 DST")]
        [InlineData("0 */30 * * * ?", "2016/11/06 01:59 DST", "2016/11/06 01:00 ST")]
        [InlineData("0 */30 * * * ?", "2016/11/06 01:15 ST", "2016/11/06 01:30 ST")]

        // As usual due to intervals
        [InlineData("0 */30 */2 * * ?", "2016/11/06 01:30 DST", "2016/11/06 02:00 ST")]

        // As usual due to intervals
        [InlineData("0 0 1 * * ?", "2016/11/06 01:00 DST", "2016/11/06 01:00 DST")]
        [InlineData("0 0 1 * * ?", "2016/11/06 01:00 ST", "2016/11/07 01:00 ST")]

        // TODO: differ from Linux Cron
        // Duplicates skipped due to non-wildcard hour
        [InlineData("0 */30 1 * * ?", "2016/11/06 01:20 DST", "2016/11/06 01:30 DST")]
        [InlineData("0 */30 1 * * ?", "2016/11/06 01:59 DST", "2016/11/06 01:00 ST")]
        [InlineData("0 */30 1 * * ?", "2016/11/06 01:30 ST", "2016/11/06 01:30 ST")]

        // Duplicates skipped due to non-wildcard minute
        [InlineData("0 0 */2 * * ?", "2016/11/06 00:30 DST", "2016/11/06 02:00 ST")]

        // Duplicates skipped due to non-wildcard
        [InlineData("0 0,30 1 * * ?", "2016/11/06 01:00 DST", "2016/11/06 01:00 DST")]
        [InlineData("0 0,30 1 * * ?", "2016/11/06 01:20 DST", "2016/11/06 01:30 DST")]
        [InlineData("0 0,30 1 * * ?", "2016/11/06 01:00 ST", "2016/11/07 01:00 ST")]

        // Duplicates skipped due to non-wildcard
        [InlineData("0 30 * * * ?", "2016/11/06 01:30 DST", "2016/11/06 01:30 DST")]
        [InlineData("0 30 * * * ?", "2016/11/06 01:59 DST", "2016/11/06 01:30 ST")]
        public void Next_HandleDST_WhenTheClockJumpsBackward(string cronExpression, string startTime, string expectedTime)
        {
            var expression = CronExpression.Parse(cronExpression);

            var executed = expression.Next(GetZonedDateTime(startTime));

            Assert.Equal(GetZonedDateTime(expectedTime), executed);
        }

        private static ZonedDateTime GetZonedDateTime(string dateTimeString)
        {
            var isDst = dateTimeString.Contains("DST");
            dateTimeString = dateTimeString.Replace("DST", "");

            var isSt = dateTimeString.Contains("ST");
            dateTimeString = dateTimeString.Replace("ST", "");

            dateTimeString = dateTimeString.Trim();

            var dateTime = DateTime.ParseExact(
                dateTimeString,
                new[]
                {
                    "HH:mm:ss",
                    "HH:mm",
                    "yyyy/MM/dd HH:mm:ss",
                    "yyyy/MM/dd HH:mm",
                    "yyyy/MM/dd"
                },
                CultureInfo.InvariantCulture,
                DateTimeStyles.NoCurrentDateDefault);

            var localDateTime = new LocalDateTime(
                dateTime.Year != 1 ? dateTime.Year : Today.Year,
                dateTime.Year != 1 ? dateTime.Month : Today.Month,
                dateTime.Year != 1 ? dateTime.Day : Today.Day,
                dateTime.Hour,
                dateTime.Minute,
                dateTime.Second);

            if(!isDst && !isSt) return localDateTime.InZoneStrictly(America);

            return isDst ?
              localDateTime.InZone(America, mapping => mapping.First()) :
              localDateTime.InZone(America, mapping => mapping.Last());
        }
    }
}
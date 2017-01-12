using System;

namespace Cronos
{
    public class CronExpression
    {
        public static CronExpression Parse(string s)
        {
            return new CronExpression();
        }

        public bool IsMatch(DateTime dateTime)
        {
            return true;
        }
    }
}
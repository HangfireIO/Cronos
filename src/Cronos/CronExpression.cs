using System;

namespace Cronos
{
    public class CronExpression
    {
        public static CronExpression Parse(string cronExpression)
        {
            if (string.IsNullOrEmpty(cronExpression)) throw new ArgumentNullException(nameof(cronExpression));

            return new CronExpression();
        }

        public bool IsMatch(DateTime dateTime)
        {
            return true;
        }
    }
}
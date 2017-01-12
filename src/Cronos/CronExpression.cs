using System;

namespace Cronos
{
    public class CronExpression
    {
        public static CronExpression Parse(string cronExpression)
        {
            if (string.IsNullOrEmpty(cronExpression)) throw new ArgumentNullException(nameof(cronExpression));

            // TODO: Add message to exception.
            if(cronExpression.Split(' ').Length < 6) throw new FormatException();

            return new CronExpression();
        }

        public bool IsMatch(int second, int minute, int hour, int dayOfMonth, int month, int dayOfWeek)
        {
            return true;
        }
    }
}
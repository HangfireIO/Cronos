using NodaTime;

namespace Cronos
{
    public static class CronExpressionExtensions
    {
        public static bool IsMatch(this CronExpression cronExpression, LocalDateTime dateTime)
        {
            return cronExpression.IsMatch(
                dateTime.Second,
                dateTime.Minute,
                dateTime.Hour,
                dateTime.Day,
                dateTime.Month,
                dateTime.DayOfWeek);
        }
    }
}

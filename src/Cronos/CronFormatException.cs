using System;

namespace Cronos
{
    internal class CronFormatException : FormatException
    {
        public CronFormatException(CronField field, string message): base($"{field}: {message}")
        {
        }

        public CronFormatException(string message) : base(message)
        {
        }
    }
}
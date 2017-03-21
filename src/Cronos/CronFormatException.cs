using System;

namespace Cronos
{
    /// <summary>
    /// Represents an exception that's thrown, when invalid Cron expression is given.
    /// </summary>
#if !NETSTANDARD1_0
    [Serializable]
#endif
    public class CronFormatException : FormatException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CronFormatException"/> class with
        /// the given message.
        /// </summary>
        public CronFormatException(string message) : base(message)
        {
        }

        internal CronFormatException(CronField field, string message) : this($"{field}: {message}")
        {
        }
    }
}
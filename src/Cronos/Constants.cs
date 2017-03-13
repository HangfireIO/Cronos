namespace Cronos
{
    internal class Constants
    {
        private static readonly string[] MonthNames =
        {
            "JAN", "FEB", "MAR", "APR", "MAY", "JUN",
            "JUL", "AUG", "SEP", "OCT", "NOV", "DEC",
        };

        private static readonly string[] DayOfWeekNames =
        {
            "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN"
        };

        private static readonly int[] MonthNamesArray = new int[MonthNames.Length];
        private static readonly int[] DayOfWeekNamesArray = new int[DayOfWeekNames.Length];

        public static readonly CronFieldDescriptor Seconds = new CronFieldDescriptor
        {
            Field = CronField.Second,
            First = 0,
            Last = 59
        };

        public static readonly CronFieldDescriptor Minutes = new CronFieldDescriptor
        {
            Field = CronField.Minute,
            First = 0,
            Last = 59
        };

        public static readonly CronFieldDescriptor Hours = new CronFieldDescriptor
        {
            Field = CronField.Hour,
            First = 0,
            Last = 23
        };

        public static readonly CronFieldDescriptor DaysOfMonth = new CronFieldDescriptor
        {
            Field = CronField.DayOfMonth,
            First = 1,
            Last = 31
        };

        public static readonly CronFieldDescriptor Months = new CronFieldDescriptor
        {
            Field = CronField.Month,
            First = 1,
            Last = 12,
            Names = MonthNamesArray
        };

        public static readonly CronFieldDescriptor DaysOfWeek = new CronFieldDescriptor
        {
            Field = CronField.DayOfWeek,
            First = 0,
            Last = 7, // 0 and 7 are both Sunday, for compatibility reasons.
            Names = DayOfWeekNamesArray
        };
        
        public const int MinDaysInMonth = 28;

        public const int DaysPerWeekCount = 7;

        public const int MinNthDayOfWeek = 1;
        public const int MaxNthDayOfWeek = 5;

        public const int SundayBits = 0b1000_0001;

        static Constants()
        {
            for (var i = 0; i < MonthNames.Length; i++)
            {
                var name = MonthNames[i].ToUpperInvariant();
                var array = new char[3];
                array[0] = name[0];
                array[1] = name[1];
                array[2] = name[2];

                var combined = name[0] | (name[1] << 8) | (name[2] << 16);

                MonthNamesArray[i] = combined;
            }

            for (var i = 0; i < DayOfWeekNames.Length; i++)
            {
                var name = DayOfWeekNames[i].ToUpperInvariant();
                var array = new char[3];
                array[0] = name[0];
                array[1] = name[1];
                array[2] = name[2];

                var combined = name[0] | (name[1] << 8) | (name[2] << 16);

                DayOfWeekNamesArray[i] = combined;
            }
        }
    }
}

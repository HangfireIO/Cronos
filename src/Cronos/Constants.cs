namespace Cronos
{
    internal class Constants
    {
        public static readonly int[] MonthNamesArray;
        public static readonly int[] DayOfWeekNamesArray;

        public static readonly string[] MonthNames =
        {
            "JAN", "FEB", "MAR", "APR", "MAY", "JUN",
            "JUL", "AUG", "SEP", "OCT", "NOV", "DEC",
        };

        public static readonly string[] DayOfWeekNames =
        {
            "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN"
        };

        public static readonly int[] FirstValues;
        public static readonly int[] LastValues;
        public static readonly int[][] NameArrays;

        public const int CronWithSecondsFieldsCount = 6;
        public const int CronWithoutSecondsFieldsCount = 5;

        public const int FirstSecond = 0;
        public const int LastSecond = 59;

        public const int FirstMinute = 0;
        public const int LastMinute = 59;

        public const int FirstHour = 0;
        public const int LastHour = 23;

        public const int FirstDayOfMonth = 1;
        public const int LastDayOfMonth = 31;
        public const int MinDaysInMonth = 28;

        public const int FirstMonth = 1;
        public const int LastMonth = 12;

        // Note on DOW: 0 and 7 are both Sunday, for compatibility reasons. 
        public const int FirstDayOfWeek = 0;
        public const int LastDayOfWeek = 7;

        public const int DaysPerWeekCount = 7;

        public const int MinNthDayOfWeek = 1;
        public const int MaxNthDayOfWeek = 5;

        public const int SundayBits = 0b1000_0001;

        static Constants()
        {
            FirstValues = new int[6];
            FirstValues[(int) CronField.Second] = FirstSecond;
            FirstValues[(int) CronField.Minute] = FirstMinute;
            FirstValues[(int) CronField.Hour] = FirstHour;
            FirstValues[(int) CronField.DayOfMonth] = FirstDayOfMonth;
            FirstValues[(int) CronField.Month] = FirstMonth;
            FirstValues[(int) CronField.DayOfWeek] = FirstDayOfWeek;

            LastValues = new int[6];
            LastValues[(int) CronField.Second] = LastSecond;
            LastValues[(int) CronField.Minute] = LastMinute;
            LastValues[(int) CronField.Hour] = LastHour;
            LastValues[(int) CronField.DayOfMonth] = LastDayOfMonth;
            LastValues[(int) CronField.Month] = LastMonth;
            LastValues[(int) CronField.DayOfWeek] = LastDayOfWeek;

            MonthNamesArray = new int[MonthNames.Length];
            DayOfWeekNamesArray = new int[DayOfWeekNames.Length];

            NameArrays = new int[6][];
            NameArrays[(int) CronField.Month] = MonthNamesArray;
            NameArrays[(int) CronField.DayOfWeek] = DayOfWeekNamesArray;

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

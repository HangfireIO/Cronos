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

        public const int FirstSecond = 0;
        public const int LastSecond = 59;

        public const int FirstMinute = 0;
        public const int LastMinute = 59;

        public const int FirstHour = 0;
        public const int LastHour = 23;

        public const int FirstDayOfMonth = 1;
        public const int LastDayOfMonth = 31;

        public const int FirstMonth = 1;
        public const int LastMonth = 12;

        // Note on DOW: 0 and 7 are both Sunday, for compatibility reasons. 
        public const int FirstDayOfWeek = 0;
        public const int LastDayOfWeek = 7;

        public const int DaysPerWeekCount = 7;

        public const int MinNthDayOfWeek = 1;
        public const int MaxNthDayOfWeek = 5;

        // 101011010101 0
        public const long MonthsWith31Days = 0x15AA;
        // 111111111101 0
        public const long MonthsWith30Or31Days = 0x1FFA;

        public const long The31ThDayOfMonth = 0x80000000;
        public const long The30ThDayOfMonth = 0x40000000;
        public const long The30ThOr31ThDayOfMonth = The30ThDayOfMonth | The31ThDayOfMonth;

        static Constants()
        {
            MonthNamesArray = new int[MonthNames.Length];
            DayOfWeekNamesArray = new int[DayOfWeekNames.Length];

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

namespace Cronos
{
    internal sealed class CronField
    {
        private static readonly string[] MonthNames =
        {
            "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC"
        };

        private static readonly string[] DayOfWeekNames =
        {
            "SUN", "MON", "TUE", "WED", "THU", "FRI", "SAT", "SUN"
        };

        private static readonly int[] MonthNamesArray = new int[MonthNames.Length];
        private static readonly int[] DayOfWeekNamesArray = new int[DayOfWeekNames.Length];

        // 0 and 7 are both Sunday, for compatibility reasons.
        public static readonly CronField DaysOfWeek = new CronField("Days of week", 0, 7, DayOfWeekNamesArray, null, false);

        public static readonly CronField Months = new CronField("Months", 1, 12, MonthNamesArray, null, false);
        public static readonly CronField DaysOfMonth = new CronField("Days of month", 1, 31, null, Months, false);
        public static readonly CronField Hours = new CronField("Hours", 0, 23, null, DaysOfMonth, true);
        public static readonly CronField Minutes = new CronField("Minutes", 0, 59, null, Hours, true);
        public static readonly CronField Seconds = new CronField("Seconds", 0, 59, null, Minutes, true);

        static CronField()
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

        public readonly string Name;
        public readonly int First;
        public readonly int Last;
        public readonly int[] Names;
        public readonly CronField Next;
        public readonly bool CanDefineInterval;

        private CronField(string name, int first, int last, int[] names, CronField next, bool canDefineInterval)
        {
            Name = name;
            First = first;
            Last = last;
            Names = names;
            Next = next;
            CanDefineInterval = canDefineInterval;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
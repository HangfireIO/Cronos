namespace Cronos
{
    // Implementation of de bruijin algorithm to find first set.
    internal static class DeBruijin
    {
        private static readonly int[] Positions =
        {
            0, 1, 2, 53, 3, 7, 54, 27,
            4, 38, 41, 8, 34, 55, 48, 28,
            62, 5, 39, 46, 44, 42, 22, 9,
            24, 35, 59, 56, 49, 18, 29, 11,
            63, 52, 6, 26, 37, 40, 33, 47,
            61, 45, 43, 21, 23, 58, 17, 10,
            51, 25, 36, 32, 60, 20, 57, 16,
            50, 31, 19, 15, 30, 14, 13, 12
        };

        public static int FindFirstSet(long value, int startBit, int endBit)
        {
            value = value >> startBit;

            if (value == 0) return -1;

            ulong res = unchecked((ulong)(value & -value) * 0x022fdd63cc95386d) >> 58;

            var result = Positions[res] + startBit;
            if (result > endBit) return -1;

            return result;
        }
    }
}
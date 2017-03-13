namespace Cronos
{
    internal sealed class CronFieldDescriptor
    {
        public readonly CronField Field;
        public readonly int First;
        public readonly int Last;
        public readonly int[] Names;

        public CronFieldDescriptor(CronField field, int first, int last, int[] names)
        {
            Field = field;
            First = first;
            Last = last;
            Names = names;
        }
    }
}
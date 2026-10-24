using System;

namespace VelocityRivals
{
    /// <summary>Visit every track once per cycle and avoid repeats at cycle boundaries.</summary>
    public sealed class ShufflePlaylist
    {
        private readonly int[] order;
        private readonly Random random;
        private int cursor;
        private int previous = -1;

        public ShufflePlaylist(int count, int seed)
        {
            if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));
            order = new int[count];
            random = new Random(seed);
            cursor = count;
        }

        public int Next()
        {
            if (order.Length == 0) return -1;
            if (cursor >= order.Length)
            {
                for (int i = 0; i < order.Length; i++) order[i] = i;
                for (int i = order.Length - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    int swap = order[i]; order[i] = order[j]; order[j] = swap;
                }
                if (order.Length > 1 && order[0] == previous)
                {
                    int j = random.Next(1, order.Length);
                    int swap = order[0]; order[0] = order[j]; order[j] = swap;
                }
                cursor = 0;
            }
            return previous = order[cursor++];
        }
    }
}

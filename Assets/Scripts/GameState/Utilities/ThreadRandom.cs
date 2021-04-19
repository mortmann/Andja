using System.Threading;

namespace Andja.Utility {

    public class ThreadRandom {
        public int Seed { get; private set; }
        private ThreadLocal<System.Random> random;

        public ThreadRandom() {
            random = new ThreadLocal<System.Random>(() => new System.Random());
            Seed = -Integer();
        }

        public ThreadRandom(int seed) {
            Seed = seed;
            random = new ThreadLocal<System.Random>(() => new System.Random(seed));
        }

        public int Range(int min, int max) {
            return random.Value.Next(min, max);
        }

        public float Range(float minimum, float maximum) {
            return (float)random.Value.NextDouble() * (maximum - minimum) + minimum;
        }

        public float Float() {
            return (float)random.Value.NextDouble();
        }

        public int Integer() {
            return random.Value.Next();
        }

        public int Next(int value) {
            return random.Value.Next(value);
        }

        public bool Bool() {
            //either 0 or 1
            return random.Value.Next(0, 2) == 0;
        }

        /// <summary>
        /// Returns a random long from min (inclusive) to max (exclusive)
        /// </summary>
        /// <param name="random">The given random instance</param>
        /// <param name="min">The inclusive minimum bound</param>
        /// <param name="max">The exclusive maximum bound.  Must be greater or equal to min</param>
        public long NextLong(long min, long max) {
            if (max < min)
                throw new System.ArgumentOutOfRangeException("max", "max must be >= min!");

            //Working with ulong so that modulo works correctly with values > long.MaxValue
            ulong uRange = (ulong)(max - min);

            //Loop to prevent a modolo bias; see http://stackoverflow.com/a/10984975/238419
            //for more information.
            //In the worst case, the expected number of calls is 2 (though usually it's
            //much closer to 1, depending on min/max) so this loop doesn't really hurt performance at all.
            ulong ulongRand;
            do {
                byte[] buf = new byte[8];
                random.Value.NextBytes(buf);
                ulongRand = (ulong)System.BitConverter.ToInt64(buf, 0);
            } while (ulongRand > ulong.MaxValue - ((ulong.MaxValue % uRange) + 1) % uRange);

            return (long)(ulongRand % uRange) + min;
        }

        /// <summary>
        /// Returns a random long from 0 (inclusive) to max (exclusive)
        /// </summary>
        /// <param name="random">The given random instance</param>
        /// <param name="max">The exclusive maximum bound.  Must be greater or equal to min</param>
        public long NextLong(long max) {
            return NextLong(0, max);
        }

        /// <summary>
        /// Returns a random long over all possible values of long (except long.MaxValue, similar to
        /// random.Next())
        /// </summary>
        /// <param name="random">The given random instance</param>
        public long NextLong() {
            return NextLong(long.MinValue, long.MaxValue);
        }
    }
}
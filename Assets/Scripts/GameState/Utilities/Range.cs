namespace Andja.Utility {

    public class Range {
        public int upper;
        public int lower;
        public int Middle => (lower + upper) / 2;

        public Range(int lower, int upper) : this() {
            this.lower = lower;
            this.upper = upper;
        }

        public Range() {
        }

        /// <summary>
        /// Returns true if the value is bigger/equal than min
        /// AND smaller(!) than max!
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool IsBetween(int value) {
            return value >= lower && value < upper;
        }

        internal int GetRandomCount(ThreadRandom threadRandom) {
            return threadRandom.Range(lower, upper + 1);
        }
    }
}
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Sums multiple values while fixing floating point errors.
    /// </summary>
    public sealed class AccurateSum {
        /// <summary>
        /// All values <see cref="Add(float)"/> was called with, added togerher accurately.
        /// </summary>
        public float Sum { get; private set; }

        /// <summary>
        /// Addition error that will be corrected in the next call of <see cref="Add(float)"/>.
        /// </summary>
        float c;

        /// <summary>
        /// Add a new <paramref name="value"/> to the sum.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(float value) {
            float y = value - c;
            float t = Sum + y;
            c = t - Sum - y;
            Sum = t;
        }

        /// <summary>
        /// Add new <paramref name="values"/> to the sum.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(float[] values) => Add(values, 0, values.Length);

        /// <summary>
        /// Add new <paramref name="values"/> to the sum between <paramref name="from"/> (inclusive) and <paramref name="to"/> (exclusive).
        /// </summary>
        public void Add(float[] values, int from, int to) {
            for (int i = from; i < to; i++) {
                Add(values[i]);
            }
        }
    }
}
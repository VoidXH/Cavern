using System;

using Cavern.Filters;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Applies the same real-coefficient convolution to a pair of arrays representing
    /// the real and imaginary parts of a complex signal.
    /// </summary>
    public sealed class PairConvolver : IDisposable {
        /// <summary>
        /// Convolution filter applied to the first array.
        /// </summary>
        public readonly FastConvolver first;

        /// <summary>
        /// Convolution filter applied to the second array.
        /// </summary>
        public readonly FastConvolver second;

        /// <summary>
        /// Creates a pair convolver from an impulse response shared by both arrays.
        /// </summary>
        public PairConvolver(float[] impulse) {
            first = new FastConvolver(impulse);
            second = new FastConvolver(impulse);
        }

        /// <summary>
        /// Applies the filter to both arrays in place.
        /// </summary>
        public void Process(float[] first, float[] second) {
            this.first.Process(first);
            this.second.Process(second);
        }

        /// <inheritdoc/>
        public void Dispose() {
            first.Dispose();
            second.Dispose();
        }
    }
}

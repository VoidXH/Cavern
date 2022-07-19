using Cavern.Filters;

namespace Benchmark.Benchmarks {
    /// <summary>
    /// Benchmarks Cavern's <see cref="FastConvolver"/> filter.
    /// </summary>
    internal class Convolution : Benchmark {
        readonly int length;
        readonly FastConvolver filter;

        /// <summary>
        /// Constructs a benchmark for Cavern's <see cref="FastConvolver"/> filter.
        /// </summary>
        public Convolution(int length) {
            this.length = length;
            filter = new FastConvolver(new float[length]);
        }

        /// <summary>
        /// Performs one round of the benchmark. Results should be displayed in actions/second.
        /// </summary>
        public override void Step() => filter.Process(new float[length]);
    }
}
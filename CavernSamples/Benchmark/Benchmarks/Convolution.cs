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
        /// Performs one round of the benchmark.
        /// </summary>
        public override void Step() => filter.Process(new float[length]);

        /// <summary>
        /// Displays the result of the benchmark in a relevant metric.
        /// </summary>
        public override string ToString(int steps, int seconds) =>
            $"{steps * length / (float)(seconds * 48000):0.0} seconds of audio processed every second at 48 kHz";
    }
}
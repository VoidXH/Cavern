using Cavern.Filters;

namespace Benchmark.Benchmarks {
    /// <summary>
    /// Benchmarks Cavern's <see cref="FastConvolver"/> filter.
    /// </summary>
    internal class Convolution : Benchmark {
        readonly FastConvolver filter;

        /// <summary>
        /// The example audio block to process.
        /// </summary>
        readonly float[] block;

        /// <summary>
        /// Constructs a benchmark for Cavern's <see cref="FastConvolver"/> filter.
        /// </summary>
        public Convolution(int length, int blockSize) {
            filter = new FastConvolver(new float[length]);
            block = new float[blockSize];
        }

        /// <summary>
        /// Performs one round of the benchmark.
        /// </summary>
        public override void Step() => filter.Process(block);

        /// <summary>
        /// Displays the result of the benchmark in a relevant metric.
        /// </summary>
        public override string ToString(int steps, int seconds) =>
            $"{steps * block.Length / (float)(seconds * 48000):0.0} seconds of audio processed every second at 48 kHz";
    }
}
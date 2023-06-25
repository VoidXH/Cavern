using Cavern;
using Cavern.Filters;

namespace Benchmark.Benchmarks {
    /// <summary>
    /// Benchmarks Cavern's <see cref="FastConvolver"/> filter.
    /// </summary>
    internal class Convolution : Benchmark {
        /// <summary>
        /// Instance of the used convolution filter.
        /// </summary>
        readonly Filter filter;

        /// <summary>
        /// Samples processed per channel per frame. Used for calculating performance.
        /// </summary>
        readonly int blockSize;

        /// <summary>
        /// The example audio block to process.
        /// </summary>
        readonly float[] block;

        /// <summary>
        /// Constructs a benchmark for Cavern's <see cref="FastConvolver"/> filter.
        /// </summary>
        public Convolution(int length, int blockSize, int channels) {
            this.blockSize = blockSize;
            if (channels == 1) {
                filter = new FastConvolver(new float[length]);
                block = new float[blockSize];
            } else {
                filter = new MultichannelConvolver(new MultichannelWaveform(channels, length));
                block = new float[blockSize * channels];
            }
        }

        /// <summary>
        /// Performs one round of the benchmark.
        /// </summary>
        public override void Step() => filter.Process(block);

        /// <summary>
        /// Displays the result of the benchmark in a relevant metric.
        /// </summary>
        public override string ToString(int steps, int seconds) =>
            $"{steps * blockSize / (float)(seconds * 48000):0.0} seconds of audio processed every second at 48 kHz";
    }
}
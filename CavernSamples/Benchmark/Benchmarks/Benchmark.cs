namespace Benchmark.Benchmarks {
    /// <summary>
    /// Benchmark base class.
    /// </summary>
    abstract class Benchmark {
        /// <summary>
        /// Performs one round of the benchmark. Results should be displayed in actions/second.
        /// </summary>
        public abstract void Step();
    }
}
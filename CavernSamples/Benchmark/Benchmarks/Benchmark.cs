namespace Benchmark.Benchmarks {
    /// <summary>
    /// Benchmark base class.
    /// </summary>
    abstract class Benchmark {
        /// <summary>
        /// Performs one round of the benchmark.
        /// </summary>
        public abstract void Step();

        /// <summary>
        /// Displays the result of the benchmark in a relevant metric.
        /// </summary>
        public virtual string ToString(int steps, int seconds) {
            return $"{steps / (float)seconds:0.0} operations/second";
        }
    }
}
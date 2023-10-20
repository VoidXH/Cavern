using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Cavern.Utilities {
    /// <summary>
    /// High-performance version of the <see cref="Parallel"/> class.
    /// </summary>
    public class Parallelizer : IDisposable {
        /// <summary>
        /// Waits while started tasks work.
        /// </summary>
        readonly ManualResetEventSlim taskWaiter = new ManualResetEventSlim(false);

        /// <summary>
        /// The action to perform if an instance is created and not the static methods are called.
        /// </summary>
        readonly Action<int> action;

        /// <summary>
        /// Remaining number of runs.
        /// </summary>
        int runs;

        /// <summary>
        /// Creates a parallel runner for a predetermined <paramref name="action"/>.
        /// </summary>
        public Parallelizer(Action<int> action) => this.action = action;

        /// <summary>
        /// Performs the <paramref name="action"/> for all values between <paramref name="start"/> (inclusive) and
        /// <paramref name="end"/> (exclusive) in parallel, while blocking the calling thread.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void For(int start, int end, Action<int> action) {
            using Parallelizer processor = new Parallelizer(action);
            processor.For(start, end);
        }

        /// <summary>
        /// Performs the <see cref="action"/> for all values between <paramref name="start"/> (inclusive) and
        /// <paramref name="end"/> (exclusive) in parallel, while blocking the calling thread.
        /// </summary>
        public void For(int start, int end) {
            runs = end - start;
            taskWaiter.Reset();
            for (int i = start; i < end; i++) {
                ThreadPool.QueueUserWorkItem(Step, i);
            }
            if (runs == 0) {
                return;
            }
            taskWaiter.Wait();
        }

        /// <summary>
        /// Frees up resources used by the object.
        /// </summary>
        public void Dispose() => taskWaiter.Dispose();

        /// <summary>
        /// The function to be called on a worker thread.
        /// </summary>
        void Step(object iteration) {
            action((int)iteration);
            if (Interlocked.Decrement(ref runs) == 0) {
                taskWaiter.Set();
            }
        }
    }
}
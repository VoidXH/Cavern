using System;
using System.Threading;

namespace Cavern.Utilities.Threading {
    /// <summary>
    /// Runs a background thread with exception handling-ready polling.
    /// </summary>
    public class BackgroundProccess : IDisposable {
        /// <summary>
        /// Check if the <see cref="thread"/> has finished executing. If it had ran to an exception, that exception is thrown.
        /// </summary>
        public bool Done {
            get {
                if (threadException != null) {
                    throw new AggregateException(threadException);
                }
                return thread != null && !thread.IsAlive;
            }
        }

        /// <summary>
        /// The background process.
        /// </summary>
        Thread thread;

        /// <summary>
        /// Contains why the background operation had failed.
        /// </summary>
        Exception threadException;

        /// <summary>
        /// Run the <paramref name="action"/> in the background <see cref="thread"/>.
        /// </summary>
        /// <param name="action"></param>
        public void Run(Action action) {
            thread = new Thread(() => {
                try {
                    action();
                } catch (Exception e) {
                    threadException = e;
                }
            });
            thread.Start();
        }

        /// <summary>
        /// Resets the object to a default state by clearing the handle and recoded exception. It's only valid to call this, when the previous thread was finished,
        /// otherwise an exception will be thrown.
        /// </summary>
        public void Reset() {
            if (thread != null && thread.IsAlive) {
                throw new InvalidOperationException("The thread is still alive.");
            }
            thread = null;
            threadException = null;
        }

        /// <inheritdoc/>
        public void Dispose() => thread?.Join();
    }
}

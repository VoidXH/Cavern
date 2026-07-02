using System.Diagnostics;
using System.Threading;

namespace Cavern.Format.Networking {
    /// <summary>
    /// Achieve as good of a PTP clock in software as possible. To further reduce jitter, native code or hardware PTP is required.
    /// </summary>
    /// <remarks>This code will hard-spin. To keep the OS taking control away from it, and prevent jitter spikes, set process priority to above normal.
    /// GC could also be an issue, so use this in an allocation-free thread.</remarks>
    public class SystemPTPClock : IPTPClock {
        /// <summary>
        /// Reference time of 0 offset.
        /// </summary>
        readonly long startTimestamp;

        /// <summary>
        /// Achieve as good of a PTP clock in software as possible. To further reduce jitter, native code or hardware PTP is required.
        /// </summary>
        public SystemPTPClock() => startTimestamp = Stopwatch.GetTimestamp();

        /// <inheritdoc/>
        public long GetCurrentTimeNanoseconds() {
            long elapsedTicks = Stopwatch.GetTimestamp() - startTimestamp;

            // Standard high-res timer (e.g. 10 MHz, Windows)
            if (StopwatchFrequency <= NanosecondsPerSecond) {
                return elapsedTicks * NanosecondsPerSecond / StopwatchFrequency;
            }

            // High-frequency TSC overflow protection (e.g. 3.5 GHz, some Linux)
            long quotient = elapsedTicks / StopwatchFrequency;
            long remainder = elapsedTicks % StopwatchFrequency;
            return quotient * NanosecondsPerSecond + remainder * NanosecondsPerSecond / StopwatchFrequency;
        }

        /// <inheritdoc/>
        public void WaitUntil(long targetTimeNanoseconds) {
            SpinWait spinner = new SpinWait();
            while (true) {
                long remainingNs = targetTimeNanoseconds - GetCurrentTimeNanoseconds();
                if (remainingNs <= 0) {
                    break;
                }

                if (remainingNs > SpinThresholdNs) {
                    spinner.SpinOnce();
                } else {
                    Thread.SpinWait(1);
                }
            }
        }

        /// <summary>
        /// Cached frequency of the <see cref="Stopwatch"/> to avoid static field lookups in hot loops.
        /// </summary>
        static readonly long StopwatchFrequency = Stopwatch.Frequency;

        /// <summary>
        /// Threshold where we hard-spin to maintain low jitter. 40 000 nanoseconds = 40 microseconds.
        /// </summary>
        const long SpinThresholdNs = 40_000;

        /// <summary>
        /// Conversion unit between nanoseconds and seconds.
        /// </summary>
        const long NanosecondsPerSecond = 1_000_000_000;
    }
}

namespace Cavern.Format.Networking {
    /// <summary>
    /// Represents the IEEE 1588-2008 Precision Time Protocol (PTP) hardware or system clock.
    /// In a true PTP environment, this reads from the NIC's hardware timestamping registers or a highly tuned OS clock disciplined by a PTP daemon (e.g., ptp4l).
    /// </summary>
    public interface IPTPClock {
        /// <summary>
        /// Gets the current PTP time in nanoseconds since the epoch.
        /// </summary>
        long GetCurrentTimeNanoseconds();

        /// <summary>
        /// Blocks or spins until the specified PTP time is reached.
        /// </summary>
        void WaitUntil(long targetTimeNanoseconds);
    }
}

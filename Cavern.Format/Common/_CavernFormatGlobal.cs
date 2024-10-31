namespace Cavern.Format.Common {
    /// <summary>
    /// Settings related to the entirety of the Cavern.Format library.
    /// </summary>
    public static class CavernFormatGlobal {
        /// <summary>
        /// Disables checks for conditions that don't inherently break operation, but are mandated by standards.
        /// </summary>
        public static bool Unsafe { get; set; }
    }
}
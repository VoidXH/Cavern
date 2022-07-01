namespace Cavern.Format.Consts {
    /// <summary>
    /// Constants needed across the entire library.
    /// </summary>
    static class FormatConsts {
        /// <summary>
        /// Only read 10 MB blocks at max to optimize I/O performance.
        /// </summary>
        public const int blockSize = 10 * 1024 * 1024;
    }
}
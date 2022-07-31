using System;
using System.IO;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// TODO: make and use this
    /// Like <see cref="Stream"/>, but faster. Caches multiple segments, expands read sizes exponentially
    /// to support both small and large chunks, collects garbage like a CPU cache.
    /// </summary>
    internal class FastFileStream : IDisposable {
        /// <summary>
        /// The smallest number of bytes to read. This is the size of the smallest possible read from a disk.
        /// </summary>
        const long minReadSize = 512;

        /// <summary>
        /// File system handle to the target file.
        /// </summary>
        readonly Stream stream;

        /// <summary>
        /// Number of bytes to cache at max.
        /// </summary>
        readonly long maxCacheSize;

        /// <summary>
        /// Open a file for reading with this class.
        /// </summary>
        /// <param name="fileName">Path to open the file from</param>
        /// <param name="maxCacheSize">Number of bytes to cache at max (default: 50 MB)</param>
        public FastFileStream(string fileName, long maxCacheSize = 50 * 1024 * 1024) {
            stream = File.OpenRead(fileName);
            this.maxCacheSize = maxCacheSize;
        }

        /// <summary>
        /// Close the file handle.
        /// </summary>
        public void Dispose() => stream.Dispose();
    }
}
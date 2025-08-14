using System.IO;

namespace Cavern.Utilities {
    /// <summary>
    /// Shorthand for common file operations.
    /// </summary>
    public static class QFile {
        /// <summary>
        /// Delete a file only if it exists.
        /// </summary>
        public static void DeleteIfExists(string path) {
            if (File.Exists(path)) {
                File.Delete(path);
            }
        }
    }
}

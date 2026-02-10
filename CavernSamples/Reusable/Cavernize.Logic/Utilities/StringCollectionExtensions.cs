using System.Collections.Specialized;

namespace Cavernize.Logic.Utilities;

/// <summary>
/// Extension functions for <see cref="StringCollection"/>s.
/// </summary>
public static class StringCollectionExtensions {
    /// <summary>
    /// Make the collection contain only file names, even recursively if a path is a subdirectory.
    /// </summary>
    public static void FlattenPaths(this StringCollection paths) {
        for (int i = 0, c = paths.Count; i < c; i++) {
            if (Directory.Exists(paths[i])) {
                string[] subdirs = Directory.GetDirectories(paths[i]);
                string[] files = Directory.GetFiles(paths[i]);
                paths.RemoveAt(i);
                i--;
                paths.AddRange(subdirs);
                paths.AddRange(files);
                c = paths.Count;
            }
        }
    }
}

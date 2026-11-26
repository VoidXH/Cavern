using System.IO;

namespace Cavern.Utilities {
    /// <summary>
    /// Provides additional library functionality for <see cref="IBase64Serializable"/> classes.
    /// </summary>
    public static class IBase64SerializableExtensions {
        /// <summary>
        /// Serializes the object to a base64-encoded string and saves it to a file.
        /// </summary>
        /// <param name="instance">The object to save</param>
        /// <param name="path">The file path to save to</param>
        public static void SaveToFile(this IBase64Serializable instance, string path) => File.WriteAllText(path, instance.ToBase64());

        /// <summary>
        /// Loads a base64-encoded string from a file and deserializes it into the object.
        /// </summary>
        /// <param name="instance">The object to load into</param>
        /// <param name="path">The file path to load from</param>
        public static void LoadFromFile(this IBase64Serializable instance, string path) => instance.FromBase64(File.ReadAllText(path));
    }
}

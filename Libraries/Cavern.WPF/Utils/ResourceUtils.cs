using System.Windows;

namespace Cavern.WPF.Utils;

/// <summary>
/// Extension methods for WPF Resource handling.
/// </summary>
public static class ResourceUtils {
    /// <summary>
    /// Try to read a value from a <paramref name="dictionary"/> by <paramref name="key"/>, and if it doesn't exist,
    /// <paramref name="fallback"/> to a default value.
    /// </summary>
    public static string TryGet(this ResourceDictionary dictionary, string key, string fallback) {
        try {
            return (string)dictionary[key];
        } catch {
            return fallback;
        }
    }
}

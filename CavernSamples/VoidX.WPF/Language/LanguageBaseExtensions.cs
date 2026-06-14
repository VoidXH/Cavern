using System.Windows;

using VoidX.WPF.Language;

namespace Cavern.WPF.Utils;

/// <summary>
/// Extension functions for <see cref="LanguageBase{T}"/>.
/// </summary>
public static class LanguageBaseExtensions {
    /// <summary>
    /// Get the contained values of a <see cref="LanguageBase{T}"/> in WPF's <see cref="ResourceDictionary"/> format.
    /// </summary>
    public static ResourceDictionary ToResourceDictionary<T>(this LanguageBase<T> language) where T : LanguageBase<T>, new() {
        ResourceDictionary result = [];
        foreach (string key in language.Keys) {
            result[key] = language[key];
        }
        return result;
    }
}

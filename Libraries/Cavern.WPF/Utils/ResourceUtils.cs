using System.Globalization;
using System.Windows;
using System.Windows.Resources;

namespace Cavern.WPF.Utils;

/// <summary>
/// Extension methods for WPF Resource handling.
/// </summary>
public static class ResourceUtils {
    /// <summary>
    /// Get the translation of a resource file in the user's language, falling back to English if it exists.
    /// </summary>
    /// <param name="resource">Name of the resource file</param>
    /// <param name="supported">List of supported cultures</param>
    /// <param name="cultureOverride">Force a culture instead of taking the system's culture</param>
    public static ResourceDictionary GetTranslationFor(string resource, string[] supported, string cultureOverride = null) {
        ResourceDictionary finalDict = [];
        Uri baseUri = new($";component/Resources/{resource}.xaml", UriKind.RelativeOrAbsolute);
        if (ResourceExists(baseUri)) {
            finalDict.MergedDictionaries.Add(new ResourceDictionary {
                Source = baseUri
            });
        }

        string culture = cultureOverride;
        if (string.IsNullOrEmpty(culture)) {
            culture = CultureInfo.CurrentUICulture.Name;
        } else if (culture == "en-US") { // Forced default
            culture = string.Empty;
        }

        if (!string.IsNullOrEmpty(culture) && Array.BinarySearch(supported, culture) >= 0) {
            Uri translatedUri = new($";component/Resources/{resource}.{culture}.xaml", UriKind.RelativeOrAbsolute);
            ResourceDictionary translatedDict = new() {
                Source = translatedUri
            };
            finalDict.MergedDictionaries.Add(translatedDict);
        }

        return finalDict;
    }

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

    /// <summary>
    /// Check if a WPF component resource exists without throwing exceptions.
    /// </summary>
    static bool ResourceExists(Uri uri) {
        try {
            StreamResourceInfo info = Application.GetResourceStream(uri);
            return info != null;
        } catch (System.IO.IOException) {
            return false;
        }
    }
}

using System;
using System.Globalization;
using System.Windows;

using Cavernize.Logic.Language;
using CavernizeGUI.Language;
using CavernizeGUI.Resources;
using CavernizeGUI.Windows;

namespace CavernizeGUI.Consts {
    /// <summary>
    /// Handle fetching of language strings and translations.
    /// </summary>
    static class Language {
        /// <summary>
        /// Get the <see cref="MainWindow"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetMainWindowStrings() => mainWindowCache ??= GetFor("MainWindowStrings");

        /// <summary>
        /// Get the <see cref="MainWindow"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetTrackStrings() => trackCache ??= GetFor("TrackStrings");

        /// <summary>
        /// Get the external converters' translation.
        /// </summary>
        public static ExternalConverterStrings GetExternalConverterStrings() => externalConverterCache ??= IsDefaultLanguage() ?
            new ExternalConverterStrings() :
            new DynamicExternalConverterStrings(GetFor("ExternalConverterStrings"));

        /// <summary>
        /// Get the post-render report dialog's translation.
        /// </summary>
        public static RenderReportStrings GetRenderReportStrings() => renderReportCache ??= IsDefaultLanguage() ?
            new RenderReportStrings() :
            new DynamicRenderReportStrings(GetFor("RenderReportStrings"));

        /// <summary>
        /// Get the <see cref="RenderTargetSelector"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetRenderTargetSelectorStrings() => GetFor("RenderTargetSelectorStrings");

        /// <summary>
        /// Get the translation of a resource file in the user's language, or in English if a translation couldn't be found.
        /// </summary>
        static ResourceDictionary GetFor(string resource) {
            string culture = Settings.Default.language;
            if (string.IsNullOrEmpty(culture)) {
                culture = CultureInfo.CurrentUICulture.Name;
            } else if (culture == "en-US") { // Forced default
                culture = string.Empty;
            }

            if (Array.BinarySearch(supported, culture) >= 0) {
                resource += '.' + culture;
            }
            return new() {
                Source = new Uri($";component/Resources/{resource}.xaml", UriKind.RelativeOrAbsolute)
            };
        }

        /// <summary>
        /// Checks if the system is set to a language that has no available localization.
        /// </summary>
        static bool IsDefaultLanguage() {
            string culture = Settings.Default.language;
            if (string.IsNullOrEmpty(culture)) {
                culture = CultureInfo.CurrentUICulture.Name;
            }
            return Array.BinarySearch(supported, culture) < 0;
        }

        /// <summary>
        /// Languages supported that are not the default English.
        /// </summary>
        static readonly string[] supported = ["hu-HU"];

        /// <summary>
        /// The loaded translation of the <see cref="MainWindow"/> for reuse.
        /// </summary>
        static ResourceDictionary mainWindowCache;

        /// <summary>
        /// The loaded translation of <see cref="Track"/>s for reuse.
        /// </summary>
        static ResourceDictionary trackCache;

        /// <summary>
        /// The loaded translation of external converter handling for reuse.
        /// </summary>
        static ExternalConverterStrings externalConverterCache;

        /// <summary>
        /// The loaded translation of the post-render report dialog for reuse.
        /// </summary>
        static RenderReportStrings renderReportCache;
    }
}
using System;
using System.Globalization;
using System.Windows;

using Cavern.WPF.Utils;

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
        public static ResourceDictionary GetMainWindowStrings() =>
            mainWindowCache ??= ResourceUtils.GetTranslationFor("MainWindowStrings", supported, Settings.Default.language);

        /// <summary>
        /// Get the <see cref="MainWindow"/>'s translation.
        /// </summary>
        public static TrackStrings GetTrackStrings() => trackCache ??= IsDefaultLanguage() ?
            new TrackStrings() :
            new DynamicTrackStrings(ResourceUtils.GetTranslationFor("TrackStrings", supported, Settings.Default.language));

        /// <summary>
        /// Get the post-render report dialog's translation.
        /// </summary>
        public static RenderReportStrings GetRenderReportStrings() => renderReportCache ??= IsDefaultLanguage() ?
            new RenderReportStrings() :
            new DynamicRenderReportStrings(ResourceUtils.GetTranslationFor("RenderReportStrings", supported, Settings.Default.language));

        /// <summary>
        /// Get the <see cref="RenderTargetSelector"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetRenderTargetSelectorStrings() =>
            ResourceUtils.GetTranslationFor("RenderTargetSelectorStrings", supported, Settings.Default.language);

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
        static TrackStrings trackCache;

        /// <summary>
        /// The loaded translation of the post-render report dialog for reuse.
        /// </summary>
        static RenderReportStrings renderReportCache;
    }
}

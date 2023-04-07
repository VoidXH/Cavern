using System;
using System.Globalization;
using System.Windows;

using CavernizeGUI.Elements;
using CavernizeGUI.Windows;

namespace CavernizeGUI.Consts {
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
        /// Get the post-render report dialog's translation.
        /// </summary>
        public static ResourceDictionary GetRenderReportStrings() => renderReportCache ??= GetFor("RenderReport");

        /// <summary>
        /// Get the <see cref="RenderTargetSelector"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetRenderTargetSelectorStrings() => GetFor("RenderTargetSelectorStrings");

        /// <summary>
        /// Get the <see cref="UpmixingSetup"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetUpmixingSetupStrings() => GetFor("UpmixingSetupStrings");

        /// <summary>
        /// Get the translation of a resource file in the user's language, or in English if a translation couldn't be found.
        /// </summary>
        static ResourceDictionary GetFor(string resource) {
            if (Array.BinarySearch(supported, CultureInfo.CurrentUICulture.Name) >= 0) {
                resource += '.' + CultureInfo.CurrentUICulture.Name;
            }
            return new() {
                Source = new Uri($";component/Resources/{resource}.xaml", UriKind.RelativeOrAbsolute)
            };
        }

        /// <summary>
        /// Languages supported that are not the default English.
        /// </summary>
        static readonly string[] supported = { "hu-HU" };

        /// <summary>
        /// The loaded translation of the <see cref="MainWindow"/> for reuse.
        /// </summary>
        static ResourceDictionary mainWindowCache;

        /// <summary>
        /// The loaded translation of <see cref="Track"/>s for reuse.
        /// </summary>
        static ResourceDictionary trackCache;

        /// <summary>
        /// The loaded translation of the post-render report dialog for reuse.
        /// </summary>
        static ResourceDictionary renderReportCache;
    }
}
using System.Windows;

using Cavern.WPF.Utils;

using CavernizeGUI.Resources;

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
        /// Languages supported that are not the default English.
        /// </summary>
        static readonly string[] supported = ["hu-HU"];

        /// <summary>
        /// The loaded translation of the <see cref="MainWindow"/> for reuse.
        /// </summary>
        static ResourceDictionary mainWindowCache;
    }
}

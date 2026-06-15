using System.Windows;

using Cavern.WPF.Utils;

using FilterStudio.Windows;
using FilterStudio.Windows.PipelineSteps;

namespace FilterStudio.Consts {
    /// <summary>
    /// Handle fetching of language strings and translations.
    /// </summary>
    static class Language {
        /// <summary>
        /// Get the <see cref="MainWindow"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetMainWindowStrings() => mainWindowCache ??= ResourceUtils.GetTranslationFor("MainWindowStrings", supported);

        /// <summary>
        /// Get the <see cref="ConvolutionLengthDialog"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetConvolutionLengthDialogStrings() =>
            convolutionLengthDialogCache ??= ResourceUtils.GetTranslationFor("ConvolutionLengthDialogStrings", supported);

        /// <summary>
        /// Get the <see cref="CrossoverDialog"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetCrossoverDialogStrings() => crossoverDialogCache ??= ResourceUtils.GetTranslationFor("CrossoverDialogStrings", supported);

        /// <summary>
        /// Get the <see cref="RenameDialog"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetRenameDialogStrings() => renameDialogCache ??= ResourceUtils.GetTranslationFor("RenameDialogStrings", supported);

        /// <summary>
        /// Languages supported that are not the default English.
        /// </summary>
        static readonly string[] supported = ["hu-HU"];

        /// <summary>
        /// The loaded translation of the <see cref="MainWindow"/> for reuse.
        /// </summary>
        static ResourceDictionary mainWindowCache;

        /// <summary>
        /// The loaded translation of the <see cref="ConvolutionLengthDialog"/> for reuse.
        /// </summary>
        static ResourceDictionary convolutionLengthDialogCache;

        /// <summary>
        /// The loaded translation of the <see cref="CrossoverDialog"/> for reuse.
        /// </summary>
        static ResourceDictionary crossoverDialogCache;

        /// <summary>
        /// The loaded translation of the <see cref="RenameDialog"/> for reuse.
        /// </summary>
        static ResourceDictionary renameDialogCache;
    }
}

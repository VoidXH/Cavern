using System.Globalization;
using System.Windows;

namespace Cavern.WPF.Consts {
    /// <summary>
    /// Handle fetching of language strings and translations.
    /// </summary>
    static class Language {
        /// <summary>
        /// Get the shared translation between windows.
        /// </summary>
        public static ResourceDictionary GetCommonStrings() => commonCache ??= GetFor("CommonStrings");

        /// <summary>
        /// Get the <see cref="BiquadEditor"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetBiquadEditorStrings() => biquadEditorCache ??= GetFor("BiquadEditorStrings");

        /// <summary>
        /// Get the <see cref="ChannelSelector"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetChannelSelectorStrings() => channelSelectorCache ??= GetFor("ChannelSelectorStrings");

        /// <summary>
        /// Get the translation of a resource file in the user's language, or in English if a translation couldn't be found.
        /// </summary>
        static ResourceDictionary GetFor(string resource) {
            if (Array.BinarySearch(supported, CultureInfo.CurrentUICulture.Name) >= 0) {
                resource += '.' + CultureInfo.CurrentUICulture.Name;
            }
            return new() {
                Source = new Uri($"/Cavern.WPF;component/Resources/{resource}.xaml", UriKind.RelativeOrAbsolute)
            };
        }

        /// <summary>
        /// Languages supported that are not the default English.
        /// </summary>
        static readonly string[] supported = ["hu-HU"];

        /// <summary>
        /// The loaded translation of shared strings for reuse.
        /// </summary>
        static ResourceDictionary commonCache;

        /// <summary>
        /// The loaded translation of the <see cref="BiquadEditor"/> for reuse.
        /// </summary>
        static ResourceDictionary biquadEditorCache;

        /// <summary>
        /// The loaded translation of the <see cref="ChannelSelector"/> for reuse.
        /// </summary>
        static ResourceDictionary channelSelectorCache;
    }
}
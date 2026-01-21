using System.Globalization;
using System.Windows;

using Cavern.Channels;
using Cavern.QuickEQ.Crossover;

namespace Cavern.WPF.Consts {
    /// <summary>
    /// Handle fetching of language strings and translations.
    /// </summary>
    public static class Language {
        /// <summary>
        /// Overrides the language of Cavern.WPF dialogs. If null, the system language is used.
        /// </summary>
        public static string Override { get; set; }

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
        /// Get the <see cref="ConvolutionEditor"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetConvolutionEditorStrings() => convolutionEditorCache ??= GetFor("ConvolutionEditorStrings");

        /// <summary>
        /// Get the translations related to crossover handling.
        /// </summary>
        public static ResourceDictionary GetCrossoverStrings() => crossoverCache ??= GetFor("CrossoverStrings");

        /// <summary>
        /// Get the <see cref="EQEditor"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetEQEditorStrings() => eqEditorCache ??= GetFor("EQEditorStrings");

        /// <summary>
        /// Get the <see cref="FilterSetTargetSelector"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetFilterSetTargetSelectorStrings() => GetFor("FilterSetTargetSelectorStrings");

        /// <summary>
        /// Get the <see cref="UpmixingSetup"/>'s translation.
        /// </summary>
        public static ResourceDictionary GetUpmixingSetupStrings() => upmixingSetupCache ??= GetFor("UpmixingSetupStrings");

        /// <summary>
        /// Show an error <paramref name="message"/> with the title in the user's language.
        /// </summary>
        public static void Error(string message) =>
            MessageBox.Show(message, (string)GetCommonStrings()["TErro"], MessageBoxButton.OK, MessageBoxImage.Error);

        /// <summary>
        /// Show a warning <paramref name="message"/> with the title in the user's language.
        /// </summary>
        public static MessageBoxResult Warning(string message) =>
            MessageBox.Show(message, (string)GetCommonStrings()["TWarn"], MessageBoxButton.OK, MessageBoxImage.Warning);

        /// <summary>
        /// Show a warning <paramref name="message"/> with the title in the user's language.
        /// </summary>
        public static MessageBoxResult Warning(string message, MessageBoxButton buttons) =>
            MessageBox.Show(message, (string)GetCommonStrings()["TWarn"], buttons, MessageBoxImage.Warning);

        /// <summary>
        /// Return a channel's name in the user's language or fall back to its short name.
        /// </summary>
        public static string Translate(this ReferenceChannel channel) {
            ResourceDictionary dictionary = GetChannelSelectorStrings();
            return channel switch {
                ReferenceChannel.FrontLeft => (string)dictionary["SpGFL"],
                ReferenceChannel.FrontLeftCenter => (string)dictionary["SpFLC"],
                ReferenceChannel.FrontCenter => (string)dictionary["SpGFC"],
                ReferenceChannel.FrontRightCenter => (string)dictionary["SpFRC"],
                ReferenceChannel.FrontRight => (string)dictionary["SpGFR"],
                ReferenceChannel.WideLeft => (string)dictionary["SpGWL"],
                ReferenceChannel.WideRight => (string)dictionary["SpGWR"],
                ReferenceChannel.SideLeft => (string)dictionary["SpGSL"],
                ReferenceChannel.ScreenLFE => (string)dictionary["SpLFE"],
                ReferenceChannel.SideRight => (string)dictionary["SpGSR"],
                ReferenceChannel.RearLeft => (string)dictionary["SpGRL"],
                ReferenceChannel.RearCenter => (string)dictionary["SpGRC"],
                ReferenceChannel.RearRight => (string)dictionary["SpGRR"],
                ReferenceChannel.TopFrontLeft => (string)dictionary["SpTFL"],
                ReferenceChannel.TopFrontCenter => (string)dictionary["SpTFC"],
                ReferenceChannel.TopFrontRight => (string)dictionary["SpTFR"],
                ReferenceChannel.TopSideLeft => (string)dictionary["SpTSL"],
                ReferenceChannel.GodsVoice => (string)dictionary["SpTGV"],
                ReferenceChannel.TopSideRight => (string)dictionary["SpTSR"],
                ReferenceChannel.TopRearLeft => (string)dictionary["SpTRL"],
                ReferenceChannel.TopRearCenter => (string)dictionary["SpTRC"],
                ReferenceChannel.TopRearRight => (string)dictionary["SpTRR"],
                ReferenceChannel.Unknown => (string)dictionary["SpUnk"],
                _ => channel.GetShortName()
            };
        }

        /// <summary>
        /// Return a crossover type's name in the user's language or fall back to its enum name.
        /// </summary>
        public static string Translate(this CrossoverType type) {
            ResourceDictionary dictionary = GetCrossoverStrings();
            return type switch {
                CrossoverType.Biquad => (string)dictionary["TyBiq"],
                CrossoverType.Cavern => "Cavern",
                CrossoverType.SyntheticBiquad => (string)dictionary["TySBi"],
                _ => type.ToString()
            };
        }

        /// <summary>
        /// Get the translation of a resource file in the user's language, or in English if a translation couldn't be found.
        /// </summary>
        static ResourceDictionary GetFor(string resource) {
            string culture = Override;
            if (string.IsNullOrEmpty(culture)) {
                culture = CultureInfo.CurrentUICulture.Name;
            } else if (culture == "en-US") { // Forced default
                culture = string.Empty;
            }

            if (Array.BinarySearch(supported, culture) >= 0) {
                resource += '.' + culture;
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

        /// <summary>
        /// The loaded translation of the <see cref="ConvolutionEditor"/> for reuse.
        /// </summary>
        static ResourceDictionary convolutionEditorCache;

        /// <summary>
        /// The loaded translation of crossover handling for reuse.
        /// </summary>
        static ResourceDictionary crossoverCache;

        /// <summary>
        /// The loaded translation of the <see cref="EQEditor"/> for reuse.
        /// </summary>
        static ResourceDictionary eqEditorCache;

        /// <summary>
        /// The loaded translation of the <see cref="UpmixingSetup"/> for reuse.
        /// </summary>
        static ResourceDictionary upmixingSetupCache;
    }
}
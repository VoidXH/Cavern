using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;

using Cavern.Channels;

namespace Cavern.WPF.Consts {
    /// <summary>
    /// Extension functions for calculating translated text.
    /// </summary>
    public static class LanguageExtensions {
        /// <summary>
        /// Display how a set of spatial <paramref name="channels"/> shall be wired for regular audio interfaces.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void DisplayWiring(this ReferenceChannel[] channels) => channels.DisplayWiring(null);

        /// <summary>
        /// Display how a set of spatial <paramref name="channels"/> shall be wired for regular audio interfaces when some channels are
        /// <paramref name="matrixed"/> into 8 channels to be extracted when the speaker is wired to two positive terminals. These matrixed channels
        /// are not part of the base <paramref name="channels"/>.
        /// </summary>
        public static void DisplayWiring(this ReferenceChannel[] channels,
            (ReferenceChannel source, ReferenceChannel posPhase, ReferenceChannel negPhase)[] matrixed) {
            ResourceDictionary language = Language.GetChannelSelectorStrings();
            ChannelPrototype[] prototypes = ChannelPrototype.Get(channels);
            if (channels.Length > 8) {
                MessageBox.Show(string.Format((string)language["Over8"], string.Join(string.Empty, prototypes.Select(x => "\n- " + x.Name)),
                    (string)language["WrGui"]));
                return;
            }

            StringBuilder output = new StringBuilder();
            ReferenceChannel[] standard = ChannelPrototype.GetStandardMatrix(prototypes.Length);
            for (int i = 0; i < prototypes.Length; i++) {
                output.AppendLine(string.Format((string)language["ChCon"], channels[i].Translate(), standard[i].Translate()));
            }
            if (matrixed != null) {
                for (int i = 0; i < matrixed.Length; i++) {
                    output.AppendLine(string.Format((string)language["ChCMx"],
                        matrixed[i].source.Translate(), matrixed[i].posPhase.Translate(), matrixed[i].negPhase.Translate()));
                }
            }
            MessageBox.Show(output.ToString(), (string)language["WrGui"]);
        }
    }
}
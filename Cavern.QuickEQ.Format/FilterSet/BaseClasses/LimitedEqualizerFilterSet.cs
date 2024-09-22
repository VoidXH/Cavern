using System.IO;
using System.Text;

using Cavern.Channels;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet.BaseClasses {
    /// <summary>
    /// A fixed set of bands to sample from an <see cref="Equalizer"/> for export into a single file. This is the recommended and fastest
    /// approach of getting a filter set for incoherent fixed EQ bands, such as the <see cref="SonyESSeriesFilterSet"/>.
    /// </summary>
    public abstract class LimitedEqualizerFilterSet : EqualizerFilterSet {
        /// <summary>
        /// All frequency bands that need to be set.
        /// </summary>
        protected abstract float[] Frequencies { get; }

        /// <summary>
        /// Frequency bands for the LFE channel.
        /// </summary>
        protected abstract float[] LFEFrequencies { get; }

        /// <summary>
        /// How much smoothing in octaves shall be applied on the results to have a precise enough averaged value at each used frequency.
        /// </summary>
        protected abstract float Smoothing { get; }

        /// <summary>
        /// A fixed set of bands to sample from an <see cref="Equalizer"/> for export into a single file.
        /// </summary>
        protected LimitedEqualizerFilterSet(int sampleRate) : base(sampleRate) { }

        /// <summary>
        /// A fixed set of bands to sample from an <see cref="Equalizer"/> for export into a single file.
        /// </summary>
        protected LimitedEqualizerFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// A fixed set of bands to sample from an <see cref="Equalizer"/> for export into a single file.
        /// </summary>
        protected LimitedEqualizerFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Save the results of each channel to a single file.
        /// </summary>
        public override void Export(string path) {
            StringBuilder result = new StringBuilder("Set up the channels according to this configuration.").AppendLine();
            for (int i = 0; i < Channels.Length; i++) {
                RootFileChannelHeader(i, result);
                Equalizer curve = (Equalizer)((EqualizerChannelData)Channels[i]).curve.Clone();
                curve.Smooth(Smoothing);
                float[] freqs = Channels[i].reference != ReferenceChannel.ScreenLFE ? Frequencies : LFEFrequencies;
                for (int j = 0; j < freqs.Length; j++) {
                    string gain = QMath.ToStringLimitDecimals(curve[freqs[j]], 2);
                    result.AppendLine($"{RangeDependentDecimals(freqs[j])} Hz:\t{gain} dB");
                }
            }
            File.WriteAllText(path, result.ToString());
        }
    }
}
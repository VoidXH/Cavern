using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Cavern.Channels;
using Cavern.QuickEQ.Equalization;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Export the <see cref="Equalizer"/>s embedded into the Equalizer APO configuration file, using its internal FIR generator
    /// and allowing later modifications of the filter curve.
    /// </summary>
    public class EqualizerAPOEqualizerFilterSet : EqualizerFilterSet {
        /// <summary>
        /// Equalizer APO filters to add before the room correction, for example a crossover's Equalizer APO export.
        /// </summary>
        public List<string> prepend;

        /// <summary>
        /// Equalizer APO filters to add after the room correction.
        /// </summary>
        public List<string> append;

        /// <summary>
        /// Construct an Equalizer APO filter set with EQ curves for each channel for a room with the target number of channels.
        /// </summary>
        public EqualizerAPOEqualizerFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Construct an Equalizer APO filter set with EQ curves for each channel for a room with the target number of channels.
        /// </summary>
        public EqualizerAPOEqualizerFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Save the results to an Equalizer APO configuration file.
        /// </summary>
        public override void Export(string path) {
            List<string> result = new List<string>();
            if (prepend != null) {
                result.AddRange(prepend);
            }

            for (int channel = 0; channel < Channels.Length; channel++) {
                EqualizerChannelData channelRef = (EqualizerChannelData)Channels[channel];
                bool hasGain = channelRef.gain != 0,
                    hasDelay = channelRef.delaySamples != 0,
                    hasEQ = channelRef.curve != null;
                if (hasGain || hasDelay || hasEQ) {
                    result.Add("Channel: " + GetLabel(channel));
                }
                if (hasGain) {
                    result.Add($"Preamp: {channelRef.gain.ToString(CultureInfo.InvariantCulture)} dB");
                }
                if (hasDelay) {
                    result.Add($"Delay: {GetDelay(channel)} ms");
                }
                if (hasEQ) {
                    result.Add(channelRef.curve.ExportToEqualizerAPO());
                }
            }

            string polarity = EqualizerAPOUtils.GetPolarityLine(Channels.Select(x => ((EqualizerChannelData)x).switchPolarity).ToArray());
            if (polarity != null) {
                result.Add(polarity);
            }

            if (append != null) {
                result.AddRange(append);
            }
            File.WriteAllLines(path, result);
        }

        /// <summary>
        /// Get the short name of a channel written to the configuration file to select that channel for setup.
        /// </summary>
        protected override string GetLabel(int channel) => Channels.Length <= 8 ?
            EqualizerAPOUtils.GetChannelLabel(Channels[channel].reference) : base.GetLabel(channel);
    }
}
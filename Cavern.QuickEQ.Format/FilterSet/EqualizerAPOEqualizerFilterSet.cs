using System.Collections.Generic;
using System.Globalization;
using System.IO;

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
        /// Save the results to an Equalizer APO configuration file.
        /// </summary>
        public override void Export(string path) {
            List<string> result = new List<string>();
            if (prepend != null) {
                result.AddRange(prepend);
            }

            for (int channel = 0; channel < Channels.Length; channel++) {
                string chName = EqualizerAPOUtils.GetChannelLabel(channel, Channels.Length);
                result.Add("Channel: " + chName);
                if (Channels[channel].gain != 0) {
                    result.Add($"Preamp: {Channels[channel].gain.ToString(CultureInfo.InvariantCulture)} dB");
                }
                if (Channels[channel].delaySamples != 0) {
                    result.Add($"Delay: {GetDelay(channel)} ms");
                }
                result.Add(Channels[channel].curve.ExportToEqualizerAPO());
            }

            if (append != null) {
                result.AddRange(append);
            }
            File.WriteAllLines(path, result);
        }
    }
}
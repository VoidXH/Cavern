using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for Equalizer APO (PC).
    /// </summary>
    public class EqualizerAPOIIRFilterSet : IIRFilterSet {
        /// <summary>
        /// Maximum number of peaking EQ filters per channel.
        /// </summary>
        public override int Bands => 20;

        /// <summary>
        /// Minimum gain of a single peaking EQ band in decibels.
        /// </summary>
        public override double MinGain => -20;

        /// <summary>
        /// Maximum gain of a single peaking EQ band in decibels.
        /// </summary>
        public override double MaxGain => 20;

        /// <summary>
        /// Round the gains to this precision.
        /// </summary>
        public override double GainPrecision => .0001;

        /// <summary>
        /// An optional header to add to the beginning of the exported file.
        /// </summary>
        readonly IEnumerable<string> header;

        /// <summary>
        /// IIR filter set for Equalizer APO (PC) with no additional header.
        /// </summary>
        public EqualizerAPOIIRFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for Equalizer APO (PC) with an additional header.
        /// </summary>
        public EqualizerAPOIIRFilterSet(int channels, int sampleRate, IEnumerable<string> header) : base(channels, sampleRate) =>
            this.header = header;

        /// <summary>
        /// IIR filter set for Equalizer APO (PC) with no additional header.
        /// </summary>
        public EqualizerAPOIIRFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for Equalizer APO (PC) with an additional header.
        /// </summary>
        public EqualizerAPOIIRFilterSet(ReferenceChannel[] channels, int sampleRate, IEnumerable<string> header) :
            base(channels, sampleRate) => this.header = header;

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public override void Export(string path) {
            List<string> configFile = header != null ? new List<string>(header) : new List<string>();
            for (int i = 0, c = Channels.Length; i < c; i++) {
                IIRChannelData channelRef = (IIRChannelData)Channels[i];
                configFile.Add("Channel: " + GetLabel(i));
                configFile.Add($"Preamp: {channelRef.gain} dB");
                if (channelRef.delaySamples != 0) {
                    configFile.Add($"Delay: {GetDelay(i)} ms");
                }
                BiquadFilter[] filters = channelRef.filters;
                for (int j = 0; j < filters.Length; j++) {
                    configFile.Add(
                        $"Filter: ON PK Fc {filters[j].CenterFreq:0.00} Hz Gain {filters[j].Gain:0.00} dB Q {filters[j].Q:0.0000}");
                }
            }
            string polarity = EqualizerAPOUtils.GetPolarityLine(Channels.Select(x => ((IIRChannelData)x).switchPolarity).ToArray());
            if (polarity != null) {
                configFile.Add(polarity);
            }
            File.WriteAllLines(path, configFile);
        }

        /// <summary>
        /// Get the short name of a channel written to the configuration file to select that channel for setup.
        /// </summary>
        protected override string GetLabel(int channel) => channel < 8 ?
            EqualizerAPOUtils.GetChannelLabel(Channels[channel].reference) : base.GetLabel(channel);
    }
}
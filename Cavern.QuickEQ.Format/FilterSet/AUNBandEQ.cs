using System.Collections.Generic;
using System.IO;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// IIR filter set for AU N-Band EQ software.
    /// </summary>
    public class AUNBandEQ : IIRFilterSet {
        /// <summary>
        /// Maximum number of peaking EQ filters per channel.
        /// </summary>
        public override int Bands => 16;

        /// <summary>
        /// Minimum gain of a single peaking EQ band in decibels.
        /// </summary>
        public override double MinGain => -96;

        /// <summary>
        /// Maximum gain of a single peaking EQ band in decibels.
        /// </summary>
        public override double MaxGain => 24;

        /// <summary>
        /// Round the gains to this precision.
        /// </summary>
        public override double GainPrecision => .1;

        /// <summary>
        /// IIR filter set for AU N-Band EQ software.
        /// </summary>
        public AUNBandEQ(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// IIR filter set for AU N-Band EQ software.
        /// </summary>
        public AUNBandEQ(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Export the filter set to a target file. This is the standard IIR filter set format.
        /// </summary>
        public override void Export(string path) {
            List<string> result = new List<string> {
                $"Set up the {Bands} bands for each channel from this file."
            };
            for (int i = 0; i < Channels.Length; i++) {
                IIRChannelData channelRef = (IIRChannelData)Channels[i];
                result.Add(string.Empty);
                result.Add(channelRef.name);
                result.Add(new string('=', channelRef.name.Length));
                RootFileExtension(i, result);
                if (channelRef.delaySamples != 0) {
                    result.Add("Delay: " + GetDelay(i).ToString("0.00 ms"));
                }
                BiquadFilter[] bands = channelRef.filters;
                for (int j = 0; j < bands.Length; j++) {
                    result.Add($"{bands[j].CenterFreq:0} Hz:\t{bands[j].Gain:0.00} dB, bandwidth: {QFactor.ToBandwidth(bands[j].Q):0.00}");
                }
            }
            File.WriteAllLines(path, result);
        }
    }
}
using System.Collections.Generic;
using System.IO;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Filter set to fine tune an existing YPAO calibration with.
    /// </summary>
    public class YPAOFilterSet : IIRFilterSet {
        /// <summary>
        /// Maximum number of peaking EQ filters per channel.
        /// </summary>
        public override int Bands => 7;

        /// <summary>
        /// Maximum gain of a single peaking EQ band in decibels.
        /// </summary>
        public override double MaxGain => 6;

        /// <summary>
        /// Round the gains to this precision.
        /// </summary>
        public override double GainPrecision => .5f;

        /// <summary>
        /// Filter set to fine tune an existing YPAO calibration with.
        /// </summary>
        public YPAOFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Filter set to fine tune an existing YPAO calibration with.
        /// </summary>
        public YPAOFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Export the filter set to a target file. This is the standard IIR filter set format
        /// </summary>
        public override void Export(string path) {
            List<string> result = new List<string> { "Use the following manual filters for each channel." };
            for (int i = 0; i < Channels.Length; i++) {
                result.Add(string.Empty);
                result.Add(GetLabel(i) + ':');
                BiquadFilter[] filters = ((IIRChannelData)Channels[i]).filters;
                for (int j = 0; j < filters.Length;) {
                    BiquadFilter filter = filters[j];
                    result.Add($"Filter {++j} - Frequency: {bands.Nearest((float)filter.CenterFreq)}, Q factor: " +
                        $"{qFactors.Nearest((float)filter.Q)}, gain: {filter.Gain}");
                }
            }
            File.WriteAllLines(path, result);
        }

        /// <summary>
        /// All the possible bands that can be selected for YPAO. These are 1/3 octaves apart.
        /// </summary>
        static readonly float[] bands = {
            39.4f, 49.6f, 62.5f, 78.7f, 99.2f, 125.0f, 157.5f, 198.4f, 250, 315, 396.9f, 500, 630, 793.7f,
            1000, 1260, 1590, 2000, 2520, 3170, 4000, 5040, 6350, 8000, 10100, 12700, 16000
        };

        /// <summary>
        /// All the possible Q-factors that can be selected for YPAO.
        /// </summary>
        static readonly float[] qFactors = { 0.5f, 0.630f, 0.794f, 1f, 1.260f, 1.587f, 2f, 2.520f, 3.175f, 4f, 5.040f, 6.350f, 8f, 10.08f };
    }
}
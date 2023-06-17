using System.Collections.Generic;
using System.IO;

using Cavern.Filters;
using Cavern.QuickEQ.Equalization;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Exports a traditional multiband eqalizer with constant bandwidth bands.
    /// </summary>
    public class MultibandPEQFilterSet : IIRFilterSet {
        /// <summary>
        /// Frequency of the first exported band.
        /// </summary>
        readonly double firstBand;

        /// <summary>
        /// Number of bands for each octave.
        /// </summary>
        readonly int bandsPerOctave;

        /// <summary>
        /// Number of total bands.
        /// </summary>
        readonly int bandCount;

        /// <summary>
        /// Construct a traditional multiband eqalizer with constant bandwidth bands.
        /// </summary>
        /// <param name="channels">Number of channels in the target system</param>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="firstBand">Frequency of the first exported band</param>
        /// <param name="bandsPerOctave">Number of bands for each octave</param>
        /// <param name="bandCount">Number of total bands</param>
        public MultibandPEQFilterSet(int channels, int sampleRate, double firstBand, int bandsPerOctave, int bandCount) :
            base(channels, sampleRate) {
            this.firstBand = firstBand;
            this.bandsPerOctave = bandsPerOctave;
            this.bandCount = bandCount;
        }

        /// <summary>
        /// Create the filters that should be used when setting up a channel.
        /// </summary>
        public PeakingEQ[] CalculateFilters(Equalizer targetToReach) {
            PeakingEQ[] result = new PeakingEqualizer(targetToReach) {
                MinGain = -12,
                MaxGain = 6
            }.GetPeakingEQ(SampleRate, firstBand, bandsPerOctave, bandCount);

            IReadOnlyList<Band> bands = targetToReach.Bands;
            double maxFreq = bands.Count != 0 ? bands[^1].Frequency : 0;
            for (int i = 1; i < result.Length; i++) {
                if (result[i].CenterFreq > maxFreq) {
                    result[i].Gain = result[i - 1].Gain;
                }
            }

            return result;
        }

        /// <summary>
        /// Export the filter set to a target file. Since these settings have to be manually entered, no separation is needed.
        /// </summary>
        public override void Export(string path) {
            List<string> result = new List<string> {
                $"Set up the {bandCount} bands for each channel from this file."
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
                    result.Add($"{bands[j].CenterFreq:0} Hz:\t{bands[j].Gain:0.00} dB");
                }
            }
            File.WriteAllLines(path, result);
        }
    }
}
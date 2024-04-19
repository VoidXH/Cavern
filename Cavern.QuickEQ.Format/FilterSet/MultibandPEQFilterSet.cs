using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.QuickEQ.Equalization;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Exports a traditional multiband eqalizer with constant bandwidth bands.
    /// </summary>
    public class MultibandPEQFilterSet : IIRFilterSet {
        /// <inheritdoc/>
        public override double MinGain => -12;

        /// <inheritdoc/>
        public override double MaxGain => 6;

        /// <inheritdoc/>
        public override double GainPrecision => .01;

        /// <summary>
        /// Round filter center frequencies to the nearest whole number.
        /// </summary>
        public virtual bool RoundedBands { get; }

        /// <summary>
        /// Some devices don't have the EQ bands spaced properly, or the generated bands have rounding errors.
        /// Using the incorrect, but actually present frequencies is possible here by adding a set of frequency pairs
        /// with the old being the generated/exported frequency, and the new being the frequency used by the device.
        /// </summary>
        public (double oldFreq, double newFreq)[] FreqOverrides { get; set; }

        /// <summary>
        /// Limit the number of bands exported for the LFE channel.
        /// </summary>
        protected int LFEBands { get; set; }

        /// <summary>
        /// Frequency of the first exported band.
        /// </summary>
        readonly double firstBand;

        /// <summary>
        /// Number of bands for each octave.
        /// </summary>
        readonly double bandsPerOctave;

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
        public MultibandPEQFilterSet(int channels, int sampleRate, double firstBand, double bandsPerOctave, int bandCount) :
            base(channels, sampleRate) {
            this.firstBand = firstBand;
            this.bandsPerOctave = bandsPerOctave;
            this.bandCount = bandCount;
            LFEBands = bandCount;
        }

        /// <summary>
        /// Construct a traditional multiband eqalizer with constant bandwidth bands.
        /// </summary>
        /// <param name="channels">Number of channels in the target system</param>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="firstBand">Frequency of the first exported band</param>
        /// <param name="bandsPerOctave">Number of bands for each octave</param>
        /// <param name="bandCount">Number of total bands</param>
        /// <param name="roundedBands">Round filter center frequencies to the nearest whole number</param>
        public MultibandPEQFilterSet(int channels, int sampleRate, double firstBand, double bandsPerOctave, int bandCount,
            bool roundedBands) :
            this(channels, sampleRate, firstBand, bandsPerOctave, bandCount) {
            RoundedBands = roundedBands;
        }

        /// <summary>
        /// Construct a traditional multiband eqalizer with constant bandwidth bands.
        /// </summary>
        /// <param name="channels">Channels in the target system</param>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="firstBand">Frequency of the first exported band</param>
        /// <param name="bandsPerOctave">Number of bands for each octave</param>
        /// <param name="bandCount">Number of total bands</param>
        public MultibandPEQFilterSet(ReferenceChannel[] channels, int sampleRate, double firstBand, double bandsPerOctave, int bandCount) :
            base(channels, sampleRate) {
            this.firstBand = firstBand;
            this.bandsPerOctave = bandsPerOctave;
            this.bandCount = bandCount;
            LFEBands = bandCount;
        }

        /// <summary>
        /// Construct a traditional multiband eqalizer with constant bandwidth bands.
        /// </summary>
        /// <param name="channels">Channels in the target system</param>
        /// <param name="sampleRate">Filter sample rate</param>
        /// <param name="firstBand">Frequency of the first exported band</param>
        /// <param name="bandsPerOctave">Number of bands for each octave</param>
        /// <param name="bandCount">Number of total bands</param>
        /// <param name="roundedBands">Round filter center frequencies to the nearest whole number</param>
        public MultibandPEQFilterSet(ReferenceChannel[] channels, int sampleRate, double firstBand, double bandsPerOctave, int bandCount,
            bool roundedBands) :
            this(channels, sampleRate, firstBand, bandsPerOctave, bandCount) {
            RoundedBands = roundedBands;
        }

        /// <summary>
        /// Parse a multiband PEQ filter set from a file which was exported with <see cref="Export(string)"/> from this class.
        /// </summary>
        /// <param name="path">Full path of the input file</param>
        /// <param name="sampleRate">Sample rate to create the <see cref="PeakingEQ"/> instances at</param>
        public MultibandPEQFilterSet(string path, int sampleRate) : base(0, sampleRate) {
            string lastLine = string.Empty;
            string lastChannelName = null;
            List<PeakingEQ> lastChannel = null;
            List<IIRChannelData> channels = new List<IIRChannelData>();
            string[] linesIn = File.ReadAllLines(path);
            // Adds an empty last line so the channel adding code doesn't have to be used at two places
            IEnumerable<string> lines = linesIn.Append(string.Empty);
            foreach (string line in lines) {
                if (line.Length > 0 && line[0] == '=') {
                    lastChannelName = lastLine;
                    lastChannel = new List<PeakingEQ>();
                } else {
                    int split = line.IndexOf('\t');
                    if (split == -1) {
                        if (lastChannelName == null) {
                            lastLine = line;
                            continue;
                        }

                        for (int i = 0, c = lastChannel.Count - 1; i < c; i++) {
                            lastChannel[i].Q = QFactor.FromBandwidth(Math.Log(lastChannel[i + 1].CenterFreq / lastChannel[i].CenterFreq, 2));
                        }
                        lastChannel[^1].Q = lastChannel[^2].Q;
                        channels.Add(new IIRChannelData() {
                            name = lastChannelName,
                            filters = lastChannel.ToArray(),
                            reference = ReferenceChannel.Unknown
                        });

                        lastChannelName = null;
                        continue;
                    }

                    double freq = double.Parse(line[..line.IndexOf(' ')]);
                    double gain = double.Parse(line[(split + 1)..line.LastIndexOf(' ')]);
                    lastChannel.Add(new PeakingEQ(sampleRate, freq, QFactor.reference, gain));
                }

                lastLine = line;
            }

            Channels = channels.ToArray();
        }

        /// <summary>
        /// Create the filters that should be used when setting up a channel.
        /// </summary>
        public PeakingEQ[] CalculateFilters(Equalizer targetToReach) {
            PeakingEQ[] result = new PeakingEqualizer(targetToReach) {
                MinGain = MinGain,
                MaxGain = MaxGain,
                GainPrecision = GainPrecision,
                FreqOverrides = FreqOverrides
            }.GetPeakingEQ(SampleRate, firstBand, bandsPerOctave, bandCount, RoundedBands);

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
                string chName = GetLabel(i);
                result.Add(chName);
                result.Add(new string('=', chName.Length));
                RootFileExtension(i, result);
                if (channelRef.delaySamples != 0) {
                    result.Add("Delay: " + GetDelay(i).ToString("0.00 ms"));
                }
                BiquadFilter[] bands = channelRef.filters;
                int bandc = channelRef.reference != ReferenceChannel.ScreenLFE ? bands.Length : LFEBands;
                for (int j = 0; j < bandc; j++) {
                    result.Add($"{bands[j].CenterFreq:0} Hz:\t{bands[j].Gain:0.00} dB");
                }
            }
            File.WriteAllLines(path, result);
        }
    }
}
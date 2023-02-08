using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Room correction filter data with infinite impulse response (biquad) filter sets for each channel.
    /// </summary>
    public class IIRFilterSet : FilterSet {
        /// <summary>
        /// All information needed for a channel.
        /// </summary>
        protected struct ChannelData {
            /// <summary>
            /// Applied filter set for the channel.
            /// </summary>
            public BiquadFilter[] filters;

            /// <summary>
            /// Gain offset for the channel.
            /// </summary>
            public double gain;

            /// <summary>
            /// Delay of this channel in samples.
            /// </summary>
            public int delaySamples;

            /// <summary>
            /// Swap the sign for this channel.
            /// </summary>
            public bool switchPolarity;

            /// <summary>
            /// The reference channel describing this channel or <see cref="ReferenceChannel.Unknown"/> if not applicable.
            /// </summary>
            public ReferenceChannel reference;

            /// <summary>
            /// Custom label for this channel or null if not applicable.
            /// </summary>
            public string name;
        }

        /// <summary>
        /// Maximum number of peaking EQ filters per channel.
        /// </summary>
        public virtual int Bands => 20;

        /// <summary>
        /// Minimum gain of a single peaking EQ band in decibels.
        /// </summary>
        public virtual double MinGain => -20;

        /// <summary>
        /// Maximum gain of a single peaking EQ band in decibels.
        /// </summary>
        public virtual double MaxGain => 20;

        /// <summary>
        /// Round the gains to this precision.
        /// </summary>
        public virtual double GainPrecision => .0001;

        /// <summary>
        /// Applied filter sets for each channel in the configuration file.
        /// </summary>
        protected ChannelData[] Channels { get; private set; }

        /// <summary>
        /// Construct a room correction with IIR filter sets for each channel for a room with the target number of channels.
        /// </summary>
        public IIRFilterSet(int channels, int sampleRate) : base(sampleRate) {
            Channels = new ChannelData[channels];
            ReferenceChannel[] matrix = ChannelPrototype.GetStandardMatrix(channels);
            for (int i = 0; i < matrix.Length; i++) {
                Channels[i].reference = matrix[i];
            }
        }

        /// <summary>
        /// Construct a room correction with IIR filter sets for each channel for a room with the target reference channels.
        /// </summary>
        public IIRFilterSet(ReferenceChannel[] channels, int sampleRate) : base(sampleRate) {
            Channels = new ChannelData[channels.Length];
            for (int i = 0; i < channels.Length; i++) {
                Channels[i].reference = channels[i];
            }
        }

        /// <summary>
        /// Setup a channel's filter set and related metadata.
        /// </summary>
        public void SetupChannel(int channel, BiquadFilter[] filters, double gain = 0, int delaySamples = 0,
            bool switchPolarity = false, string name = null) {
            Channels[channel].filters = filters;
            Channels[channel].gain = gain;
            Channels[channel].delaySamples = delaySamples;
            Channels[channel].switchPolarity = switchPolarity;
            Channels[channel].name = name;
        }

        /// <summary>
        /// Setup a channel's filter set and related metadata.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, BiquadFilter[] filters,
            double gain = 0, int delaySamples = 0, bool switchPolarity = false, string name = null) {
            for (int i = 0; i < Channels.Length; i++) {
                if (Channels[i].reference == channel) {
                    Channels[i].filters = filters;
                    Channels[i].gain = gain;
                    Channels[i].delaySamples = delaySamples;
                    Channels[i].switchPolarity = switchPolarity;
                    Channels[i].name = name;
                    return;
                }
            }
        }

        /// <summary>
        /// Export the filter set to a target file. This is the standard IIR filter set format
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];
            CreateRootFile(path, "txt");

            for (int i = 0, c = Channels.Length; i < c; i++) {
                List<string> channelData = new List<string>();
                BiquadFilter[] filters = Channels[i].filters;
                for (int j = 0; j < filters.Length; j++) {
                    string freq;
                    if (filters[j].CenterFreq < 100) {
                        freq = filters[j].CenterFreq.ToString("0.00");
                    } else if (filters[j].CenterFreq < 1000) {
                        freq = filters[j].CenterFreq.ToString("0.0");
                    } else {
                        freq = filters[j].CenterFreq.ToString("0");
                    }
                    channelData.Add(string.Format("Filter {0,2}: ON  PK       Fc {1,7} Hz  Gain {2,6} dB  Q {3,6}",
                        j + 1, freq, filters[j].Gain.ToString("0.00", CultureInfo.InvariantCulture),
                        Math.Max(Math.Round(filters[j].Q * 4) / 4, .25).ToString("0.00", CultureInfo.InvariantCulture)));
                }
                for (int j = filters.Length; j < Bands;) {
                    channelData.Add($"Filter {++j}: OFF None");
                }
                File.WriteAllLines(Path.Combine(folder, $"{fileNameBase} {GetLabel(i)}.txt"), channelData);
            }
        }

        /// <summary>
        /// Get the short name of a channel written to the configuration file to select that channel for setup.
        /// </summary>
        protected override string GetLabel(int channel) => Channels[channel].name ?? base.GetLabel(channel);

        /// <summary>
        /// Create the file with gain/delay/polarity info as the root document that's saved in the save dialog.
        /// </summary>
        protected void CreateRootFile(string path, string filterFileExtension) {
            string fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];
            List<string> result = new List<string>();
            bool hasAnything = false,
                hasDelays = false;
            for (int i = 0, c = Channels.Length; i < c; i++) {
                result.Add(string.Empty);
                result.Add("Channel: " + GetLabel(i));
                if (Channels[i].delaySamples != 0) {
                    result.Add("Delay: " + GetDelay(i).ToString("0.0 ms"));
                    hasAnything = true;
                    hasDelays = true;
                }
                if (Channels[i].gain != 0) {
                    result.Add("Level: " + Channels[i].gain.ToString("0.0 dB"));
                    hasAnything = true;
                }
                if (Channels[i].switchPolarity) {
                    result.Add("Switch polarity");
                    hasAnything = true;
                }
            }
            if (hasAnything) {
                File.WriteAllLines(path, result.Prepend(hasDelays ?
                    $"Set up levels and delays by this file. Load \"{fileNameBase} <channel>.{filterFileExtension}\" files as EQ." :
                    $"Set up levels by this file. Load \"{fileNameBase} <channel>.{filterFileExtension}\" files as EQ."));
            }
        }

        /// <summary>
        /// Get the delay for a given channel in milliseconds instead of samples.
        /// </summary>
        protected double GetDelay(int channel) => Channels[channel].delaySamples * 1000.0 / SampleRate;
    }
}
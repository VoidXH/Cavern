﻿using System.IO;

using Cavern.Channels;
using Cavern.QuickEQ.Equalization;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Room correction filter data with an EQ curve for each channel. This base class exports standard EQ curves compatible with most
    /// applications using frequency/gain pairs in TXT files.
    /// </summary>
    public class EqualizerFilterSet : FilterSet {
        /// <summary>
        /// Additional text to be added to the first line of each output as comment.
        /// </summary>
        public string optionalHeader;

        /// <summary>
        /// Required data for each exported channel.
        /// </summary>
        protected struct ChannelData {
            /// <summary>
            /// Applied equalization filter for the channel, using which is resulting in the expected target response.
            /// </summary>
            public Equalizer curve;

            /// <summary>
            /// Gain offset for the channel.
            /// </summary>
            public double gain;

            /// <summary>
            /// Delay of this channel in samples.
            /// </summary>
            public int delaySamples;

            /// <summary>
            /// Custom label for this channel or null if not applicable.
            /// </summary>
            public string name;

            /// <summary>
            /// The reference channel describing this channel or <see cref="ReferenceChannel.Unknown"/> if not applicable.
            /// </summary>
            public ReferenceChannel reference;
        };

        /// <summary>
        /// Applied equalization parameters for each channel in the configuration file.
        /// </summary>
        protected ChannelData[] Channels { get; private set; }

        /// <summary>
        /// Construct a room correction with EQ curves for each channel for a room with the target number of channels.
        /// </summary>
        public EqualizerFilterSet(int channels, int sampleRate) : base(sampleRate) {
            Channels = new ChannelData[channels];
            ReferenceChannel[] matrix = ChannelPrototype.GetStandardMatrix(channels);
            for (int i = 0; i < matrix.Length; i++) {
                Channels[i].reference = matrix[i];
            }
        }

        /// <summary>
        /// Construct a room correction with EQ curves for each channel for a room with the target reference channels.
        /// </summary>
        public EqualizerFilterSet(ReferenceChannel[] channels, int sampleRate) : base(sampleRate) {
            Channels = new ChannelData[channels.Length];
            for (int i = 0; i < channels.Length; i++) {
                Channels[i].reference = channels[i];
            }
        }

        /// <summary>
        /// Setup a channel's curve and related metadata.
        /// </summary>
        public void SetupChannel(int channel, Equalizer curve, double gain = 0, int delaySamples = 0, string name = null) {
            Channels[channel].curve = curve;
            Channels[channel].gain = gain;
            Channels[channel].delaySamples = delaySamples;
            Channels[channel].name = name;
        }

        /// <summary>
        /// Setup a channel's curve and related metadata.
        /// </summary>
        public void SetupChannel(ReferenceChannel channel, Equalizer curve, double gain = 0, int delaySamples = 0, string name = null) {
            for (int i = 0; i < Channels.Length; ++i) {
                if (Channels[i].reference == channel) {
                    Channels[i].curve = curve;
                    Channels[i].gain = gain;
                    Channels[i].delaySamples = delaySamples;
                    Channels[i].name = name;
                    return;
                }
            }
        }

        /// <summary>
        /// Save the results to EQ curve files for each channel.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];
            for (int i = 0; i < Channels.Length; i++) {
                Channels[i].curve.Export(Path.Combine(folder, $"{fileNameBase} {Channels[i].name}.txt"),
                    Channels[i].gain, optionalHeader);
            }
        }
    }
}
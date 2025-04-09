using System.Collections.Generic;
using System.IO;

using Cavern.Channels;
using Cavern.QuickEQ.Equalization;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Generates FIR filters in multiple sample rates for Roon devices.
    /// </summary>
    public class RoonFilterSet : EqualizerFilterSet {
        /// <summary>
        /// Generates FIR filters in multiple sample rates for Roon devices.
        /// </summary>
        public RoonFilterSet(int channels) : base(channels, Listener.DefaultSampleRate) { }

        /// <summary>
        /// Generates FIR filters in multiple sample rates for Roon devices.
        /// </summary>
        public RoonFilterSet(ReferenceChannel[] channels) : base(channels, Listener.DefaultSampleRate) { }

        /// <inheritdoc/>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileNameWithoutExtension(path);
            for (int i = 0; i < Channels.Length; i++) {
                EqualizerChannelData channel = (EqualizerChannelData)Channels[i];
                for (int j = 0; j < exports.Length; j++) {
                    int sampleRate = exports[j].sampleRate;
                    string fileName = Path.Combine(folder, $"{fileNameBase} Filter {GetChannelName(i)} Speaker {sampleRate}.wav");
                    RIFFWaveWriter.Write(fileName, channel.curve.GetConvolution(sampleRate, exports[j].length), 1, sampleRate, BitDepth.Float32);
                }
            }
        }

        /// <summary>
        /// Converts an entry of <see cref="FilterSet.Channels"/> at a given <paramref name="index"/> to Roon's <see cref="channelNames"/>.
        /// </summary>
        string GetChannelName(int index) {
            ChannelData channel = Channels[index];
            if (channelNames.TryGetValue(channel.reference, out string result)) {
                return result;
            }
            return channel.name ?? channel.reference.ToString();
        }

        /// <summary>
        /// How Roon calls the <see cref="ReferenceChannel"/>s.
        /// </summary>
        static readonly Dictionary<ReferenceChannel, string> channelNames = new Dictionary<ReferenceChannel, string> {
            [ReferenceChannel.FrontLeft] = "Left",
            [ReferenceChannel.FrontRight] = "Right",
        };

        /// <summary>
        /// Sample rate and sample count pairs that are required by Roon.
        /// </summary>
        static readonly (int sampleRate, int length)[] exports = {
            (44100, 32768),
            (48000, 32768),
            (88200, 65536),
            (96000, 65536),
            (176400, 131072),
            (192000, 131072),
            (352800, 262144),
            (384000, 262144),
        };
    }
}
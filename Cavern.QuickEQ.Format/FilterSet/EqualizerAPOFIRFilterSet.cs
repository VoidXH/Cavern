using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

using Cavern.Channels;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Convolution filter set for Equalizer APO.
    /// </summary>
    public class EqualizerAPOFIRFilterSet : FIRFilterSet {
        /// <inheritdoc/>
        public override string FileExtension => "txt";

        /// <summary>
        /// Store this many channels' convolutions in each exported file.
        /// </summary>
        public int ChannelsPerConvolution { get; set; } = 1;

        /// <summary>
        /// An optional header to add to the beginning of the exported file.
        /// </summary>
        readonly IEnumerable<string> header;

        /// <summary>
        /// Convolution filter set for Equalizer APO with a given number of channels with no additional header.
        /// </summary>
        public EqualizerAPOFIRFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Convolution filter set for Equalizer APO with a given number of channels with an additional header.
        /// </summary>
        public EqualizerAPOFIRFilterSet(int channels, int sampleRate, IEnumerable<string> header) : base(channels, sampleRate) =>
            this.header = header;

        /// <summary>
        /// Convolution filter set for Equalizer APO with a given number of channels with no additional header.
        /// </summary>
        public EqualizerAPOFIRFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Convolution filter set for Equalizer APO with a given number of channels with an additional header.
        /// </summary>
        public EqualizerAPOFIRFilterSet(ReferenceChannel[] channels, int sampleRate, IEnumerable<string> header) :
            base(channels, sampleRate) => this.header = header;

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.Combine(folder, Path.GetFileNameWithoutExtension(path));

            List<string> configFile = header != null ? new List<string>(header) : new List<string>();
            for (int i = 0; i < Channels.Length; i += ChannelsPerConvolution) {
                Export(i, fileNameBase, configFile);
            }

            File.WriteAllLines(path, configFile);
        }

        /// <summary>
        /// Get the short name of a channel written to the configuration file to select that channel for setup.
        /// </summary>
        protected override string GetLabel(int channel) => channel < 8 ?
            EqualizerAPOUtils.GetChannelLabel(Channels[channel].reference) : base.GetLabel(channel);

        /// <summary>
        /// When a channel's delay is less than half the filter length, it should be applied to the convolution
        /// as the filter is unlikely to be pushed out to the right.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool IsDelayEmbedded(FIRChannelData channelRef) => channelRef.delaySamples < (channelRef.filter.Length >> 1);

        /// <summary>
        /// Export the filters for each channel from the <paramref name="firstChannel"/> that is set, delayed if needed (<see cref="IsDelayEmbedded"/>).
        /// </summary>
        void Export(int firstChannel, string fileNameBase, List<string> configFile) {
            int inThisFile = Math.Min(ChannelsPerConvolution, Channels.Length - firstChannel);

            StringBuilder fileName = new StringBuilder(fileNameBase);
            string[] labels = new string[inThisFile];
            float[][] filters = new float[inThisFile][];
            for (int i = 0; i < inThisFile; i++) {
                FIRChannelData channelRef = (FIRChannelData)Channels[firstChannel + i];
                labels[i] = GetLabel(firstChannel + i);
                fileName.Append(' ' + (channelRef.name ?? labels[i]));
                filters[i] = channelRef.filter.FastClone();
                if (channelRef.delaySamples != 0) {
                    if (IsDelayEmbedded(channelRef)) {
                        WaveformUtils.Delay(filters[i], channelRef.delaySamples);
                    } else {
                        configFile.Add("Channel: " + labels[i]);
                        configFile.Add($"Delay: {GetDelay(firstChannel + i)} ms");
                    }
                }
            }
            fileName.Append(".wav");
            string finalName = fileName.ToString();
            RIFFWaveWriter.Write(finalName, filters, SampleRate, BitDepth.Float32);
            configFile.Add("Channel: " + string.Join(' ', labels));
            configFile.Add("Convolution: " + Path.GetFileName(finalName));
        }
    }
}

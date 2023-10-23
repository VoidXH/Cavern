using System.Collections.Generic;
using System.IO;

using Cavern.Channels;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Convolution filter set for Equalizer APO.
    /// </summary>
    public class EqualizerAPOFIRFilterSet : FIRFilterSet {
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
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];

            List<string> configFile = header != null ? new List<string>(header) : new List<string>();
            for (int i = 0; i < Channels.Length; i++) {
                FIRChannelData channelRef = (FIRChannelData)Channels[i];
                if (channelRef.filter == null) {
                    continue;
                }

                string targetLabel = GetLabel(i),
                    filterRelative = $"{fileNameBase} {channelRef.name ?? targetLabel}.wav",
                    filterPath = Path.Combine(folder, filterRelative);
                configFile.Add("Channel: " + targetLabel);

                if (channelRef.delaySamples != 0) {
                    // Only delay in the actual convolution if it's less than half the filter
                    if ((channelRef.filter.Length >> 1) > channelRef.delaySamples) {
                        WaveformUtils.Delay(channelRef.filter, channelRef.delaySamples);
                    } else {
                        configFile.Add($"Delay: {channelRef.delaySamples * 1000.0 / SampleRate} ms");
                    }
                }

                RIFFWaveWriter.Write(Path.Combine(folder, filterPath), channelRef.filter, 1, SampleRate, BitDepth.Float32);
                configFile.Add("Convolution: " + filterRelative);
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
using System.Collections.Generic;
using System.IO;

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
        /// Convolution filter set for Equalizer APO with a given number of channels.
        /// </summary>
        public EqualizerAPOFIRFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Convolution filter set for Equalizer APO with a given number of channels with an optional header.
        /// </summary>
        public EqualizerAPOFIRFilterSet(int channels, int sampleRate, IEnumerable<string> header) : base(channels, sampleRate) =>
            this.header = header;

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];

            List<string> configFile = header != null ? new List<string>(header) : new List<string>();
            for (int i = 0; i < Channels.Length; i++) {
                string targetLabel = EqualizerAPOUtils.GetChannelLabel(i, Channels.Length),
                    filterRelative = $"{fileNameBase} {Channels[i].name ?? targetLabel}.wav",
                    filterPath = Path.Combine(folder, filterRelative);
                configFile.Add("Channel: " + targetLabel);

                if (Channels[i].delaySamples != 0) {
                    // Only delay in the actual convolution if it's less than half the filter
                    if ((Channels[i].filter.Length >> 1) > Channels[i].delaySamples) {
                        WaveformUtils.Delay(Channels[i].filter, Channels[i].delaySamples);
                    } else {
                        configFile.Add($"Delay: {Channels[i].delaySamples * 1000.0 / SampleRate} ms");
                    }
                }

                RIFFWaveWriter.Write(Path.Combine(folder, filterPath), Channels[i].filter, 1, SampleRate, BitDepth.Float32);
                configFile.Add("Convolution: " + filterRelative);
            }

            File.WriteAllLines(path, configFile);
        }
    }
}
using System.Collections.Generic;
using System.IO;

using Cavern.Channels;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Convolution filter set for CamillaDSP.
    /// </summary>
    public class CamillaDSPFilterSet : FIRFilterSet {
        /// <inheritdoc/>
        public override string FileExtension => "yml";

        /// <summary>
        /// Convolution filter set for CamillaDSP.
        /// </summary>
        public CamillaDSPFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Convolution filter set for CamillaDSP.
        /// </summary>
        public CamillaDSPFilterSet(ReferenceChannel[] channels, int sampleRate) : base(channels, sampleRate) { }

        /// <inheritdoc/>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileNameWithoutExtension(path);

            List<string> configFile = new List<string> { "filters:" };
            bool[] additionalDelays = new bool[Channels.Length];
            for (int i = 0; i < Channels.Length; i++) {
                FIRChannelData channelRef = (FIRChannelData)Channels[i];
                string label = channelRef.name ?? EqualizerAPOUtils.GetChannelLabel(i, Channels.Length),
                    filterRelative = $"{fileNameBase} {label}.wav",
                    filterPath = Path.Combine(folder, filterRelative);
                configFile.AddRange(new[] {
                    $"  channel_{label}:",
                    "    type: Conv",
                    "    parameters:",
                    "      type: Wav",
                    "      filename: " + filterRelative
                });

                if (channelRef.delaySamples != 0) {
                    // Only delay in the actual convolution if it's less than half the filter
                    if ((channelRef.filter.Length >> 1) > channelRef.delaySamples) {
                        WaveformUtils.Delay(channelRef.filter, channelRef.delaySamples);
                    } else {
                        configFile.AddRange(new[] {
                            $"  channel_{label}_delay:",
                            "    type: Delay",
                            "    parameters:",
                            "      delay: " + channelRef.delaySamples,
                            "      unit: samples",
                            "      subsample: false"
                        });
                        additionalDelays[i] = true;
                    }
                }

                RIFFWaveWriter.Write(filterPath, channelRef.filter, 1, SampleRate, BitDepth.Float32);
            }

            configFile.Add("pipeline:");
            for (int i = 0; i < Channels.Length; i++) {
                string label = Channels[i].name ?? EqualizerAPOUtils.GetChannelLabel(i, Channels.Length);
                configFile.AddRange(new[] {
                    "  - type: Filter",
                    "    channel: " + i,
                    "    names:",
                    "      - channel_" + label
                });
                if (additionalDelays[i]) {
                    configFile.Add($"      - channel_{label}_delay");
                }
            }

            File.WriteAllLines(path, configFile);
        }
    }
}
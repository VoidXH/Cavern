using System.Collections.Generic;
using System.IO;

using Cavern.Channels;
using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    /// <summary>
    /// Convolution filter set for CamillaDSP.
    /// </summary>
    public class CamillaDSPFilterSet : FIRFilterSet {
        /// <summary>
        /// Extension of the single-file export. This should be displayed on export dialogs.
        /// </summary>
        public override string FileExtension => "yml";

        /// <summary>
        /// Convolution filter set for CamillaDSP.
        /// </summary>
        public CamillaDSPFilterSet(int channels, int sampleRate) : base(channels, sampleRate) { }

        /// <summary>
        /// Export the filter set to a target file.
        /// </summary>
        public override void Export(string path) {
            string folder = Path.GetDirectoryName(path),
                fileNameBase = Path.GetFileName(path);
            fileNameBase = fileNameBase[..fileNameBase.LastIndexOf('.')];

            List<string> configFile = new List<string> { "filters:" };
            bool[] additionalDelays = new bool[Channels.Length];
            for (int i = 0; i < Channels.Length; i++) {
                string label = Channels[i].name ?? EqualizerAPOUtils.GetChannelLabel(i, Channels.Length),
                    filterRelative = $"{fileNameBase} {label}.wav",
                    filterPath = Path.Combine(folder, filterRelative);
                configFile.AddRange(new string[] {
                    $"  channel_{label}:",
                    "    type: Conv",
                    "    parameters:",
                    "      type: Wav",
                    "      filename: " + filterRelative
                });

                if (Channels[i].delaySamples != 0) {
                    // Only delay in the actual convolution if it's less than half the filter
                    if ((Channels[i].filter.Length >> 1) > Channels[i].delaySamples) {
                        WaveformUtils.Delay(Channels[i].filter, Channels[i].delaySamples);
                    } else {
                        configFile.AddRange(new string[] {
                            $"  channel_{label}_delay:",
                            "    type: Delay",
                            "    parameters:",
                            "      delay: " + Channels[i].delaySamples,
                            "      unit: samples",
                            "      subsample: false"
                        });
                        additionalDelays[i] = true;
                    }
                }

                RIFFWaveWriter.Write(Path.Combine(folder, filterPath), Channels[i].filter, 1, SampleRate, BitDepth.Float32);
            }

            configFile.Add("pipeline:");
            for (int i = 0; i < Channels.Length; i++) {
                string label = Channels[i].name ?? EqualizerAPOUtils.GetChannelLabel(i, Channels.Length);
                configFile.AddRange(new string[] {
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
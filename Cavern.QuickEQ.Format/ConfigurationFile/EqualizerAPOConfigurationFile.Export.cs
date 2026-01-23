using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cavern.Filters;
using Cavern.Filters.Interfaces;
using Cavern.Filters.Utilities;

namespace Cavern.Format.ConfigurationFile {
    // Export algorithm for Equalizer APO
    partial class EqualizerAPOConfigurationFile {
        /// <summary>
        /// Add this step to the <paramref name="configuration"/>, and overwrite the last line if it selected a channel
        /// it did not filter, rendering that line redundant.
        /// </summary>
        static void AppendSelector(List<string> configuration, string newLine) {
            int last = configuration.Count - 1;
            if (last != -1 && configuration[last].StartsWith(channelFilter)) {
                configuration[last] = newLine; // No filter comes after this selector, overwrite it
            } else {
                configuration.Add(newLine);
            }
        }

        /// <summary>
        /// Get the export path of configuration filters by index.
        /// </summary>
        static string ConvolutionFileName(string convolutionRoot, int index) => $"{convolutionRoot}_{index}.wav";

        /// <summary>
        /// Throw an <see cref="DuplicateLabelException"/> if the filter set contains a non-channel label twice.
        /// </summary>
        static void ValidateForExport((FilterGraphNode node, int _)[] exportOrder) {
            HashSet<string> labels = new HashSet<string>();
            for (int i = 0; i < exportOrder.Length; i++) {
                if (exportOrder[i].node.Filter is BypassFilter label && !(label is EndpointFilter)) {
                    if (labels.Contains(label.Name)) {
                        throw new DuplicateLabelException(label.Name);
                    } else {
                        labels.Add(label.Name);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override void Export(string path) {
            (List<string> lines, List<IConvolution> convolutions) = ExportToMemory(path);
            File.WriteAllLines(path, lines);
            ExportConvolutions(convolutions, path);
        }

        /// <summary>
        /// Write the convolution files defined by <see cref="ExportToMemory(string)"/> to disk.
        /// </summary>
        public void ExportConvolutions(List<IConvolution> convolutions, string path) {
            string folder = Path.GetDirectoryName(path);
            string convolutionRoot = Path.GetFileNameWithoutExtension(path);
            for (int i = 0; i < convolutions.Count; i++) {
                string convolutionFile = Path.Combine(folder, ConvolutionFileName(convolutionRoot, i));
                RIFFWaveWriter.Write(convolutionFile, convolutions[i].Impulse, 1, convolutions[i].SampleRate, BitDepth.Float32);
            }
        }

        /// <summary>
        /// Get the lines and convolution files that should be in an Equalizer APO configuration file to result in this configuration.
        /// </summary>
        public (List<string> lines, List<IConvolution> convolutions) ExportToMemory(string path) {
            Dictionary<int, string> virtualChannelNames = new Dictionary<int, string>();
            string GetChannelLabel(int channel) { // Convert index to label
                if (channel < 0) {
                    if (virtualChannelNames.ContainsKey(channel)) {
                        return virtualChannelNames[channel];
                    } else {
                        throw new NotPreparedChannelException("V" + -channel);
                    }
                } else {
                    if (channelLabels.Contains(InputChannels[channel].name) || channel > 7) {
                        return InputChannels[channel].name;
                    } else {
                        return channelLabels[channel];
                    }
                }
            }

            (FilterGraphNode node, int channel)[] exportOrder = GetExportOrder();
            ValidateForExport(exportOrder);
            List<string> result = new List<string>();
            List<IConvolution> convolutions = new List<IConvolution>();
            List<string> finalCopies = new List<string>();
            int lastChannel = int.MaxValue;
            string convolutionRoot = Path.GetFileNameWithoutExtension(path);
            for (int i = 0; i < exportOrder.Length; i++) {
                int channel = exportOrder[i].channel;
                FilterGraphNode node = exportOrder[i].node;
                Filter baseFilter = node.Filter;
                BypassFilter label = baseFilter is EndpointFilter ? null : baseFilter as BypassFilter;
                if (channel < 0 && !virtualChannelNames.ContainsKey(channel)) {
                    if (label == null) {
                        virtualChannelNames[channel] = "V" + -channel;
                    } else {
                        virtualChannelNames[channel] = label.Name.Replace(" ", string.Empty);
                    }
                }

                int[] parents = GetExportedParents(exportOrder, i);
                if (parents.Length == 0 || (parents.Length == 1 && parents[0] == channel)) {
                    // If there are no parents or the single parent is from the same channel, don't mix
                } else {
                    string copy = $"{GetChannelLabel(channel)}={string.Join('+', parents.Select(GetChannelLabel))}";
                    if (baseFilter is OutputChannel && exportOrder[i].node.Children.Count == 0) {
                        finalCopies.Add(copy);
                    } else {
                        AppendSelector(result, "Copy: " + copy);
                    }
                }

                if (channel != lastChannel) { // When the channel has changed, select it
                    AppendSelector(result, channelFilter + GetChannelLabel(channel));
                    lastChannel = channel;
                }

                if (baseFilter == null || baseFilter is BypassFilter) {
                    continue;
                }
                if (baseFilter is IEqualizerAPOFilter filter) {
                    filter.ExportToEqualizerAPO(result);
                } else if (baseFilter is IConvolution convolution) {
                    result.Add(convolutionFilter + ConvolutionFileName(convolutionRoot, convolutions.Count));
                    convolutions.Add(convolution);
                } else {
                    throw new NotEqualizerAPOFilterException(baseFilter);
                }
            }

            if (finalCopies.Count != 0) {
                AppendSelector(result, "Copy: " + string.Join(' ', finalCopies));
            }

            int last = result.Count - 1;
            if (last != -1 && result[last].StartsWith(channelFilter)) {
                result.RemoveAt(last); // A selector of a bypass might remain
            }
            return (result, convolutions);
        }
    }
}

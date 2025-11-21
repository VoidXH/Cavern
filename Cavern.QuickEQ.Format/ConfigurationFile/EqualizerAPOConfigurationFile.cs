using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Interfaces;
using Cavern.Filters.Utilities;
using Cavern.Format.Common;
using Cavern.Format.ConfigurationFile.Helpers;
using Cavern.QuickEQ.Equalization;
using Cavern.Utilities;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Parsed single Equalizer APO configuration file.
    /// </summary>
    public sealed class EqualizerAPOConfigurationFile : ConfigurationFile {
        /// <inheritdoc/>
        public override string FileExtension => "txt";

        /// <summary>
        /// Convert an<paramref name="other"/> configuration file to Equalizer APO's format.
        /// </summary>
        public EqualizerAPOConfigurationFile(ConfigurationFile other) : base(other) { }

        /// <summary>
        /// Create an empty Equalizer APO configuration file.
        /// </summary>
        /// <param name="name">Name of the filter set</param>
        /// <param name="channelCount">Number of processed system channels</param>
        /// <param name="apoLabels">Use Equalizer APO channel labeling instead of the standard layout for the <paramref name="channelCount"/></param>
        public EqualizerAPOConfigurationFile(string name, int channelCount, bool apoLabels) : base(name, GetChannelLabels(channelCount, apoLabels)) => FinishEmpty();

        /// <summary>
        /// Parse a single Equalizer APO configuration file.
        /// </summary>
        /// <param name="path">Filesystem location of the configuration file</param>
        /// <param name="sampleRate">The sample rate to use for the internally created filters</param>
        public EqualizerAPOConfigurationFile(string path, int sampleRate) : base(Path.GetFileNameWithoutExtension(path), channelLabels) {
            Dictionary<string, FilterGraphNode> lastNodes = InputChannels.ToDictionary(x => x.name, x => x.root);
            List<string> activeChannels = channelLabels.ToList();
            AddConfigFile(path, lastNodes, activeChannels, sampleRate);

            for (int i = 0; i < channelLabels.Length; i++) { // Output markers
                lastNodes[channelLabels[i]].AddChild(new FilterGraphNode(new OutputChannel(channelLabels[i])));
            }
            Optimize();
            FinishLazySetup(131072);
        }

        /// <summary>
        /// Get the channel index to label mapping for a given number of channels.
        /// </summary>
        /// <param name="count">Number of channels</param>
        /// <param name="apoLabels">Use Equalizer APO channel labeling instead of the standard layout for the channel <paramref name="count"/></param>
        public static string[] GetChannelLabels(int count, bool apoLabels) {
            string[] result = new string[count];
            if (apoLabels) {
                for (int i = 0; i < result.Length; i++) {
                    result[i] = EqualizerAPOUtils.GetChannelLabel(i, result.Length);
                }
            } else {
                ReferenceChannel[] channels = ChannelPrototype.GetStandardMatrix(count);
                for (int i = 0; i < result.Length; i++) {
                    result[i] = channels[i] == ReferenceChannel.Unknown ? "CH" + (i + 1) : EqualizerAPOUtils.GetChannelLabel(channels[i]);
                }
            }
            return result;
        }

        /// <summary>
        /// Add a filter to the currently active channels.
        /// </summary>
        static void AddFilter(Dictionary<string, FilterGraphNode> lastNodes, List<string> channels, Filter filter) {
            List<FilterGraphNode> addedTo = new List<FilterGraphNode>();
            bool clone = false; // Filters have to be individually editable on different paths = make copies after the first was set
            for (int i = 0, c = channels.Count; i < c; i++) {
                FilterGraphNode oldLastNode = lastNodes[channels[i]];
                if (addedTo.Contains(oldLastNode)) {
                    lastNodes[channels[i]] = oldLastNode.Children[^1]; // The channel pipelines were merged with a Copy filter
                } else {
                    lastNodes[channels[i]] = oldLastNode.AddChild(clone ? (Filter)filter.Clone() : filter);
                    clone = true;
                    addedTo.Add(oldLastNode);
                }
            }
        }

        /// <summary>
        /// Parse a Copy filter from the last <paramref name="split"/> of the configuration file. Mixing will be handled by edges and
        /// <see cref="Gain"/> filters where needed.
        /// </summary>
        static void AddCopyFilter(Dictionary<string, FilterGraphNode> lastNodes, string[] split) {
            Dictionary<string, FilterGraphNode> oldLastNodes = lastNodes.ToDictionary(x => x.Key, x => x.Value);
            for (int i = 1; i < split.Length; i++) {
                string[] copy = split[i].Split(new[] { '=', '+' });
                FilterGraphNode target = new FilterGraphNode(null);
                for (int j = 1; j < copy.Length; j++) {
                    string channel;
                    int mul = copy[j].IndexOf('*');
                    if (mul != -1) {
                        channel = copy[j][(mul + 1)..];
                        double copyGain = double.Parse(copy[j][..mul].Replace(',', '.'), CultureInfo.InvariantCulture),
                            gainDb = QMath.GainToDb(Math.Abs(copyGain));
                        Gain gainFilter = new Gain(gainDb) {
                            Invert = gainDb >= 0
                        };
                        FilterGraphNode gainNode = new FilterGraphNode(gainFilter);
                        gainNode.AddParent(oldLastNodes[channel]);
                        target.AddParent(gainNode);
                    } else {
                        channel = copy[j];
                        target.AddParent(oldLastNodes[channel]);
                    }
                }
                if (!lastNodes.ContainsKey(copy[0])) {
                    target.Filter = new BypassFilter(copy[0]);
                }
                lastNodes[copy[0]] = target;
            }
        }

        /// <summary>
        /// Get the export path of configuration filters by index.
        /// </summary>
        static string ConvolutionFileName(string convolutionRoot, int index) => $"{convolutionRoot}_{index}.wav";

        /// <summary>
        /// Parse a Channel filter and make the next parsed filters only affect those channels.
        /// </summary>
        static void SelectChannels(Dictionary<string, FilterGraphNode> lastNodes, List<string> activeChannels, string[] split) {
            activeChannels.Clear();
            if (split.Length == 2 && split[1].ToLowerInvariant() == "all") {
                activeChannels.AddRange(channelLabels);
                return;
            }

            for (int i = 1; i < split.Length; i++) {
                if (lastNodes.ContainsKey(split[i])) {
                    activeChannels.Add(split[i]);
                } else {
                    throw new InvalidChannelException(split[i]);
                }
            }
        }

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

            List<string> result = new List<string>();
            void AppendSelector(string newLine) { // Add this step, and overwrite the previous line if it selected an unfiltered channel
                int last = result.Count - 1;
                if (last != -1 && result[last].StartsWith(channelFilter)) {
                    result[last] = newLine; // No filter comes after this selector, overwrite it
                } else {
                    result.Add(newLine);
                }
            }

            (FilterGraphNode node, int channel)[] exportOrder = GetExportOrder();
            ValidateForExport(exportOrder);
            int lastChannel = int.MaxValue;
            List<IConvolution> convolutions = new List<IConvolution>();
            string convolutionRoot = Path.GetFileNameWithoutExtension(path);
            for (int i = 0; i < exportOrder.Length; i++) {
                int channel = exportOrder[i].channel;
                Filter baseFilter = exportOrder[i].node.Filter;
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
                    AppendSelector($"Copy: {GetChannelLabel(channel)}={string.Join('+', parents.Select(GetChannelLabel))}");
                }

                if (channel != lastChannel) { // When the channel has changed, select it
                    AppendSelector(channelFilter + GetChannelLabel(channel));
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

            int last = result.Count - 1;
            if (last != -1 && result[last].StartsWith(channelFilter)) {
                result.RemoveAt(last); // A selector of a bypass might remain
            }
            return (result, convolutions);
        }

        /// <summary>
        /// If there are operations in 8-channel space before the actual processing (like Spatial Remapping),
        /// swap channels to their correct labels in a new split point.
        /// </summary>
        public void Downmap(int actualChannels) {
            if (InputChannels.Length < 8 || actualChannels > 6 || actualChannels <= 2) {
                return; // Only supported case: 7.1+ container for a 5.1 or quadraphonic system
            }
            int splitIndex = SplitPoints.Count;
            AddSplitPoint(splitIndex, "Downmap");
            if (actualChannels > 4) { // For 5.1: swap surrounds with rears as that's their position in 7.1
                FilterGraphNode rearLeft = GetSplitPointRoot(splitIndex, 4);
                FilterGraphNode rearRight = GetSplitPointRoot(splitIndex, 5);
                FilterGraphNode surroundLeft = GetSplitPointRoot(splitIndex, 6);
                FilterGraphNode surroundRight = GetSplitPointRoot(splitIndex, 7);
                rearLeft.SwapChildren(surroundLeft);
                rearRight.SwapChildren(surroundRight);
            } else { // For quadraphonic: swap C/LFE with rears
                FilterGraphNode center = GetSplitPointRoot(splitIndex, 2);
                FilterGraphNode lfe = GetSplitPointRoot(splitIndex, 3);
                FilterGraphNode rearLeft = GetSplitPointRoot(splitIndex, 4);
                FilterGraphNode rearRight = GetSplitPointRoot(splitIndex, 5);
                center.SwapChildren(rearLeft);
                lfe.SwapChildren(rearRight);
            }
        }

        /// <summary>
        /// Read a configuration file and append it to the previously parsed configuration.
        /// </summary>
        void AddConfigFile(string path, Dictionary<string, FilterGraphNode> lastNodes, List<string> activeChannels, int sampleRate) {
            foreach (string line in File.ReadLines(path)) {
                string[] split = line.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length <= 1) {
                    continue;
                }

                switch (split[0].ToLowerInvariant()) {
                    // Control
                    case "include":
                        string included = Path.Combine(Path.GetDirectoryName(path), string.Join(' ', split, 1, split.Length - 1));
                        CreateSplit(Path.GetFileNameWithoutExtension(included), lastNodes);
                        AddConfigFile(included, lastNodes, activeChannels, sampleRate);
                        break;
                    case "channel":
                        SelectChannels(lastNodes, activeChannels, split);
                        break;
                    // Basic filters
                    case "preamp":
                        double gain = double.Parse(split[1].Replace(',', '.'), CultureInfo.InvariantCulture);
                        AddFilter(lastNodes, activeChannels, new Gain(gain));
                        break;
                    case "delay":
                        AddFilter(lastNodes, activeChannels, Delay.FromEqualizerAPO(split, sampleRate));
                        break;
                    case "copy":
                        AddCopyFilter(lastNodes, split);
                        break;
                    // Parametric filters
                    case "filter":
                        AddFilter(lastNodes, activeChannels, BiquadFilter.FromEqualizerAPO(split, sampleRate));
                        break;
                    // Graphic equalizers
                    case "graphiceq":
                        AddFilter(lastNodes, activeChannels, new LazyGraphicEQ(EQGenerator.FromEqualizerAPO(split), sampleRate));
                        break;
                    case "convolution":
                        string convolution = Path.Combine(Path.GetDirectoryName(path), line[(line.IndexOf(' ') + 1)..]);
                        AddFilter(lastNodes, activeChannels, new FastConvolver(AudioReader.Open(convolution).Read()));
                        break;
                }
            }
        }

        /// <summary>
        /// Mark the current point of the configuration as the beginning of the next section of filters or next pipeline step.
        /// </summary>
        void CreateSplit(string name, Dictionary<string, FilterGraphNode> lastNodes) {
            KeyValuePair<string, FilterGraphNode>[] outputs =
                lastNodes.Where(x => ReferenceChannelExtensions.FromStandardName(x.Key) != ReferenceChannel.Unknown).ToArray();
            for (int i = 0; i < outputs.Length; i++) {
                lastNodes[outputs[i].Key] = lastNodes[outputs[i].Key].AddChild(new OutputChannel(outputs[i].Key));
            }
            CreateNewSplitPoint(name);
            for (int i = 0; i < outputs.Length; i++) {
                lastNodes[outputs[i].Key] = lastNodes[outputs[i].Key].Children[0];
            }
        }

        /// <summary>
        /// Default initial channels in Equalizer APO.
        /// </summary>
        static readonly string[] channelLabels = { "L", "R", "C", "SUB", "RL", "RR", "SL", "SR" };

        /// <summary>
        /// Prefix for channel selection lines in an Equalizer APO configuration file.
        /// </summary>
        const string channelFilter = "Channel: ";

        /// <summary>
        /// Prefix for convolution file selection lines in an Equalizer APO configuration file.
        /// </summary>
        const string convolutionFilter = "Convolution: ";
    }
}

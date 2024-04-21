using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.Format.Common;

namespace Cavern.Format.ConfigurationFile {
    /// <summary>
    /// Parsed single Equalizer APO configuration file.
    /// </summary>
    public class EqualizerAPOConfigurationFile : ConfigurationFile {
        /// <summary>
        /// Parse a single Equalizer APO configuration file.
        /// </summary>
        /// <param name="path">Filesystem location of the configuration file</param>
        /// <param name="sampleRate">The sample rate to use when</param>
        public EqualizerAPOConfigurationFile(string path, int sampleRate) : base(channelLabels) {
            Dictionary<string, FilterGraphNode> lastNodes = InputChannels.ToDictionary(x => x.name, x => x.root);
            List<string> activeChannels = channelLabels.ToList();
            foreach (string line in File.ReadLines(path)) {
                string[] split = line.Split(new[] { ':', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (split.Length <= 1) {
                    continue;
                }

                switch (split[0].ToLower()) {
                    case "channel":
                        string[] channels = split[1].Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        activeChannels.Clear();
                        for (int i = 0; i < channels.Length; i++) {
                            if (lastNodes.ContainsKey(channels[i])) {
                                activeChannels.Add(channels[i]);
                            } else {
                                throw new InvalidChannelException(channels[i]);
                            }
                        }
                        break;
                    case "preamp":
                        double gain = double.Parse(split[1].Replace(',', '.'), CultureInfo.InvariantCulture);
                        AddFilter(lastNodes, activeChannels, new Gain(gain));
                        break;
                    case "delay":
                        double delay = double.Parse(split[1].Replace(',', '.'), CultureInfo.InvariantCulture);
                        switch (split[2].ToLower()) {
                            case "ms":
                                AddFilter(lastNodes, activeChannels, new Delay(delay, sampleRate));
                                break;
                            case "samples":
                                AddFilter(lastNodes, activeChannels, new Delay((int)delay));
                                break;
                            default:
                                throw new ArgumentOutOfRangeException(split[0]);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Add a filter to the currently active channels.
        /// </summary>
        void AddFilter(Dictionary<string, FilterGraphNode> lastNodes, List<string> channels, Filter filter) {
            for (int i = 0, c = channels.Count; i < c; i++) {
                lastNodes[channels[i]] = lastNodes[channels[i]].AddChild(filter);
            }
        }

        /// <summary>
        /// Default initial channels in Equalizer APO.
        /// </summary>
        static readonly string[] channelLabels = { "L", "R", "C", "SUB", "RL", "RR", "SL", "SR" };
    }
}
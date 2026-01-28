using System.Collections.Generic;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Interfaces;
using Cavern.Filters.Utilities;
using Cavern.Format.Common;
using Cavern.QuickEQ.SignalGeneration;
using Cavern.Utilities;

using ConfigFile = Cavern.Format.ConfigurationFile.ConfigurationFile;

namespace Cavern.Format.Utilities {
    /// <summary>
    /// Produces impulse responses for each output channel with different input channels being active.
    /// </summary>
    public class ConfigurationFileSimulator {
        /// <summary>
        /// The configuration file to simulate.
        /// </summary>
        readonly ConfigFile configurationFile;

        /// <summary>
        /// Produces impulse responses for each output channel with different input channels being active.
        /// </summary>
        public ConfigurationFileSimulator(ConfigFile configurationFile) => this.configurationFile = configurationFile;

        /// <summary>
        /// Produces impulse responses for each output channel with different input channels being active.
        /// </summary>
        /// <param name="channels">Channel indices that have input Dirac-delta signals</param>
        /// <param name="samples">Number of samples in the resulting impulse response</param>
        public MultichannelWaveform Simulate(int[] channels, int samples) {
            (string name, FilterGraphNode root)[] sources = configurationFile.InputChannels;
            MultichannelWaveform result = new MultichannelWaveform(sources.Length, samples);
            for (int i = 0; i < sources.Length; i++) {
                if (channels.Contains(i)) {
                    float[] initialSignal = WaveformGenerator.DiracDelta(samples);
                    Simulate(sources[i].root, initialSignal, result);
                }
            }
            return result;
        }

        /// <summary>
        /// Produces impulse responses for each output channel with different input channels being active.
        /// </summary>
        /// <param name="channels">Channel indices that have input Dirac-delta signals</param>
        /// <param name="samples">Number of samples in the resulting impulse response</param>
        public MultichannelWaveform Simulate(ReferenceChannel[] channels, int samples) {
            (string name, FilterGraphNode root)[] sources = configurationFile.InputChannels;
            MultichannelWaveform result = new MultichannelWaveform(sources.Length, samples);
            for (int i = 0; i < sources.Length; i++) {
                FilterGraphNode root = sources[i].root;
                ReferenceChannel channel = ((InputChannel)root.Filter).Channel;
                if (channels.Contains(channel)) {
                    float[] initialSignal = WaveformGenerator.DiracDelta(samples);
                    Simulate(root, initialSignal, result);
                }
            }
            return result;
        }

        /// <summary>
        /// Simulate impulse response for the given <paramref name="node"/> and pass the simulation down to children, until it reaches an output
        /// where it gets added to the <paramref name="result"/>.
        /// </summary>
        void Simulate(FilterGraphNode node, float[] signal, MultichannelWaveform result) {
            if (node.Filter is OutputChannel output) {
                int channel = configurationFile.InputChannels
                    .IndexOf(x => x.name == output.ChannelName || ((InputChannel)x.root.Filter).Channel == output.Channel);
                if (channel != -1) {
                    WaveformUtils.Mix(signal, result[channel]);
                } else {
                    throw new InvalidChannelException(output.ChannelName);
                }
            }

            IReadOnlyList<FilterGraphNode> children = node.Children;
            node.Filter.Process(signal);
            if (node.Filter is IResettableFilter resettable) {
                resettable.Reset();
            }

            if (children.Count == 0) {
                return;
            }
            Simulate(children[0], signal, result);
            for (int i = 1, c = children.Count; i < c; i++) {
                Simulate(children[1], signal.FastClone(), result);
            }
        }
    }
}

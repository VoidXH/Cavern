using System.Collections.Generic;
using System.Linq;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.QuickEQ.Crossover;
using Cavern.Utilities;

using Crossover = Cavern.QuickEQ.Crossover.Crossover;

namespace Cavern.Format.ConfigurationFile.Presets {
    /// <summary>
    /// An added crossover step to a <see cref="ConfigurationFile"/> filter graph.
    /// </summary>
    public class CrossoverFilterSet : FilterSetPreset {
        /// <summary>
        /// User-defined name of this crossover that will be given to the created split point.
        /// </summary>
        readonly string name;

        /// <summary>
        /// Crossover implementation algorithm.
        /// </summary>
        readonly CrossoverType type;

        /// <summary>
        /// Sample rate of the DSP this filter set is applied to.
        /// </summary>
        readonly int sampleRate;

        /// <summary>
        /// If the crossover <see cref="type"/> can only be implemented as a convolution, this will be its sample count.
        /// </summary>
        readonly int filterLength;

        /// <summary>
        /// The target channel to cross over to.
        /// </summary>
        readonly ReferenceChannel targetChannel;

        /// <summary>
        /// The channels to cross over to the <see cref="targetChannel"/>.
        /// </summary>
        readonly (ReferenceChannel channel, float frequency)[] sourceChannels;

        /// <summary>
        /// An added crossover step to a <see cref="ConfigurationFile"/> filter graph.
        /// </summary>
        /// <param name="name">User-defined name of this crossover that will be given to the created split point</param>
        /// <param name="type">Crossover implementation algorithm</param>
        /// <param name="sampleRate">Sample rate of the DSP this filter set is applied to</param>
        /// <param name="filterLength">If the crossover <see cref="type"/> can only be implemented as a convolution,
        /// this will be its sample count</param>
        /// <param name="targetChannel">The target channel to cross over to</param>
        /// <param name="sourceChannels">The channels to cross over to the <see cref="targetChannel"/></param>
        public CrossoverFilterSet(string name, CrossoverType type, int sampleRate, int filterLength, ReferenceChannel targetChannel,
            params (ReferenceChannel channel, float frequency)[] sourceChannels) {
            this.name = name;
            this.type = type;
            this.sampleRate = sampleRate;
            this.filterLength = filterLength;
            this.targetChannel = targetChannel;
            this.sourceChannels = sourceChannels;
        }

        /// <inheritdoc/>
        public override void Add(ConfigurationFile file, int index) {
            file.AddSplitPoint(index, name);
            FilterGraphNode lowpassOut = file.GetSplitPointRoot(index, targetChannel).Children[0]; // OutputChannel filter for the target
            float[] freqsPerChannel = sourceChannels.GetItem2s();
            float[] freqs = freqsPerChannel.Distinct().ToArray();
            Crossover generator = Crossover.Create(type, freqsPerChannel, new bool[sourceChannels.Length]);
            Dictionary<float, FilterGraphNode> aggregators = new Dictionary<float, FilterGraphNode>();
            for (int i = 0; i < freqs.Length; i++) {
                Filter lowpass = generator.GetLowpassOptimized(sampleRate, freqs[i], filterLength);
                FilterGraphNode node = new FilterGraphNode(lowpass);
                node.AddChild(lowpassOut);
                aggregators[freqs[i]] = node;
            }

            for (int i = 0; i < sourceChannels.Length; i++) {
                Filter highpass = generator.GetHighpassOptimized(sampleRate, freqsPerChannel[i], filterLength);
                FilterGraphNode root = file.GetSplitPointRoot(index, sourceChannels[i].channel);
                root.AddBeforeChildren(highpass);
                root.AddChild(aggregators[freqsPerChannel[i]]);
            }
        }
    }
}
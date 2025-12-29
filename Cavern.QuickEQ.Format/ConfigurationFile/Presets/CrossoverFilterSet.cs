using System;
using System.Collections.Generic;

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
        /// Gain for each crossovered channel before being mixed to the target channel(s).
        /// </summary>
        public double CrossoverGain { get; set; } = -10;

        /// <summary>
        /// Apply this filter on each crossovered signal in addition to the crossover.
        /// </summary>
        public Filter OnCrossovered { get; set; }

        /// <summary>
        /// Apply this filter on each LFE input.
        /// </summary>
        public Filter OnLFEInput { get; set; }

        /// <summary>
        /// Apply this filter on the combined post-crossover LFE signal (all crossovered bass and LFE).
        /// </summary>
        public Filter OnEntireBass { get; set; }

        /// <summary>
        /// User-defined name of this crossover that will be given to the created split point.
        /// </summary>
        readonly string name;

        /// <summary>
        /// Crossover implementation algorithm.
        /// </summary>
        readonly Crossover generator;

        /// <summary>
        /// Sample rate of the DSP this filter set is applied to.
        /// </summary>
        readonly int sampleRate;

        /// <summary>
        /// If the crossover <see cref="generator"/> can only be implemented as a convolution, this will be its sample count.
        /// </summary>
        readonly int filterLength;

        /// <summary>
        /// An added crossover step to a <see cref="ConfigurationFile"/> filter graph.
        /// </summary>
        /// <param name="name">User-defined name of this crossover that will be given to the created split point</param>
        /// <param name="type">Crossover implementation algorithm</param>
        /// <param name="sampleRate">Sample rate of the DSP this filter set is applied to</param>
        /// <param name="filterLength">If the crossover <paramref name="type"/> can only be implemented as a convolution,
        /// this will be its sample count</param>
        /// <param name="mixing">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        public CrossoverFilterSet(string name, CrossoverType type, int sampleRate, int filterLength, CrossoverDescription mixing) {
            this.name = name;
            generator = Crossover.Create(type, mixing);
            this.sampleRate = sampleRate;
            this.filterLength = filterLength;
        }

        /// <summary>
        /// An added crossover step to a <see cref="ConfigurationFile"/> filter graph.
        /// </summary>
        /// <param name="name">User-defined name of this crossover that will be given to the created split point</param>
        /// <param name="crossover">Fetch crossover data from this</param>
        /// <param name="sampleRate">Sample rate of the DSP this filter set is applied to</param>
        /// <param name="filterLength">If the <paramref name="crossover"/> can only be implemented as a convolution,
        /// this will be its sample count</param>
        public CrossoverFilterSet(string name, Crossover crossover, int sampleRate, int filterLength) {
            this.name = name;
            generator = crossover;
            this.sampleRate = sampleRate;
            this.filterLength = filterLength;
        }

        /// <inheritdoc/>
        public override void Add(ConfigurationFile file, int index) {
            file.AddSplitPoint(index, name);
            int[] outputs = generator.Mixing.GetOutputs();
            if (outputs.Length == 0) {
                return;
            }

            FilterGraphNode lowpassMix;
            if (outputs.Length == 1) {
                FilterGraphNode lfeInput = file.GetSplitPointRoot(index, outputs[0]);
                FilterGraphNode lfeOutput = lfeInput.Children[0];
                lowpassMix = lfeOutput.AddParent(new Gain(CrossoverGain)); // Final mixing gain
                if (OnLFEInput != null) {
                    lfeInput.AddBeforeChildren((Filter)OnLFEInput.Clone());
                }
                if (OnEntireBass != null) {
                    lfeOutput.AddAfterParents((Filter)OnEntireBass.Clone());
                }
            } else {
                FilterGraphNode lfeMerge = new FilterGraphNode(new BypassFilter("LFE merge"));
                for (int i = 0; i < outputs.Length; i++) {
                    FilterGraphNode root = file.GetSplitPointRoot(index, outputs[i]);
                    root.AddBeforeChildren(lfeMerge);
                    if (OnLFEInput != null) {
                        root.AddBeforeChildren((Filter)OnLFEInput.Clone());
                    }
                }
                lowpassMix = new FilterGraphNode(new Gain(CrossoverGain + QMath.GainToDb(1 / MathF.Sqrt(outputs.Length))));
                lfeMerge.AddParent(lowpassMix);
                if (OnEntireBass != null) {
                    lfeMerge.AddBeforeChildren((Filter)OnEntireBass.Clone());
                }
            }

            if (OnCrossovered != null) {
                lowpassMix.AddBeforeChildren((Filter)OnCrossovered.Clone());
            }

            (float frequency, int[])[] groups = generator.CrossoverGroups;
            Dictionary<float, FilterGraphNode> aggregators = new Dictionary<float, FilterGraphNode>();
            for (int i = 0; i < groups.Length; i++) {
                float freq = groups[i].frequency;
                Filter lowpass = generator.GetLowpassOptimized(sampleRate, freq, filterLength);
                FilterGraphNode node = new FilterGraphNode(lowpass);
                node.AddChild(lowpassMix);
                aggregators[freq] = node;
            }

            (bool mixHere, float freq)[] mixing = generator.Mixing.Mixing;
            for (int i = 0; i < mixing.Length; i++) {
                float freq = mixing[i].freq;
                if (freq <= 0) {
                    continue;
                }

                Filter highpass = generator.GetHighpassOptimized(sampleRate, freq, filterLength);
                FilterGraphNode root = file.GetSplitPointRoot(index, i);
                root.AddBeforeChildren(highpass);
                root.AddChild(aggregators[freq]);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Cavern.Format.Utilities;

namespace Cavern.Format.ConfigurationFile.ConvolutionBoxFormat {
    /// <summary>
    /// A mixing filter in a <see cref="ConvolutionBoxFormatConfigurationFile"/>.
    /// </summary>
    class MatrixEntry : CBFEntry {
        /// <summary>
        /// Which channels to mix to which other. When multiple sources mix to the same target, the signals shall be merged.
        /// </summary>
        public IReadOnlyList<(int source, int[] targets)> Mixes => mixes;

        /// <summary>
        /// Which channels to mix to which other.
        /// </summary>
        readonly List<(int source, int[] targets)> mixes;

        /// <summary>
        /// A mixing filter in a <see cref="ConvolutionBoxFormatConfigurationFile"/>.
        /// </summary>
        public MatrixEntry() => mixes = new List<(int, int[])>();

        /// <summary>
        /// A mixing filter from a <see cref="ConvolutionBoxFormatConfigurationFile"/> <paramref name="stream"/>.
        /// </summary>
        public MatrixEntry(Stream stream) : this() {
            int count = stream.ReadInt32();
            for (int i = 0; i < count; i++) {
                int source = stream.ReadInt32();
                int[] targets = new int[stream.ReadInt32()];
                for (int target = 0; target < targets.Length; target++) {
                    targets[target] = stream.ReadInt32();
                }
                mixes.Add((source, targets));
            }
        }

        /// <summary>
        /// Add a new entry to this mixing table of 1 channel being mixed to any number of channels.
        /// </summary>
        public void Expand(int sourceChannel, params int[] targetChannels) => mixes.Add((sourceChannel, targetChannels));

        /// <summary>
        /// Add a new entry to this mixing table of many channels all being mixed to any number of channels.
        /// </summary>
        public void Expand(int[] sourceChannels, params int[] targetChannels) {
            for (int i = 0; i < sourceChannels.Length; i++) {
                mixes.Add((sourceChannels[i], targetChannels));
            }
        }

        /// <inheritdoc/>
        public override void Write(Stream target) {
            Cleanup();
            target.WriteByte(0);
            int count = mixes.Count;
            target.WriteAny(count);
            for (int i = 0; i < count; i++) {
                target.WriteAny(mixes[i].source);
                int[] targets = mixes[i].targets;
                target.WriteAny(targets.Length);
                for (int j = 0; j < targets.Length; j++) {
                    target.WriteAny(targets[j]);
                }
            }
        }

        /// <inheritdoc/>
        public override string ToString() => string.Join(", ", mixes.Select(x => $"{x.source} -> [{string.Join(", ", x.targets)}]"));

        /// <summary>
        /// Remove duplications of the same operation if present.
        /// </summary>
        void Cleanup() {
            List<int> toRemove = new List<int>();
            for (int i = 0, c = mixes.Count; i < c; i++) {
                int source = mixes[i].source;
                int[] targets = mixes[i].targets;
                for (int target = 0; target < targets.Length; target++) {
                    for (int j = i + 1; j < c; j++) {
                        if (toRemove.Contains(j)) {
                            continue;
                        }
                        int source2 = mixes[j].source;
                        int[] targets2 = mixes[j].targets;
                        for (int target2 = 0; target2 < targets2.Length; target2++) {
                            if (source == source2 && targets[target] == targets2[target2]) {
                                if (targets2.Length == 1) {
                                    toRemove.Add(j);
                                } else {
                                    targets2[target2] = targets2[^1];
                                    Array.Resize(ref targets2, targets2.Length - 1);
                                }
                            }
                        }
                    }
                }
            }
            toRemove.Sort();
            for (int i = toRemove.Count - 1; i >= 0; i--) {
                mixes.RemoveAt(toRemove[i]);
            }
         }
    }
}
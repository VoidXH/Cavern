using System;
using System.Collections.Generic;
using System.Linq;

namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// Contains mixing data for crossovers.
    /// </summary>
    public sealed class CrossoverDescription {
        /// <summary>
        /// Which channels to mix to, and which channels to mix from at what crossover frequency.
        /// </summary>
        public (bool mixHere, float crossoverFreq)[] Mixing { get; }

        /// <summary>
        /// Number of channels contained.
        /// </summary>
        public int Channels => Mixing.Length;

        /// <summary>
        /// Contains mixing data for crossovers.
        /// </summary>
        /// <param name="channels">Which channels to mix to, and which channels to mix from at what crossover frequency</param>
        public CrossoverDescription(params (bool mixHere, float crossoverFreq)[] channels) {
            Mixing = channels;
        }

        /// <summary>
        /// Get which channel indices to cross bass over to.
        /// </summary>
        public int[] GetOutputs() {
            int[] result = new int[Mixing.Length];
            int results = 0;
            for (int i = 0; i < Mixing.Length; i++) {
                if (Mixing[i].mixHere) {
                    result[results++] = i;
                }
            }
            Array.Resize(ref result, results);
            return result;
        }

        /// <summary>
        /// Get a lossy representation of the description in the format of (frequency, indices of channels crossovered at that frequency).
        /// </summary>
        /// <remarks>This data is lossy because it doesn't contain the output channels.</remarks>
        public (float frequency, int[] channels)[] ConvertToGroups() {
            Dictionary<float, List<int>> result = new Dictionary<float, List<int>>();
            for (int i = 0; i < Channels; i++) {
                float freq = Mixing[i].crossoverFreq;
                if (freq <= 0) {
                    continue;
                }

                if (result.ContainsKey(freq)) {
                    result[freq].Add(i);
                } else {
                    result[freq] = new List<int> { i };
                }
            }
            return result.Select(x => (x.Key, x.Value.ToArray())).ToArray();
        }
    }
}

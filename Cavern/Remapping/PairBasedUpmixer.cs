using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Cavern.Remapping {
    /// <summary>
    /// An <see cref="Upmixer"/> that recreates intermediate points between pairs of input sources.
    /// </summary>
    public abstract class PairBasedUpmixer : Upmixer {
        /// <summary>
        /// Location of the input channels in the environment.
        /// </summary>
        protected readonly Vector3[] positions;

        /// <summary>
        /// Pairs of indices of inputs to recreate the space between.
        /// </summary>
        protected readonly (int, int)[] pairs;

        /// <summary>
        /// An <see cref="Upmixer"/> that recreates intermediate points between pairs of input sources.
        /// </summary>
        /// <param name="positions">Location of the input channels in the environment</param>
        /// <param name="pairs">Pairs of indices of later given inputs to recreate the space between</param>
        /// <param name="intermediateSourceCount">Number of bands to separate</param>
        /// <param name="sampleRate">Content sample rate</param>
        protected PairBasedUpmixer(Vector3[] positions, (int, int)[] pairs, int intermediateSourceCount, int sampleRate) :
            base(pairs.Length * intermediateSourceCount, sampleRate) {
            this.positions = positions;
            this.pairs = pairs;
        }

        /// <summary>
        /// Connects channels in a circle on each height level.
        /// </summary>
        public static (int, int)[] GetLayeredPairs(Vector3[] positions) {
            Dictionary<float, List<int>> layers = new Dictionary<float, List<int>>();
            float[] angles = new float[positions.Length];
            for (int i = 0; i < positions.Length; i++) {
                if (!layers.ContainsKey(positions[i].Y)) {
                    layers[positions[i].Y] = new List<int>();
                }
                layers[positions[i].Y].Add(i);
                angles[i] = MathF.Atan(positions[i].X / positions[i].Z);
                if (positions[i].Z < 0) {
                    angles[i] += MathF.PI;
                }
            }

            (int, int)[] result = new (int, int)[layers.Sum(layer => layer.Value.Count > 1 ? layer.Value.Count : 0)];
            int resultIndex = 0;
            foreach (List<int> layer in layers.Values) {
                layer.Sort((a, b) => angles[a].CompareTo(angles[b]));
                int c = layer.Count - 1;
                if (c > 0) {
                    for (int i = 0; i < c; i++) {
                        result[resultIndex++] = (layer[i], layer[i + 1]);
                    }
                    result[resultIndex++] = (layer[c], layer[0]);
                }
            }
            return result;
        }
    }
}
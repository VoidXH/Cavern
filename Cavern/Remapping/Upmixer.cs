using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;

using Cavern.SpecialSources;
using Cavern.Utilities;

namespace Cavern.Remapping {
    /// <summary>
    /// Creates new, intermediate sources of an existing channel-based render.
    /// </summary>
    public abstract class Upmixer {
        /// <summary>
        /// Gets samples for each source for a given update rate.
        /// </summary>
        public delegate float[][] SampleCollector(int samplesPerSource);

        /// <summary>
        /// This function is called when new samples are needed for the next frame, it should return a frame for each source.
        /// </summary>
        public event SampleCollector OnSamplesNeeded;

        /// <summary>
        /// Output rendering sources.
        /// </summary>
        public Source[] CreatedSources => intermediateSources;

        /// <summary>
        /// Location of the input channels in the environment.
        /// </summary>
        protected readonly Vector3[] positions;

        /// <summary>
        /// Pairs of indices of inputs to recreate the space between.
        /// </summary>
        protected readonly (int, int)[] pairs;

        /// <summary>
        /// New rendered sources.
        /// </summary>
        protected readonly Source[] intermediateSources;

        /// <summary>
        /// Preallocated output source sample array reference cache.
        /// </summary>
        protected readonly float[][] output;

        /// <summary>
        /// Creates new, intermediate sources of an existing channel-based render.
        /// </summary>
        /// <param name="positions">Location of the input channels in the environment</param>
        /// <param name="pairs">Pairs of indices of later given inputs to recreate the space between</param>
        /// <param name="intermediateSourceCount">Number of bands to separate</param>
        /// <param name="sampleRate">Content sample rate</param>
        public Upmixer(Vector3[] positions, (int, int)[] pairs, int intermediateSourceCount, int sampleRate) {
            this.positions = positions;
            this.pairs = pairs;

            StreamMaster reader = new StreamMaster(UpdateSourcesFully);
            int sourceCount = pairs.Length * intermediateSourceCount;
            intermediateSources = new Source[sourceCount];
            output = new float[sourceCount][];
            output[0] = new float[0];
            for (int i = 0; i < sourceCount; i++) {
                intermediateSources[i] = new StreamMasterSource(reader, i) {
                    VolumeRolloff = Rolloffs.Disabled
                };
            }
            reader.SetupSources(intermediateSources, sampleRate);
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

        /// <summary>
        /// Uses the sample collector to read new samples.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected float[][] GetNewSamples(int samplesPerSource) => OnSamplesNeeded(samplesPerSource);

        /// <summary>
        /// Get the input samples, place the upmixed targets in space, and return their samples.
        /// </summary>
        protected abstract float[][] UpdateSources(int samplesPerSource);

        /// <summary>
        /// Get the input samples, place the upmixed targets in space, and return their samples.
        /// Calls the overridden <see cref="UpdateSources(int)"/> and makes sure the cache arrays are the correct size.
        /// </summary>
        float[][] UpdateSourcesFully(int samplesPerSource) {
            if (output[0].Length != samplesPerSource) {
                for (int i = 0; i < output.Length; i++) {
                    output[i] = new float[samplesPerSource];
                }
            }
            return UpdateSources(samplesPerSource);
        }
    }
}
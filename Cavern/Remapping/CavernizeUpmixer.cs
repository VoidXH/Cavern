using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Remapping {
    /// <summary>
    /// Creates height information for ground sources with the <see cref="Cavernize"/> filter.
    /// </summary>
    public class CavernizeUpmixer : Upmixer {
        /// <summary>
        /// Height separation effect strength.
        /// </summary>
        public float Effect { get; set; } = .75f;

        /// <summary>
        /// Smoothness of object movements, [0;1].
        /// </summary>
        public float Smoothness { get; set; } = .8f;

        /// <summary>
        /// Keep the center channel from gaining height.
        /// </summary>
        public bool CenterStays = true;

        /// <summary>
        /// Height separation filters for each channel.
        /// </summary>
        readonly Cavernize[] filters;

        /// <summary>
        /// Creates height information for ground sources with the <see cref="Cavernize"/> filter.
        /// The default crossover frequency of 250 Hz will be used.
        /// </summary>
        /// <param name="sources">Mono sources to upconvert, not attached to any <see cref="Listener"/></param>
        /// <param name="sampleRate">Content sample rate</param>
        /// <remarks>Positions of sources are taken on creation and will stay there, regardless of their further movement.</remarks>
        public CavernizeUpmixer(IReadOnlyList<Source> sources, int sampleRate) : this(sources, sampleRate, 250) { }

        /// <summary>
        /// Creates height information for ground sources with the <see cref="Cavernize"/> filter.
        /// </summary>
        /// <param name="sources">Mono sources to upconvert, not attached to any <see cref="Listener"/></param>
        /// <param name="sampleRate">Content sample rate</param>
        /// <param name="crossoverFrequency">Keep sounds below this frequency on the ground layer</param>
        /// <remarks>Positions of sources are taken on creation and will stay there, regardless of their further movement.</remarks>
        public CavernizeUpmixer(IReadOnlyList<Source> sources, int sampleRate, int crossoverFrequency) :
            base(sources.Sum(x => x.LFE ? 1 : 2), sampleRate) {
            filters = new Cavernize[sources.Count];
            for (int i = 0, offset = 0; i < filters.Length; i++) {
                IntermediateSources[offset].Position = sources[i].Position;
                if (sources[i].LFE) {
                    IntermediateSources[offset].LFE = true;
                } else {
                    filters[i] = new Cavernize(sampleRate, crossoverFrequency);
                }
                offset++;
            }
            SetupCollection(sources, sampleRate);
        }

        /// <summary>
        /// Get the input samples, place the upmixed targets in space, and return their samples.
        /// </summary>
        protected override float[][] UpdateSources(int samplesPerSource) {
            filters[0].Effect = Effect;
            filters[0].CalculateSmoothingFactor(samplesPerSource, Smoothness);
            bool centerToStay = CenterStays;
            for (int i = 0; i < filters.Length; i++) {
                if (filters[i] == null) {
                    continue;
                }

                Vector3 position = IntermediateSources[i].Position;
                if (centerToStay && position.X == 0 && position.Y == 0 && position.Z > 0) {
                    filters[i].Effect = 0;
                    centerToStay = false;
                } else {
                    filters[i].Effect = Effect;
                }
                filters[i].SmoothFactor = filters[0].SmoothFactor;
            }

            float[][] input = OnSamplesNeeded(samplesPerSource);
            int pair = input.Length;
            for (int i = 0; i < input.Length; i++) {
                if (IntermediateSources[i].LFE) {
                    Array.Copy(input[i], output[i], input[i].Length);
                } else {
                    filters[i].Process(input[i]);
                    filters[i].GroundLevel.CopyTo(output[i]);
                    filters[i].HeightLevel.CopyTo(output[pair]);
                    IntermediateSources[pair].Position = IntermediateSources[i].Position + new Vector3(0, filters[i].Height * Listener.EnvironmentSize.Y, 0);
                    pair++;
                }
            }
            return output;
        }
    }
}

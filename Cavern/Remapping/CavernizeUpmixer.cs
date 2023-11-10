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
        /// Mono sources to upconvert. Don't attach these to a <see cref="Listener"/>.
        /// </summary>
        readonly Source[] sources;

        /// <summary>
        /// Height separation filters for each channel.
        /// </summary>
        readonly Cavernize[] filters;

        /// <summary>
        /// A dummy listener to forefully get source samples.
        /// </summary>
        readonly Listener pinger;

        /// <summary>
        /// Creates height information for ground sources with the <see cref="Cavernize"/> filter.
        /// The default crossover frequency of 250 Hz will be used.
        /// </summary>
        /// <param name="sources">Mono sources to upconvert, not attached to any <see cref="Listener"/></param>
        /// <param name="sampleRate">Content sample rate</param>
        public CavernizeUpmixer(IList<Source> sources, int sampleRate) : this(sources, sampleRate, 250) { }

        /// <summary>
        /// Creates height information for ground sources with the <see cref="Cavernize"/> filter.
        /// </summary>
        /// <param name="sources">Mono sources to upconvert, not attached to any <see cref="Listener"/></param>
        /// <param name="sampleRate">Content sample rate</param>
        /// <param name="crossoverFrequency">Keep sounds below this frequency on the ground layer</param>
        public CavernizeUpmixer(IList<Source> sources, int sampleRate, int crossoverFrequency) :
            base(2 * sources.Count, sampleRate) {
            this.sources = sources.ToArray();

            filters = new Cavernize[sources.Count];
            for (int i = 0; i < filters.Length; i++) {
                filters[i] = new Cavernize(sampleRate, crossoverFrequency);
            }

            pinger = new Listener(false) {
                SampleRate = sampleRate
            };
            for (int i = 0; i < sources.Count; i++) {
                pinger.AttachSource(sources[i]);
            }
        }

        /// <summary>
        /// Get the input samples, place the upmixed targets in space, and return their samples.
        /// </summary>
        protected override float[][] UpdateSources(int samplesPerSource) {
            filters[0].Effect = Effect;
            filters[0].CalculateSmoothingFactor(samplesPerSource, Smoothness);
            bool centerToStay = CenterStays;
            for (int i = 1; i < filters.Length; i++) {
                if (centerToStay && sources[i].Position.X == 0 && sources[i].Position.Y == 0 && sources[i].Position.Z > 0) {
                    filters[i].Effect = 0;
                    centerToStay = false;
                } else {
                    filters[i].Effect = Effect;
                }
                filters[i].SmoothFactor = filters[0].SmoothFactor;
            }

            pinger.UpdateRate = samplesPerSource;
            pinger.Ping();
            for (int i = 0; i < sources.Length; i++) {
                int current = 2 * i,
                    pair = current + 1;
                filters[i].Process(sources[i].Rendered[0]);
                filters[i].GroundLevel.CopyTo(output[current]);
                filters[i].HeightLevel.CopyTo(output[pair]);
                IntermediateSources[current].Position = sources[i].Position;
                IntermediateSources[pair].Position = sources[i].Position +
                    new Vector3(0, filters[i].Height * Listener.EnvironmentSize.Y, 0);
            }
            return output;
        }
    }
}
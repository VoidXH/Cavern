﻿using System;
using System.Collections.Generic;
using System.Linq;

using Cavern.Channels;

namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// Supported types of crossovers.
    /// </summary>
    public enum CrossoverType {
        /// <summary>
        /// Crossover made of generic 2nd order highpass/lowpass filters.
        /// </summary>
        Biquad,
        /// <summary>
        /// Brickwall FIR crossover.
        /// </summary>
        Cavern,
        /// <summary>
        /// FIR realization of <see cref="Biquad"/>, without any phase distortions.
        /// </summary>
        SyntheticBiquad
    }

    /// <summary>
    /// A crossover to modify an Equalizer APO configuration file with.
    /// </summary>
    public abstract class Crossover {
        /// <summary>
        /// Extra Equalizer APO commands to be performed on crossovered channels, like adding Sealing as convolution.
        /// </summary>
        public string[] extraOperations;

        /// <summary>
        /// Crossover frequencies for each channel. Only values over 0 mean crossovered channels.
        /// </summary>
        protected float[] frequencies;

        /// <summary>
        /// Channels to route bass to. The energy will remain constant.
        /// </summary>
        protected bool[] subs;

        /// <summary>
        /// Create a crossover with frequencies for each channel. Only values over 0 mean crossovered channels.
        /// </summary>
        /// <param name="frequencies">Crossover frequencies for each channel, only values over 0 mean crossovered channels</param>
        /// <param name="subs">Channels to route bass to</param>
        public Crossover(float[] frequencies, bool[] subs) {
            this.frequencies = frequencies;
            this.subs = subs;
        }

        /// <summary>
        /// Create the appropriate type of <see cref="Crossover"/> object for the selected <paramref name="type"/>.
        /// </summary>
        /// <param name="frequencies">Crossover frequencies for each channel, only values over 0 mean crossovered channels</param>
        /// <param name="subs">Channels to route bass to</param>
        /// <param name="type">The type of crossover to use</param>
        public static Crossover Create(CrossoverType type, float[] frequencies, bool[] subs) {
            return type switch {
                CrossoverType.Biquad => new BasicCrossover(frequencies, subs),
                CrossoverType.Cavern => new CavernCrossover(frequencies, subs),
                CrossoverType.SyntheticBiquad => new SyntheticBiquadCrossover(frequencies, subs),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };
        }

        /// <summary>
        /// Attach the crossover to an Equalizer APO configuration file in the making.
        /// </summary>
        public abstract void ExportToEqualizerAPO(List<string> wipConfig);

        /// <summary>
        /// Add the filter's interpretation of highpass to the previously selected channel in a WIP configuration file.
        /// </summary>
        public virtual void AddHighpass(List<string> wipConfig, float frequency) {
            string hpf = $"Filter: ON HP Fc {frequency} Hz";
            wipConfig.Add(hpf);
            wipConfig.Add(hpf);
        }

        /// <summary>
        /// Add the filter's interpretation of lowpass to the previously selected channel in a WIP configuration file.
        /// </summary>
        /// <remarks>Don't forget to call <see cref="AddExtraOperations(List{string})"/>, this is generally the best place for it.</remarks>
        public virtual void AddLowpass(List<string> wipConfig, float frequency) {
            string lpf = $"Filter: ON LP Fc {frequency} Hz";
            wipConfig.Add(lpf);
            wipConfig.Add(lpf);
            AddExtraOperations(wipConfig);
        }

        /// <summary>
        /// Get the labels of channels to route bass to.
        /// </summary>
        public string[] GetSubLabels() {
            List<string> result = new List<string>();
            for (int i = 0; i < subs.Length; i++) {
                if (subs[i]) {
                    result.Add(EqualizerAPOUtils.GetChannelLabel(i, subs.Length));
                }
            }
            return result.ToArray();
        }

        /// <summary>
        /// For each frequency, get which channels are using it for crossover.
        /// </summary>
        protected (float frequency, string[] channelLabels)[] GetCrossoverGroups() {
            Dictionary<float, List<string>> result = new Dictionary<float, List<string>>();
            for (int i = 0; i < frequencies.Length; i++) {
                if (frequencies[i] <= 0) {
                    continue;
                }

                string label = EqualizerAPOUtils.GetChannelLabel(i, frequencies.Length);
                if (result.ContainsKey(frequencies[i])) {
                    result[frequencies[i]].Add(label);
                } else {
                    result[frequencies[i]] = new List<string> { label };
                }
            }
            return result.Select(x => (x.Key, x.Value.ToArray())).ToArray();
        }

        /// <summary>
        /// Add the <see cref="extraOperations"/> to the crossovered signal.
        /// </summary>
        protected void AddExtraOperations(List<string> wipConfig) {
            if (extraOperations != null) {
                for (int j = 0; j < extraOperations.Length; j++) {
                    wipConfig.Add(extraOperations[j]);
                }
            }
        }

        /// <summary>
        /// Use this value to mix crossover results to an LFE channel.
        /// The LFE's level is over the mains with 10 dB, this results in level matching.
        /// </summary>
        protected const float minus10dB = 0.31622776601f;
    }
}
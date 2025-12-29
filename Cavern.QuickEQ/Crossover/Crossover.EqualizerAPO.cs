using System;
using System.Collections.Generic;
using System.Globalization;

using Cavern.Channels;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Crossover {
    // Crossover export to Equalizer APO configuration files
    partial class Crossover {
        /// <summary>
        /// Extra Equalizer APO commands to be performed on crossovered channels, like adding Sealing as convolution.
        /// </summary>
        public string[] extraOperations;

        /// <summary>
        /// Append the completed crossover to a work-in-progress Equalizer APO configuration file with a -10 dB reference crossover gain.
        /// </summary>
        public virtual void ExportToEqualizerAPO(List<string> wipConfig) => ExportToEqualizerAPO(wipConfig, -10);

        /// <summary>
        /// Append the completed crossover to a work-in-progress Equalizer APO configuration file with a custom crossover <paramref name="gain"/>.
        /// </summary>
        public virtual void ExportToEqualizerAPO(List<string> wipConfig, float gain) {
            (float frequency, string[] channelLabels)[] groups = GetCrossoverGroupsWithLabels();
            string[] targets = GetSubLabels();
            string subGain = (MathF.Sqrt(1f / targets.Length) * QMath.DbToGain(gain)).ToString("0.000", CultureInfo.InvariantCulture);

            List<string> outputMix = new List<string>();
            for (int i = 0; i < groups.Length; i++) {
                if (Mixing.Mixing[i].mixHere || groups[i].frequency <= 0) {
                    continue;
                }

                wipConfig.Add($"Copy: XO{i + 1}={string.Join('+', groups[i].channelLabels)}");
                wipConfig.Add("Channel: " + string.Join(" ", groups[i].channelLabels));
                AddHighpass(wipConfig, groups[i].frequency);
                wipConfig.Add("Channel: XO" + (i + 1));
                AddLowpass(wipConfig, groups[i].frequency);
                outputMix.Add($"{subGain}*XO{i + 1}");
            }

            if (outputMix.Count > 0) {
                string mix = string.Join('+', outputMix);
                for (int i = 0; i < targets.Length; i++) {
                    wipConfig.Add($"Copy: {targets[i]}={targets[i]}+{mix}");
                }
            }
        }

        /// <summary>
        /// Add the filter's interpretation of highpass to the previously selected channel in an Equalizer APO configuration file.
        /// </summary>
        public virtual void AddHighpass(List<string> wipConfig, float frequency) {
            string hpf = $"Filter: ON HP Fc {frequency} Hz";
            wipConfig.Add(hpf);
            wipConfig.Add(hpf);
        }

        /// <summary>
        /// Add the filter's interpretation of lowpass to the previously selected channel in an Equalizer APO configuration file.
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
            for (int i = 0; i < Mixing.Channels; i++) {
                if (Mixing.Mixing[i].mixHere) {
                    result.Add(EqualizerAPOUtils.GetChannelLabel(i, Mixing.Channels));
                }
            }
            return result.ToArray();
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
        /// Get the labels of channels in each crossover group.
        /// </summary>
        (float frequency, string[] channelLabels)[] GetCrossoverGroupsWithLabels() {
            (float frequency, int[] channels)[] source = CrossoverGroups;
            (float frequency, string[] labels)[] result = new (float frequency, string[] labels)[source.Length];
            for (int i = 0; i < source.Length; i++) {
                int[] channels = source[i].channels;
                string[] labels = new string[channels.Length];
                for (int j = 0; j < channels.Length; j++) {
                    labels[j] = EqualizerAPOUtils.GetChannelLabel(channels[j], Mixing.Channels);
                }
                result[i] = (source[i].frequency, labels);
            }
            return result;
        }
    }
}

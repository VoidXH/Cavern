using System;
using System.Collections.Generic;
using System.Globalization;

namespace Cavern.QuickEQ.Crossover {
    /// <summary>
    /// A generic crossover used most of the time. This will use Equalizer APO's included lowpass/highpass filters to create the crossover,
    /// but that can be overridden to create any custom mains-to-LFE crossover function.
    /// </summary>
    public class BasicCrossover : Crossover {
        /// <summary>
        /// Create a biquad crossover with frequencies for each channel. Only values over 0 mean crossovered channels.
        /// </summary>
        /// <param name="frequencies">Crossover frequencies for each channel, only values over 0 mean crossovered channels</param>
        /// <param name="subs">Channels to route bass to</param>
        public BasicCrossover(float[] frequencies, bool[] subs) : base(frequencies, subs) { }

        /// <inheritdoc/>
        public override void ExportToEqualizerAPO(List<string> wipConfig) {
            (float frequency, string[] channelLabels)[] groups = GetCrossoverGroups();
            string[] targets = GetSubLabels();
            string subGain = (MathF.Sqrt(1f / targets.Length) * minus10dB).ToString("0.000", CultureInfo.InvariantCulture);

            List<string> outputMix = new List<string>();
            for (int i = 0; i < groups.Length; i++) {
                if (subs[i] || groups[i].frequency <= 0) {
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
    }
}
using System;
using System.Linq;

using Cavern.Utilities;

namespace Cavern.Format.FilterSet {
    // Protected functions for common parameter handling like gain
    partial class FilterSet {
        /// <summary>
        /// Get the short name of a channel written to the configuration file to select that channel for setup.
        /// </summary>
        protected virtual string GetLabel(int channel) => Channels[channel].name ?? "CH" + (channel + 1);

        /// <summary>
        /// Get the delay for a given <paramref name="channel"/> in milliseconds instead of samples.
        /// </summary>
        public double GetDelay(int channel) => Channels[channel].delaySamples * 1000.0 / SampleRate;

        /// <summary>
        /// Set the <paramref name="delay"/> in samples for a given <paramref name="channel"/>.
        /// </summary>
        public void OverrideDelay(int channel, int delay) => Channels[channel].delaySamples = delay;

        /// <summary>
        /// Get the gain of each channel in decibels, between the allowed limits of the output format.
        /// If the gains are not out of range, they will be returned as-is.
        /// </summary>
        protected double[] GetGains(double min, double max) {
            double[] result = Channels.Select(x => {
                if (x is IIRFilterSet.IIRChannelData iirData) {
                    return iirData.gain;
                } else if (x is EqualizerFilterSet.EqualizerChannelData eqData) {
                    return eqData.gain;
                } else if (x is FIRFilterSet.FIRChannelData) {
                    throw new FIRGainException();
                } else {
                    throw new NotImplementedException();
                }
            }).ToArray();
            double minFound = double.MaxValue, maxFound = double.MinValue;
            for (int i = 0; i < result.Length; i++) {
                if (minFound > result[i]) {
                    minFound = result[i];
                }
                if (maxFound < result[i]) {
                    maxFound = result[i];
                }
            }
            if (minFound >= min && maxFound <= max) {
                return result;
            }

            double avg = QMath.Average(result);
            for (int i = 0; i < result.Length; i++) {
                result[i] = Math.Clamp(result[i] - avg, min, max);
            }

            return result;
        }
    }
}

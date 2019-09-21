using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>Sound processing functions.</summary>
    public static class WaveformUtils {
        /// <summary>Downmix audio for a lesser channel count with limited knowledge of the target system's channel locations.</summary>
        public static void Downmix(float[] from, int fromChannels, float[] to, int toChannels) {
            int samplesPerChannel = to.Length / toChannels;
            for (int channel = 0, unityChannel = 0; channel < fromChannels; ++channel, unityChannel = channel % toChannels) {
                if (toChannels > 4 || (Listener.Channels[channel].Y != 0 && !Listener.Channels[channel].LFE))
                    for (int sample = 0; sample < samplesPerChannel; ++sample)
                        to[sample * toChannels + unityChannel] += from[sample * fromChannels + channel];
                else {
                    for (int sample = 0; sample < samplesPerChannel; ++sample) {
                        int leftOut = sample * toChannels;
                        float copySample = from[sample * fromChannels + channel];
                        to[leftOut] += copySample;
                        to[leftOut + 1] += copySample;
                    }
                }
            }
        }

        /// <summary>Multiplies all values in an array.</summary>
        /// <param name="target">Array reference</param>
        /// <param name="value">Multiplier</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Gain(float[] target, float value) {
            for (int i = 0; i < target.Length; ++i)
                target[i] *= value;
        }

        /// <summary>Set gain for a channel in a multichannel array.</summary>
        /// <param name="target">Sample reference</param>
        /// <param name="gain">Gain</param>
        /// <param name="channel">Target channel</param>
        /// <param name="channels">Channel count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Gain(float[] target, float gain, int channel, int channels) {
            for (int sample = channel; sample < target.Length; sample += channels)
                target[sample] *= gain;
        }

        /// <summary>Get the peak amplitude of a single-channel array.</summary>
        /// <param name="target">Array reference</param>
        /// <returns>Peak amplitude in the array in decibels</returns>
        public static float GetPeak(float[] target) {
            float max = Math.Abs(target[0]), absSample;
            for (int sample = 1; sample < target.Length; ++sample) {
                absSample = Math.Abs(target[sample]);
                if (max < absSample)
                    max = absSample;
            }
            return max != 0 ? (20 * (float)Math.Log10(max)) : -300;
        }

        /// <summary>Get the peak amplitude of a given channel in a multichannel array.</summary>
        /// <param name="target">Array reference</param>
        /// <param name="samples">Samples per channel</param>
        /// <param name="channel">Target channel</param>
        /// <param name="channels">Channel count</param>
        /// <returns>Maximum absolute value in the array</returns>
        public static float GetPeak(float[] target, int samples, int channel, int channels) {
            float max = 0, absSample;
            for (int sample = channel, end = samples * channels; sample < end; sample += channels) {
                absSample = Math.Abs(target[sample]);
                if (max < absSample)
                    max = absSample;
            }
            return max;
        }

        /// <summary>Mix a track to a stream.</summary>
        /// <param name="from">Track</param>
        /// <param name="to">Stream</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Mix(float[] from, float[] to) {
            for (int i = 0; i < from.Length; ++i)
                to[i] += from[i];
        }

        /// <summary>Normalize an array of samples.</summary>
        /// <param name="target">Samples to normalize</param>
        /// <param name="decayFactor">Gain increment per frame, should be decay rate * update rate / sample rate</param>
        /// <param name="lastGain">Last normalizer gain (a reserved float with a default of 1 to always pass to this function)</param>
        /// <param name="limiterOnly">Don't go over 0 dB gain</param>
        public static void Normalize(ref float[] target, float decayFactor, ref float lastGain, bool limiterOnly) {
            float max = Math.Abs(target[0]), absSample;
            for (int sample = 1; sample < target.Length; ++sample) {
                absSample = Math.Abs(target[sample]);
                if (max < absSample)
                    max = absSample;
            }
            if (max * lastGain > 1) // Attack
                lastGain = .9f / max;
            Gain(target, lastGain); // Normalize last samples
            // Decay
            lastGain += decayFactor;
            if (limiterOnly && lastGain > 1)
                lastGain = 1;
        }
    }
}

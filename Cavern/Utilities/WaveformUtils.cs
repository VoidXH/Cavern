using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>Sound processing functions.</summary>
    public static class WaveformUtils {
        /// <summary>Downmix audio to mono.</summary>
        /// <param name="source">Audio to downmix</param>
        /// <param name="channels">Source channel count</param>
        public static float[] Downmix(float[] source, int channels) {
            int length = source.Length / channels;
            float[] target = new float[length];
            for (int sample = 0; sample < length; ++sample)
                for (int channel = 0; channel < channels; ++channel)
                    target[sample] = source[channels * sample + channel];
            return target;
        }

        /// <summary>Downmix audio for a lesser channel count with limited knowledge of the target system's channel locations.</summary>
        public static void Downmix(float[] from, int fromChannels, float[] to, int toChannels) {
            int samplesPerChannel = to.Length / toChannels;
            for (int channel = 0; channel < fromChannels; ++channel) {
                if (toChannels > 4 || (Listener.Channels[channel].Y != 0 && !Listener.Channels[channel].LFE))
                    for (int sample = 0; sample < samplesPerChannel; ++sample)
                        to[sample * toChannels + channel % toChannels] += from[sample * fromChannels + channel];
                else {
                    for (int sample = 0; sample < samplesPerChannel; ++sample) {
                        float copySample = from[sample * fromChannels + channel];
                        to[sample * toChannels] += copySample;
                        to[sample * toChannels + 1] += copySample;
                    }
                }
            }
        }

        /// <summary>Extract a single channel from a multichannel audio stream</summary>
        /// <param name="from">Source audio stream</param>
        /// <param name="to">Destination channel data</param>
        /// <param name="channel">Target channel</param>
        /// <param name="channels">Channel count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExtractChannel(float[] from, float[] to, int channel, int channels) {
            for (int sample = 0, samples = from.Length / channels; sample < samples; ++sample)
                to[sample] = from[sample * channels + channel];
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
        /// <returns>Peak amplitude in the array</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetPeak(float[] target) {
            float max = Math.Abs(target[0]), absSample;
            for (int sample = 1; sample < target.Length; ++sample) {
                absSample = Math.Abs(target[sample]);
                if (max < absSample)
                    max = absSample;
            }
            return max;
        }

        /// <summary>Get the peak amplitude in a partial audio signal.</summary>
        /// <param name="target">Array reference</param>
        /// <param name="from">Range start sample (inclusive)</param>
        /// <param name="to">Range end sample (exclusive)</param>
        /// <returns>Peak amplitude in the given range</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetPeak(float[] target, int from, int to) {
            float max = Math.Abs(target[from++]), absSample;
            for (; from < to; ++from) {
                absSample = Math.Abs(target[from]);
                if (max < absSample)
                    max = absSample;
            }
            return max;
        }

        /// <summary>Get the peak amplitude of a given channel in a multichannel array.</summary>
        /// <param name="target">Array reference</param>
        /// <param name="samples">Samples per channel</param>
        /// <param name="channel">Target channel</param>
        /// <param name="channels">Channel count</param>
        /// <returns>Peak amplitude of the channel</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetPeak(float[] target, int samples, int channel, int channels) {
            float max = 0, absSample;
            for (int sample = channel, end = samples * channels; sample < end; sample += channels) {
                absSample = Math.Abs(target[sample]);
                if (max < absSample)
                    max = absSample;
            }
            return max;
        }

        /// <summary>Get the peak amplitude with its sign in a partial audio signal.</summary>
        /// <param name="target">Array reference</param>
        /// <param name="from">Range start sample (inclusive)</param>
        /// <param name="to">Range end sample (exclusive)</param>
        /// <returns>Peak amplitude with its sign in the given range</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetPeakSigned(float[] target, int from, int to) {
            int pos = from;
            float max = Math.Abs(target[from++]), absSample;
            for (; from < to; ++from) {
                absSample = Math.Abs(target[from]);
                if (max < absSample) {
                    max = absSample;
                    pos = from;
                }
            }
            return target[pos];
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
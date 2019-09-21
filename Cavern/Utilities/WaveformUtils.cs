using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>Sound processing functions.</summary>
    public static class WaveformUtils {
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

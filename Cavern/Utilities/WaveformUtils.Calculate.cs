using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    partial class WaveformUtils {
        /// <summary>
        /// Get the peak amplitude of a single-channel array.
        /// </summary>
        /// <param name="target">Array reference</param>
        /// <returns>Peak amplitude in the array</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetPeak(this float[] target) => GetPeak(target, 0, target.Length);

        /// <summary>
        /// Get the peak amplitude in a partial audio signal.
        /// </summary>
        /// <param name="target">Array reference</param>
        /// <param name="from">Range start sample (inclusive)</param>
        /// <param name="to">Range end sample (exclusive)</param>
        /// <returns>Peak amplitude in the given range</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetPeak(this float[] target, int from, int to) {
            float max = Math.Abs(target[from++]), absSample;
            while (from < to) {
                absSample = Math.Abs(target[from++]);
                if (max < absSample) {
                    max = absSample;
                }
            }
            return max;
        }

        /// <summary>
        /// Get the peak amplitude of a given channel in a multichannel array.
        /// </summary>
        /// <param name="target">Array reference</param>
        /// <param name="samples">Samples per channel</param>
        /// <param name="channel">Target channel</param>
        /// <param name="channels">Channel count</param>
        /// <returns>Peak amplitude of the channel</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetPeak(this float[] target, int samples, int channel, int channels) {
            float max = 0, absSample;
            for (int sample = channel, end = samples * channels; sample < end; sample += channels) {
                absSample = Math.Abs(target[sample]);
                if (max < absSample) {
                    max = absSample;
                }
            }
            return max;
        }

        /// <summary>
        /// Get the peak amplitude with its sign in a partial audio signal.
        /// </summary>
        /// <param name="target">Array reference</param>
        /// <returns>Peak amplitude with its sign</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetPeakSigned(float[] target) => GetPeakSigned(target, 0, target.Length);

        /// <summary>
        /// Get the peak amplitude with its sign in a partial audio signal.
        /// </summary>
        /// <param name="target">Array reference</param>
        /// <param name="from">Range start sample (inclusive)</param>
        /// <param name="to">Range end sample (exclusive)</param>
        /// <returns>Peak amplitude with its sign in the given range</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetPeakSigned(float[] target, int from, int to) {
            int pos = from;
            float max = Math.Abs(target[from++]),
                absSample;
            for (; from < to; from++) {
                absSample = Math.Abs(target[from]);
                if (max < absSample) {
                    max = absSample;
                    pos = from;
                }
            }
            return target[pos];
        }

        /// <summary>
        /// Get the root mean square amplitude of a single-channel signal.
        /// </summary>
        /// <param name="target">Samples of the signal</param>
        /// <returns>RMS amplitude of the signal</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetRMS(this float[] target) {
            float sum = 0;
            for (int sample = 0; sample < target.Length; sample++) {
                sum += target[sample] * target[sample];
            }
            return MathF.Sqrt(sum / target.Length);
        }

        /// <summary>
        /// Get the root mean square amplitude of a single channel in a multichannel signal.
        /// </summary>
        /// <param name="target">Samples of the signal</param>
        /// <param name="channel">The measured channel</param>
        /// <param name="channels">Total number of channels</param>
        /// <returns>RMS amplitude of the signal</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetRMS(this float[] target, int channel, int channels) {
            float sum = 0;
            for (int sample = channel; sample < target.Length; sample += channels) {
                sum += target[sample] * target[sample];
            }
            return MathF.Sqrt(sum * channels / target.Length);
        }

        /// <summary>
        /// Gets if a signal has no amplitude.
        /// </summary>
        public static bool IsMute(this float[] source) {
            for (int i = 0; i < source.Length; i++) {
                if (source[i] != 0) {
                    return false;
                }
            }
            return true;
        }
    }
}

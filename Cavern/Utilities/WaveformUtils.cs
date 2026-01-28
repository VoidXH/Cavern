using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Sound processing functions.
    /// </summary>
    public static partial class WaveformUtils {
        /// <summary>
        /// Sets all samples in a single channel of an interlaced signal to 0.
        /// </summary>
        /// <param name="signal">Interlaced signal to clear a channel of</param>
        /// <param name="channel">Channel index, but can be used as the first sample's index to clear</param>
        /// <param name="channels">Total number of channels in the <paramref name="signal"/></param>
        /// <param name="limit">Total number of samples to remove</param>
        public static unsafe void ClearChannel(float[] signal, int channel, int channels, int limit) {
            fixed (float* pSignal = signal) {
                float* pChannel = pSignal + channel;
                while (limit-- != 0) {
                    *pChannel = 0;
                    pChannel += channels;
                }
            }
        }

        /// <summary>
        /// Apply a delay of a given number of <paramref name="samples"/> on a <paramref name="waveform"/>.
        /// </summary>
        public static void Delay(float[] waveform, int samples) {
            int count = waveform.Length - samples;
            if (count > 0) {
                Array.Copy(waveform, 0, waveform, samples, count);
                Array.Clear(waveform, 0, samples);
            } else {
                Array.Clear(waveform, 0, waveform.Length);
            }
        }

        /// <summary>
        /// Apply a delay on the <paramref name="signal"/> even with fraction <paramref name="samples"/>.
        /// You could call it subsample delay precision.
        /// </summary>
        public static void Delay(float[] signal, float samples) {
            using FFTCache cache = new FFTCache(QMath.Base2Ceil(signal.Length));
            Delay(signal, samples, cache);
        }

        /// <summary>
        /// Apply a delay on the <paramref name="signal"/> even with fraction <paramref name="samples"/>.
        /// You could call it subsample delay precision.
        /// </summary>
        public static void Delay(float[] signal, float samples, FFTCache cache) {
            Complex[] fft = signal.ParseForFFT();
            fft.InPlaceFFT(cache);
            Delay(fft, samples);
            fft.InPlaceIFFT(cache);
            for (int i = 0; i < signal.Length; i++) {
                signal[i] = fft[i].Real;
            }
        }

        /// <summary>
        /// Apply a delay on the <paramref name="signal"/> even with fraction <paramref name="samples"/>.
        /// You could call it subsample delay precision.
        /// </summary>
        public static void Delay(Complex[] signal, float samples) {
            float cycle = 2 * (float)Math.PI * samples / signal.Length;
            for (int i = 1; i < signal.Length / 2; i++) {
                float phase = cycle * i;
                signal[i].Rotate(-phase);
                signal[^i].Rotate(phase);
            }
        }

        /// <summary>
        /// Multiplies all values in an array.
        /// </summary>
        /// <param name="target">Array reference</param>
        /// <param name="value">Multiplier</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Gain(float[] target, float value) {
            for (int i = 0; i < target.Length; i++) {
                target[i] *= value;
            }
        }

        /// <summary>
        /// Set gain for a channel in a multichannel array.
        /// </summary>
        /// <param name="target">Sample reference</param>
        /// <param name="addedGain">The number to multiply each element with</param>
        /// <param name="channel">Target channel</param>
        /// <param name="channels">Channel count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Gain(float[] target, float addedGain, int channel, int channels) {
            for (int sample = channel; sample < target.Length; sample += channels) {
                target[sample] *= addedGain;
            }
        }

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
        public static float GetRMS(float[] target) {
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
        public static float GetRMS(float[] target, int channel, int channels) {
            float sum = 0;
            for (int sample = channel; sample < target.Length; sample += channels) {
                sum += target[sample] * target[sample];
            }
            return MathF.Sqrt(sum * channels / target.Length);
        }

        /// <summary>
        /// Invert an audio signal.
        /// </summary>
        public static void Invert(float[] target) {
            for (int i = 0; i < target.Length; i++) {
                target[i] = -target[i];
            }
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

        /// <summary>
        /// Set a signal's peak to 1 (0 dB FS).
        /// </summary>
        /// <param name="target">Samples to normalize</param>
        public static void Normalize(this float[] target) => Gain(target, 1 / GetPeak(target));

        /// <summary>
        /// Swap two channels in an interlaced block of samples.
        /// </summary>
        /// <param name="target">Interlaced block of samples</param>
        /// <param name="channelA">First channel index</param>
        /// <param name="channelB">Second channel index</param>
        /// <param name="channels">Total channel count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void SwapChannels(this float[] target, int channelA, int channelB, int channels) {
            for (int i = 0; i < target.Length; i += channels) {
                (target[i + channelA], target[i + channelB]) = (target[i + channelB], target[i + channelA]);
            }
        }

        /// <summary>
        /// Remove the 0s from the beginning of the signal.
        /// </summary>
        public static void TrimStart(ref float[] target) {
            int trim = 0;
            while (target[trim] == 0 && trim < target.Length) {
                ++trim;
            }
            if (trim != 0) {
                target = target[trim..];
            }
        }

        /// <summary>
        /// Remove the 0s from the end of the signal.
        /// </summary>
        public static void TrimEnd(ref float[] target) {
            int trim = target.Length;
            while (trim > 0 && target[trim - 1] == 0) {
                --trim;
            }
            if (trim != target.Length) {
                target = target[..trim];
            }
        }
    }
}

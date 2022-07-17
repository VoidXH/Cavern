using Cavern.Remapping;
using System;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Sound processing functions.
    /// </summary>
    public static class WaveformUtils {
        /// <summary>
        /// Apply a delay of a given number of <paramref name="samples"/> on a <paramref name="waveform"/>.
        /// </summary>
        public static void Delay(float[] waveform, int samples) {
            Array.Copy(waveform, 0, waveform, samples, waveform.Length - samples);
            Array.Clear(waveform, 0, samples);
        }

        /// <summary>
        /// Apply a delay on the <paramref name="signal"/> even with fraction <paramref name="samples"/>.
        /// </summary>
        public static void Delay(float[] signal, float samples) {
            using FFTCache cache = new FFTCache(QMath.Base2Ceil(signal.Length));
            Delay(signal, samples);
        }

        /// <summary>
        /// Apply a delay on the <paramref name="signal"/> even with fraction <paramref name="samples"/>.
        /// </summary>
        public static void Delay(float[] signal, float samples, FFTCache cache) {
            Complex[] fft = signal.ParseForFFT();
            fft.InPlaceFFT(cache);
            Delay(signal, samples, cache);
            fft.InPlaceIFFT(cache);
            for (int i = 0; i < signal.Length; ++i) {
                signal[i] = fft[i].Real;
            }
        }

        /// <summary>
        /// Apply a delay on the <paramref name="signal"/> even with fraction <paramref name="samples"/>.
        /// </summary>
        public static void Delay(Complex[] signal, float samples) {
            float cycle = 2 * (float)Math.PI * samples / signal.Length;
            for (int i = 1; i < signal.Length / 2; ++i) {
                float phase = cycle * i;
                signal[i].Rotate(-phase);
                signal[^i].Rotate(phase);
            }
        }

        /// <summary>
        /// Downmix audio to mono.
        /// </summary>
        /// <param name="source">Audio to downmix</param>
        /// <param name="channels">Source channel count</param>
        public static float[] Downmix(float[] source, int channels) {
            int length = source.Length / channels;
            float[] target = new float[length];
            for (int sample = 0; sample < length; ++sample) {
                for (int channel = 0; channel < channels; ++channel) {
                    target[sample] = source[channels * sample + channel];
                }
            }
            return target;
        }

        /// <summary>
        /// Downmix audio for a lesser channel count with limited knowledge of the target system's channel locations.
        /// </summary>
        /// <param name="from">The output of a <see cref="Listener"/> or an audio signal
        /// that matches the renderer's channel count.</param>
        /// <param name="to">Output array</param>
        /// <param name="toChannels">Output channel count</param>
        public static void Downmix(float[] from, float[] to, int toChannels) {
            int samplesPerChannel = to.Length / toChannels,
                fromChannels = Listener.Channels.Length;
            for (int channel = 0; channel < fromChannels; ++channel) {
                if (toChannels > 4 || (Listener.Channels[channel].Y != 0 && !Listener.Channels[channel].LFE)) {
                    for (int sample = 0, overflow = channel % toChannels; sample < samplesPerChannel; ++sample) {
                        to[sample * toChannels + overflow] += from[sample * fromChannels + channel];
                    }
                } else {
                    for (int sample = 0; sample < samplesPerChannel; ++sample) {
                        float copySample = from[sample * fromChannels + channel];
                        to[sample * toChannels] += copySample;
                        to[sample * toChannels + 1] += copySample;
                    }
                }
            }

            // Quadro surrounds have the IDs of center/LFE, move them to their correct locations
            if (fromChannels == 4 && toChannels > 5) {
                int sl, sr;
                (sl, sr) = toChannels > 7 ? (6, 7) : (4, 5);
                for (int sample = 0; sample < to.Length; sample += toChannels) {
                    to[sample + sl] = to[sample + 2];
                    to[sample + sr] = to[sample + 3];
                    to[sample + 2] = 0;
                    to[sample + 3] = 0;
                }
            }
        }

        /// <summary>
        /// Extract a single channel from a multichannel audio stream.
        /// </summary>
        /// <param name="from">Source audio stream</param>
        /// <param name="to">Destination channel data</param>
        /// <param name="channel">Target channel</param>
        /// <param name="channels">Channel count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void ExtractChannel(float[] from, float[] to, int channel, int channels) {
            for (int sample = 0, samples = from.Length / channels; sample < samples; ++sample) {
                to[sample] = from[sample * channels + channel];
            }
        }

        /// <summary>
        /// Multiplies all values in an array.
        /// </summary>
        /// <param name="target">Array reference</param>
        /// <param name="value">Multiplier</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Gain(float[] target, float value) {
            for (int i = 0; i < target.Length; ++i) {
                target[i] *= value;
            }
        }

        /// <summary>
        /// Set gain for a channel in a multichannel array.
        /// </summary>
        /// <param name="target">Sample reference</param>
        /// <param name="gain">Gain</param>
        /// <param name="channel">Target channel</param>
        /// <param name="channels">Channel count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Gain(float[] target, float gain, int channel, int channels) {
            for (int sample = channel; sample < target.Length; sample += channels) {
                target[sample] *= gain;
            }
        }

        /// <summary>
        /// Get the peak amplitude of a single-channel array.
        /// </summary>
        /// <param name="target">Array reference</param>
        /// <returns>Peak amplitude in the array</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetPeak(float[] target) {
            float max = Math.Abs(target[0]), absSample;
            for (int sample = 1; sample < target.Length; ++sample) {
                absSample = Math.Abs(target[sample]);
                if (max < absSample) {
                    max = absSample;
                }
            }
            return max;
        }

        /// <summary>
        /// Get the peak amplitude in a partial audio signal.
        /// </summary>
        /// <param name="target">Array reference</param>
        /// <param name="from">Range start sample (inclusive)</param>
        /// <param name="to">Range end sample (exclusive)</param>
        /// <returns>Peak amplitude in the given range</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetPeak(float[] target, int from, int to) {
            float max = Math.Abs(target[from++]), absSample;
            for (; from < to; ++from) {
                absSample = Math.Abs(target[from]);
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
        public static float GetPeak(float[] target, int samples, int channel, int channels) {
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
        public static float GetPeakSigned(float[] target) {
            int pos = 0;
            float max = Math.Abs(target[0]), absSample;
            for (int sample = 1; sample < target.Length; ++sample) {
                absSample = Math.Abs(target[sample]);
                if (max < absSample) {
                    max = absSample;
                    pos = sample;
                }
            }
            return target[pos];
        }

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

        /// <summary>
        /// Get the root mean square amplitude of a single-channel signal.
        /// </summary>
        /// <param name="target">Array reference</param>
        /// <returns>RMS amplitude of the signal</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetRMS(float[] target) {
            float sum = 0;
            for (int sample = 0; sample < target.Length; ++sample) {
                sum += target[sample] * target[sample];
            }
            return (float)Math.Sqrt(sum / target.Length);
        }

        /// <summary>
        /// Convert an interlaced multichannel waveform to separate arrays.
        /// </summary>
        public static void InterlacedToMultichannel(float[] source, float[][] target) {
            int channels = target.Length,
                perChannel = target[0].Length;
            for (int channel = 0; channel < channels; ++channel) {
                float[] targetChannel = target[channel];
                for (long sample = 0; sample < perChannel; ++sample) {
                    targetChannel[sample] = source[channel + channels * sample];
                }
            }
        }

        /// <summary>
        /// Invert an audio signal.
        /// </summary>
        public static void Invert(float[] target) {
            for (int i = 0; i < target.Length; ++i) {
                target[i] = -target[i];
            }
        }

        /// <summary>
        /// Mix a track to a stream.
        /// </summary>
        /// <param name="source">Source track</param>
        /// <param name="destination">Destination stream</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Mix(float[] source, float[] destination) {
            for (int i = 0; i < source.Length; ++i) {
                destination[i] += source[i];
            }
        }

        /// <summary>
        /// Mix a track to a stream with a given gain.
        /// </summary>
        /// <param name="source">Source track</param>
        /// <param name="destination">Destination stream</param>
        /// <param name="gain">Linear amplification of the <paramref name="source"/> track</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Mix(float[] source, float[] destination, float gain) {
            for (int i = 0; i < source.Length; ++i) {
                destination[i] += source[i] * gain;
            }
        }

        /// <summary>
        /// Convert part of a multichannel waveform in different arrays to an interlaced waveform.
        /// </summary>
        // TODO: remove everywhere, use cached version
        public static float[] MultichannelToInterlaced(float[][] source, long from, long to) {
            float[] result = new float[source.Length * source[0].Length];
            to -= from;
            for (int channel = 0; channel < source.Length; ++channel) {
                float[] sourceChannel = source[channel];
                for (long sample = 0; sample < to; ++sample) {
                    result[sample * source.Length + channel] = sourceChannel[from + sample];
                }
            }
            return result;
        }

        /// <summary>
        /// Normalize an array of samples.
        /// </summary>
        /// <param name="target">Samples to normalize</param>
        /// <param name="decayFactor">Gain increment per frame, should be decay rate * update rate / sample rate</param>
        /// <param name="lastGain">Last normalizer gain (a reserved float with a default of 1 to
        /// always pass to this function)</param>
        /// <param name="limiterOnly">Don't go over 0 dB gain</param>
        public static void Normalize(ref float[] target, float decayFactor, ref float lastGain, bool limiterOnly) {
            float max = Math.Abs(target[0]), absSample;
            for (int sample = 1; sample < target.Length; ++sample) {
                absSample = Math.Abs(target[sample]);
                if (max < absSample) {
                    max = absSample;
                }
            }
            if (max * lastGain > 1) // Attack
                lastGain = .9f / max;
            Gain(target, lastGain); // Normalize last samples
            // Decay
            lastGain += decayFactor;
            if (limiterOnly && lastGain > 1) {
                lastGain = 1;
            }
        }
    }
}
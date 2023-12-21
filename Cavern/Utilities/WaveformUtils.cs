using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    /// <summary>
    /// Sound processing functions.
    /// </summary>
    public static class WaveformUtils {
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
        /// </summary>
        public static void Delay(float[] signal, float samples) {
            using FFTCache cache = new FFTCache(QMath.Base2Ceil(signal.Length));
            Delay(signal, samples, cache);
        }

        /// <summary>
        /// Apply a delay on the <paramref name="signal"/> even with fraction <paramref name="samples"/>.
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
        /// Downmix audio to mono.
        /// </summary>
        /// <param name="source">Audio to downmix</param>
        /// <param name="channels">Source channel count</param>
        public static float[] Downmix(this float[] source, int channels) {
            int length = source.Length / channels;
            float[] target = new float[length];
            for (int sample = 0; sample < length; sample++) {
                for (int channel = 0; channel < channels; channel++) {
                    target[sample] += source[channels * sample + channel];
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
            for (int channel = 0; channel < fromChannels; channel++) {
                if (toChannels > 4 || (Listener.Channels[channel].Y != 0 && !Listener.Channels[channel].LFE)) {
                    for (int sample = 0, overflow = channel % toChannels; sample < samplesPerChannel; sample++) {
                        to[sample * toChannels + overflow] += from[sample * fromChannels + channel];
                    }
                } else {
                    for (int sample = 0; sample < samplesPerChannel; sample++) {
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
        public static unsafe void ExtractChannel(float[] from, float[] to, int channel, int channels) {
            fixed (float* pFrom = from)
            fixed (float* pTo = to) {
                float* source = pFrom + channel,
                    destination = pTo,
                    end = pTo + Math.Min(from.Length, to.Length);
                while (destination != end) {
                    *destination++ = *source;
                    source += channels;
                }
            }
        }

        /// <summary>
        /// Extract part of a single channel from a multichannel audio stream.
        /// </summary>
        /// <param name="from">Source audio stream</param>
        /// <param name="offset">Sample offset in the <paramref name="from"/> array, across all channels</param>
        /// <param name="to">Destination channel data</param>
        /// <param name="channel">Target channel</param>
        /// <param name="channels">Channel count</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void ExtractChannel(float[] from, long offset, float[] to, int channel, int channels) {
            fixed (float* pFrom = from)
            fixed (float* pTo = to) {
                float* source = pFrom + offset + channel,
                    destination = pTo,
                    end = pTo + Math.Min(from.Length - offset, to.Length);
                while (destination != end) {
                    *destination++ = *source;
                    source += channels;
                }
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
        /// Sets a track to a stream with a set gain.
        /// </summary>
        /// <param name="source">Source track</param>
        /// <param name="destination">Destination stream</param>
        /// <param name="gain">Multiplier of signal amplitude</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Insert(float[] source, float[] destination, float gain) {
            int i = 0;
            Vector<float> mul = new Vector<float>(gain);
            for (int c = source.Length - Vector<float>.Count; i <= c; i += Vector<float>.Count) {
                (new Vector<float>(source, i) * mul).CopyTo(destination, i);
            }
            for (; i < source.Length; i++) {
                destination[i] = source[i] * gain;
            }
        }

        /// <summary>
        /// Sets a channel to a signal in a multichannel waveform.
        /// </summary>
        /// <param name="source">Samples of the given <paramref name="destinationChannel"/></param>
        /// <param name="destination">Channel array to write to</param>
        /// <param name="destinationChannel">Channel index</param>
        /// <param name="destinationChannels">Total channels</param>
        /// <remarks>It is assumed that the size of <paramref name="destination"/> equals the size of
        /// <paramref name="source"/> * <paramref name="destinationChannels"/>.</remarks>
        public static unsafe void Insert(float[] source, float[] destination, int destinationChannel, int destinationChannels) {
            fixed (float* pSource = source, pDestination = destination) {
                float* sourcePos = pSource,
                    destinationPos = pDestination + destinationChannel,
                    end = sourcePos + source.Length;
                while (sourcePos != end) {
                    *destinationPos = *sourcePos++;
                    destinationPos += destinationChannels;
                }
            }
        }

        /// <summary>
        /// Sets a channel to a signal in a multichannel waveform.
        /// </summary>
        /// <param name="source">Samples of the given <paramref name="destinationChannel"/></param>
        /// <param name="sourceChannel">Source channel index, but can be used as a sample offset (all channels count)</param>
        /// <param name="sourceChannels">Total channels in the <paramref name="source"/></param>
        /// <param name="destination">Channel array to write to</param>
        /// <param name="destinationChannel">Destination channel index, but can be used as a sample offset (all channels count)</param>
        /// <param name="destinationChannels">Total channels in the <paramref name="destination"/></param>
        /// <param name="count">Number of samples of the channel to copy</param>
        /// <remarks>It is assumed that the size of <paramref name="destination"/> equals the size of
        /// <paramref name="source"/> * <paramref name="destinationChannels"/>.</remarks>
        public static unsafe void Insert(float[] source, int sourceChannel, int sourceChannels,
            float[] destination, int destinationChannel, int destinationChannels, int count) {
            fixed (float* pSource = source, pDestination = destination) {
                float* sourcePos = pSource + sourceChannel,
                    destinationPos = pDestination + destinationChannel;
                while (count-- != 0) {
                    *destinationPos = *sourcePos;
                    destinationPos += destinationChannels;
                    sourcePos += sourceChannels;
                }
            }
        }

        /// <summary>
        /// Convert an interlaced multichannel waveform to separate arrays.
        /// </summary>
        public static void InterlacedToMultichannel(float[] source, float[][] target) {
            int channels = target.Length,
                perChannel = target[0].Length;
            for (int channel = 0; channel < channels; channel++) {
                float[] targetChannel = target[channel];
                for (long sample = 0; sample < perChannel; sample++) {
                    targetChannel[sample] = source[channel + channels * sample];
                }
            }
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
        /// Mix a track to a mono stream.
        /// </summary>
        /// <param name="source">Source track</param>
        /// <param name="destination">Mono destination stream</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Mix(float[] source, float[] destination) {
            int i = 0;
            for (int c = source.Length - Vector<float>.Count; i <= c; i += Vector<float>.Count) {
                (new Vector<float>(source, i) + new Vector<float>(destination, i)).CopyTo(destination, i);
            }
            for (; i < source.Length; i++) {
                destination[i] += source[i];
            }
        }

        /// <summary>
        /// Mix a partial track to a mono stream. The mixing starts at a sample offset, and lasts until the stream.
        /// </summary>
        /// <param name="source">Source track</param>
        /// <param name="sourceOffset">Start the source from this sample</param>
        /// <param name="destination">Mono destination stream</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Mix(float[] source, int sourceOffset, float[] destination) {
            int i = 0;
            for (int c = destination.Length - Vector<float>.Count; i <= c; i += Vector<float>.Count) {
                (new Vector<float>(source, i + sourceOffset) + new Vector<float>(destination, i)).CopyTo(destination, i);
            }
            for (; i < destination.Length; i++) {
                destination[i] += source[i + sourceOffset];
            }
        }

        /// <summary>
        /// Mix a track to a mono stream with a given gain.
        /// </summary>
        /// <param name="source">Source track</param>
        /// <param name="destination">Mono destination stream</param>
        /// <param name="gain">Linear amplification of the <paramref name="source"/> track</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Mix(float[] source, float[] destination, float gain) {
            int i = 0;
            Vector<float> mul = new Vector<float>(gain);
            for (int c = source.Length - Vector<float>.Count; i <= c; i += Vector<float>.Count) {
                (new Vector<float>(destination, i) + new Vector<float>(source, i) * mul).CopyTo(destination, i);
            }
            for (; i < source.Length; i++) {
                destination[i] += source[i] * gain;
            }
        }

        /// <summary>
        /// Mix a track to a stream' given channel with a given gain.
        /// </summary>
        /// <param name="source">Source track</param>
        /// <param name="destination">Interlaced destination stream</param>
        /// <param name="destinationChannel">Channel of the <paramref name="destination"/></param>
        /// <param name="destinationChannels">Number of channels in the <paramref name="destination"/></param>
        /// <param name="gain">Linear amplification of the <paramref name="source"/> track</param>
        /// <remarks>It is assumed that the size of <paramref name="destination"/> equals the size of
        /// <paramref name="source"/> * <paramref name="destinationChannels"/>.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void Mix(float[] source, float[] destination, int destinationChannel, int destinationChannels, float gain) {
            fixed (float* pSource = source, pDestination = destination) {
                float* sourcePos = pSource,
                    destinationPos = pDestination + destinationChannel,
                    end = sourcePos + source.Length;
                while (sourcePos != end) {
                    *destinationPos += *sourcePos++ * gain;
                    destinationPos += destinationChannels;
                }
            }
        }

        /// <summary>
        /// Mix a channel of a stream to one of its other track.
        /// </summary>
        /// <param name="source">Source track</param>
        /// <param name="sourceChannel">Channel to copy to the <paramref name="destinationChannel"/></param>
        /// <param name="destinationChannel">Channel to mix the <paramref name="sourceChannel"/> to</param>
        /// <param name="channels">Number of channels in the <paramref name="source"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Mix(float[] source, int sourceChannel, int destinationChannel, int channels) {
            for (int i = 0; i < source.Length; i += channels) {
                source[i + destinationChannel] += source[i + sourceChannel];
            }
        }

        /// <summary>
        /// Mix a channel of a stream to one of its other track.
        /// </summary>
        /// <param name="source">Source track</param>
        /// <param name="sourceChannel">Channel to copy to the <paramref name="destinationChannel"/></param>
        /// <param name="destinationChannel">Channel to mix the <paramref name="sourceChannel"/> to</param>
        /// <param name="channels">Number of channels in the <paramref name="source"/></param>
        /// <param name="gain">Signal amplitude multiplier</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Mix(float[] source, int sourceChannel, int destinationChannel, int channels, float gain) {
            for (int i = 0; i < source.Length; i += channels) {
                source[i + destinationChannel] += source[i + sourceChannel] * gain;
            }
        }

        /// <summary>
        /// Convert part of a multichannel waveform in different arrays to an interlaced waveform.
        /// </summary>
        public static float[] MultichannelToInterlaced(float[][] source, long from, long to) {
            float[] result = new float[source.Length * source[0].Length];
            to -= from;
            for (int channel = 0; channel < source.Length; channel++) {
                float[] sourceChannel = source[channel];
                for (long sample = 0; sample < to; sample++) {
                    result[sample * source.Length + channel] = sourceChannel[from + sample];
                }
            }
            return result;
        }

        /// <summary>
        /// Convert part of a multichannel waveform in different arrays to an interlaced waveform.
        /// </summary>
        public static void MultichannelToInterlaced(float[][] source, long from, long to, float[] target, long offset) {
            to -= from;
            for (int channel = 0; channel < source.Length; channel++) {
                long localOffset = channel + offset;
                float[] sourceChannel = source[channel];
                for (long sample = 0; sample < to; sample++) {
                    target[sample * source.LongLength + localOffset] = sourceChannel[from + sample];
                }
            }
        }

        /// <summary>
        /// Set a signal's peak to 1 (0 dB FS).
        /// </summary>
        /// <param name="target">Samples to normalize</param>
        public static void Normalize(this float[] target) => Gain(target, 1 / GetPeak(target));

        /// <summary>
        /// Subtract a track from a stream.
        /// </summary>
        /// <param name="source">Track to subtract</param>
        /// <param name="destination">Track to subtract from</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Subtract(this float[] source, float[] destination) {
            for (int i = 0; i < source.Length; i++) {
                destination[i] -= source[i];
            }
        }

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
using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Cavern.Utilities {
    static partial class WaveformUtils {
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
    }
}

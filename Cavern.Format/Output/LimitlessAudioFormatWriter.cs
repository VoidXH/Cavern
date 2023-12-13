using System;
using System.IO;
using System.Runtime.CompilerServices;

using Cavern.Format.Common;
using Cavern.Format.Consts;
using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format {
    /// <summary>
    /// Minimal Limitless Audio Format file writer.
    /// </summary>
    public class LimitlessAudioFormatWriter : AudioWriter {
        /// <summary>
        /// Output channel information.
        /// </summary>
        readonly Channel[] channels;

        /// <summary>
        /// The past second for each channel.
        /// </summary>
        readonly float[] cache;

        /// <summary>
        /// Write position in the <see cref="cache"/>. Used to check if the cache is full for block dumping.
        /// </summary>
        int cachePosition;

        /// <summary>
        /// Total samples written in the file so far. Used to check the end of file and dump the unfilled last block.
        /// </summary>
        long totalWritten;

        /// <summary>
        /// Minimal Limitless Audio Format file writer.
        /// </summary>
        /// <param name="writer">File writer object</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        /// <param name="channels">Output channel information</param>
        public LimitlessAudioFormatWriter(Stream writer, long length, int sampleRate, BitDepth bits, Channel[] channels) :
            base(writer, channels.Length, length, sampleRate, bits) {
            this.channels = channels;
            cache = new float[channels.Length * sampleRate];
        }

        /// <summary>
        /// Minimal Limitless Audio Format file writer.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        /// <param name="channels">Output channel information</param>
        public LimitlessAudioFormatWriter(string path, long length, int sampleRate, BitDepth bits, Channel[] channels) :
            base(path, channels.Length, length, sampleRate, bits) {
            this.channels = channels;
            cache = new float[channels.Length * sampleRate];
        }

        /// <summary>
        /// Export an array of samples to an audio file.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="data">Samples to write in the file</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        /// <param name="channels">Output channel information</param>
        public static void Write(string path, float[] data, int sampleRate, BitDepth bits, Channel[] channels) {
            LimitlessAudioFormatWriter writer =
                new LimitlessAudioFormatWriter(path, data.Length / channels.Length, sampleRate, bits, channels);
            writer.Write(data);
        }

        /// <summary>
        /// Export an array of multichannel samples to an audio file.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="data">Samples to write in the file</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        /// <param name="channels">Output channel information</param>
        public static void Write(string path, float[][] data, int sampleRate, BitDepth bits, Channel[] channels) {
            LimitlessAudioFormatWriter writer = new LimitlessAudioFormatWriter(path, data[0].LongLength, sampleRate, bits, channels);
            writer.Write(data);
        }

        /// <summary>
        /// Export an audio file to be played back channel after channel.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="data">Samples to write in the file</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        /// <param name="channels">Output channel information</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteOffset(string path, float[][] data, int sampleRate, BitDepth bits, Channel[] channels) =>
            WriteOffset(path, data, sampleRate, bits, channels, channels.Length);

        /// <summary>
        /// Export an audio file to be played back channel after channel, but some channels play simultaneously.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="data">Samples to write in the file</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        /// <param name="channels">Output channel information</param>
        /// <param name="period">Channels separated by this many channels are played simultaneously</param>
        public static void WriteOffset(string path, float[][] data, int sampleRate, BitDepth bits, Channel[] channels, int period) {
            LimitlessAudioFormatWriter writer = new LimitlessAudioFormatWriter(path, data[0].LongLength, sampleRate, bits, channels);
            writer.WriteOffset(data, period);
        }

        /// <summary>
        /// Create the file header.
        /// </summary>
        public override void WriteHeader() => WriteHeader(false, ChannelCount);

        /// <summary>
        /// Create the file header.
        /// </summary>
        internal void WriteHeader(bool objectMode, int objectCount) {
            writer.Write(LimitlessAudioFormat.limitless); // Limitless marker
            // No custom headers
            writer.Write(LimitlessAudioFormat.head); // Main header marker
            byte qualityByte = Bits switch {
                BitDepth.Int8 => (byte)LAFMode.Int8,
                BitDepth.Int16 => (byte)LAFMode.Int16,
                BitDepth.Int24 => (byte)LAFMode.Int24,
                BitDepth.Float32 => (byte)LAFMode.Float32,
                _ => throw new IOException($"Unsupported bit depth: {Bits}.")
            };
            writer.WriteAny(qualityByte);
            writer.WriteAny(objectMode ? (byte)1 : (byte)0); // Mode
            writer.WriteAny(ChannelCount); // Channel/object count
            for (int channel = 0; channel < objectCount; channel++) { // Audible channel/object info
                writer.WriteAny(channels[channel].X); // Rotation on vertical axis
                writer.WriteAny(channels[channel].Y); // Rotation on horizontal axis
                writer.WriteAny(channels[channel].LFE ? (byte)1 : (byte)0); // Low frequency
            }
            for (int channel = objectCount; channel < ChannelCount; channel++) { // Positional track markers
                writer.WriteAny(float.NaN); // Marker
                writer.WriteAny(0); // Reserved
                writer.WriteAny((byte)0); // Reserved
            }
            writer.Write(BitConverter.GetBytes(SampleRate));
            writer.Write(BitConverter.GetBytes(Length));
        }

        /// <summary>
        /// Write a block of samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public override void WriteBlock(float[] samples, long from, long to) {
            long dumpLength = to - from;
            while (from < to) {
                for (; from < to && cachePosition < cache.Length; from++) {
                    cache[cachePosition++] = samples[from];
                }
                if (cachePosition == cache.Length) {
                    DumpBlock(cache.LongLength);
                }
            }
            if ((totalWritten += dumpLength) == Length * ChannelCount) {
                DumpBlock(cachePosition);
            }
        }

        /// <summary>
        /// Write a block of multichannel samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public override void WriteBlock(float[][] samples, long from, long to) {
            long dumpLength = to - from;
            while (from < to) {
                long samplesToWrite = Math.Min((to - from) * samples.LongLength, cache.Length - cachePosition);
                WaveformUtils.MultichannelToInterlaced(samples, from, from += samplesToWrite / samples.LongLength, cache, cachePosition);
                cachePosition += (int)samplesToWrite;
                if (cachePosition == cache.Length) {
                    DumpBlock(cache.LongLength);
                }
            }
            if ((totalWritten += dumpLength) == Length) {
                DumpBlock(cachePosition);
            }
        }

        /// <summary>
        /// Output only the used channels from the last second.
        /// </summary>
        /// <param name="until">Samples to dump from the <see cref="cache"/></param>
        void DumpBlock(long until) {
            bool[] toWrite = new bool[ChannelCount];
            for (int channel = 0; channel < ChannelCount; channel++) {
                for (int sample = channel; !toWrite[channel] && sample < cache.Length; sample += ChannelCount) {
                    if (cache[sample] != 0) {
                        toWrite[channel] = true;
                    }
                }
            }
            byte[] layoutBytes = new byte[(ChannelCount & 7) == 0 ? ChannelCount >> 3 : ((ChannelCount >> 3) + 1)];
            for (int channel = 0; channel < ChannelCount; channel++) {
                if (toWrite[channel]) {
                    layoutBytes[channel >> 3] += (byte)(1 << (channel & 7));
                }
            }
            writer.Write(layoutBytes);
            switch (Bits) {
                case BitDepth.Int8:
                    for (int sample = 0; sample < until; ++sample) {
                        if (toWrite[sample % ChannelCount]) {
                            writer.WriteAny((sbyte)(cache[sample] * sbyte.MaxValue));
                        }
                    }
                    break;
                case BitDepth.Int16:
                    for (int sample = 0; sample < until; ++sample) {
                        if (toWrite[sample % ChannelCount]) {
                            writer.WriteAny((short)(cache[sample] * short.MaxValue));
                        }
                    }
                    break;
                case BitDepth.Int24:
                    for (int sample = 0; sample < until; ++sample) {
                        if (toWrite[sample % ChannelCount]) {
                            int src = (int)(cache[sample] * BitConversions.int24Max);
                            writer.WriteAny((byte)src);
                            writer.WriteAny((byte)(src >> 8));
                            writer.WriteAny((byte)(src >> 16));
                        }
                    }
                    break;
                case BitDepth.Float32:
                    for (int sample = 0; sample < until; ++sample) {
                        if (toWrite[sample % ChannelCount]) {
                            writer.Write(BitConverter.GetBytes(cache[sample]));
                        }
                    }
                    break;
                default:
                    throw new InvalidBitDepthException(Bits);
            }
            cachePosition = 0;
        }
    }
}
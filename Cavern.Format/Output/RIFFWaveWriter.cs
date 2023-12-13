using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

using Cavern.Channels;
using Cavern.Format.Common;
using Cavern.Format.Consts;
using Cavern.Format.Utilities;
using Cavern.Utilities;

using static Cavern.Format.Consts.RIFFWave;

namespace Cavern.Format {
    /// <summary>
    /// Minimal RIFF wave file writer.
    /// </summary>
    public class RIFFWaveWriter : AudioWriter {
        /// <summary>
        /// The maximum number of additionally needed chunks that could surpass 4 GB.
        /// </summary>
        public int MaxLargeChunks { get; set; }

        /// <summary>
        /// Channels present in the file, see <see cref="WaveExtensibleChannel"/>.
        /// </summary>
        readonly int channelMask = -1;

        /// <summary>
        /// Sizes of chunks larger than 4 GB.
        /// </summary>
        List<Tuple<int, long>> largeChunkSizes;

        /// <summary>
        /// Bytes used for the actual PCM data.
        /// </summary>
        long DataLength => Length * ChannelCount * ((int)Bits / 8);

        /// <summary>
        /// Total samples written.
        /// </summary>
        long samplesWritten;

        /// <summary>
        /// Minimal RIFF Wave file writer.
        /// </summary>
        /// <param name="writer">File writer object</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public RIFFWaveWriter(Stream writer, int channelCount, long length, int sampleRate, BitDepth bits) :
            base(writer, channelCount, length, sampleRate, bits) { }

        /// <summary>
        /// Minimal RIFF Wave file writer.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public RIFFWaveWriter(string path, int channelCount, long length, int sampleRate, BitDepth bits) :
            base(path, channelCount, length, sampleRate, bits) { }

        /// <summary>
        /// WAVEFORMATEXTENSIBLE file writer.
        /// </summary>
        /// <param name="writer">File writer object</param>
        /// <param name="channels">Output channel mapping</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public RIFFWaveWriter(Stream writer, ReferenceChannel[] channels, long length, int sampleRate, BitDepth bits) :
            base(writer, channels.Length, length, sampleRate, bits) => channelMask = CreateChannelMask(channels);

        /// <summary>
        /// WAVEFORMATEXTENSIBLE file writer.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="channels">Output channel mapping</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public RIFFWaveWriter(string path, ReferenceChannel[] channels, long length, int sampleRate, BitDepth bits) :
            base(path, channels.Length, length, sampleRate, bits) => channelMask = CreateChannelMask(channels);

        /// <summary>
        /// Export an array of samples to an audio file.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="data">Samples to write in the file</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public static void Write(string path, float[] data, int channelCount, int sampleRate, BitDepth bits) {
            RIFFWaveWriter writer = new RIFFWaveWriter(path, channelCount, data.LongLength / channelCount, sampleRate, bits);
            writer.Write(data);
        }

        /// <summary>
        /// Export an array of multichannel samples to an audio file.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="data">Samples to write in the file</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public static void Write(string path, float[][] data, int sampleRate, BitDepth bits) {
            RIFFWaveWriter writer = new RIFFWaveWriter(path, data.Length, data[0].LongLength, sampleRate, bits);
            writer.Write(data);
        }

        /// <summary>
        /// Export an audio file to be played back channel after channel.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="data">Samples to write in the file</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WriteOffset(string path, float[][] data, int sampleRate, BitDepth bits) =>
            WriteOffset(path, data, sampleRate, bits, data.Length);

        /// <summary>
        /// Export an audio file to be played back channel after channel, but some channels play simultaneously.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="data">Samples to write in the file</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        /// <param name="period">Channels separated by this many channels are played simultaneously</param>
        public static void WriteOffset(string path, float[][] data, int sampleRate, BitDepth bits, int period) {
            RIFFWaveWriter writer = new RIFFWaveWriter(path, data.Length, data[0].LongLength, sampleRate, bits);
            writer.WriteOffset(data, period);
        }

        /// <summary>
        /// Export an audio file to be played back channel after channel.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="data">Samples to write in the file</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public static void WriteForEachChannel(string path, float[] data, int channelCount, int sampleRate, BitDepth bits) {
            RIFFWaveWriter writer = new RIFFWaveWriter(path, channelCount, data.LongLength / channelCount, sampleRate, bits);
            writer.WriteForEachChannel(data, channelCount);
            writer.Dispose();
        }

        /// <summary>
        /// Create the file header.
        /// </summary>
        public override void WriteHeader() {
            // RIFF header with file length
            writer.WriteAny(syncWord1);
            uint fmtContentLength = channelMask == -1 ? fmtContentSize : waveformatextensibleSize;
            uint fmtSize = (byte)(fmtJunkSize + fmtContentLength);
            bool inConstraints = fmtSize + DataLength < uint.MaxValue;
            if (inConstraints && MaxLargeChunks == 0) {
                writer.WriteAny(fmtSize + (uint)DataLength);
                writer.WriteAny(syncWord2);
            } else {
                writer.WriteAny(inConstraints ? fmtSize + (uint)DataLength : 0xFFFFFFFF);
                writer.WriteAny(syncWord2);
                if (!inConstraints || MaxLargeChunks != 0) {
                    writer.WriteAny(junkSync);
                    int junkLength = junkBaseSize + MaxLargeChunks * junkExtraSize;
                    writer.WriteAny(junkLength);
                    writer.Write(new byte[junkLength]);
                }
            }

            // Format header
            writer.WriteAny(formatSync);
            writer.WriteAny(fmtContentLength);
            if (channelMask == -1) {
                writer.WriteAny(Bits == BitDepth.Float32 ? (short)3 : (short)1); // Sample format...
            } else {
                writer.WriteAny(extensibleSampleFormat); // ...or that it will be in an extended header
            }
            writer.WriteAny((short)ChannelCount); // Audio channels
            writer.WriteAny(SampleRate); // Sample rate
            int blockAlign = ChannelCount * ((int)Bits / 8), BPS = SampleRate * blockAlign;
            writer.WriteAny(BPS); // Bytes per second
            writer.WriteAny((short)blockAlign); // Block size in bytes
            writer.WriteAny((short)Bits); // Bit depth

            if (channelMask != -1) { // Use WAVEFORMATEXTENSIBLE
                writer.WriteAny((short)22); // Extensible header size - 1
                writer.WriteAny((short)Bits); // Bit depth
                writer.WriteAny(channelMask); // Channel mask
                writer.WriteAny(Bits == BitDepth.Float32 ? (short)3 : (short)1); // Sample format
                writer.Write(new byte[] { 0, 0, 0, 0, 16, 0, 128, 0, 0, 170, 0, 56, 155, 113 }); // SubFormat GUID
            }

            // Data header
            writer.WriteAny(dataSync);
            writer.Write(BitConverter.GetBytes(DataLength > uint.MaxValue ? 0xFFFFFFFF : (uint)DataLength));
        }

        /// <summary>
        /// Write a block of samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public override void WriteBlock(float[] samples, long from, long to) {
            const long skip = FormatConsts.blockSize / sizeof(float); // Source split optimization for both memory and IO
            if (to - from > skip) {
                long actualSkip = skip - skip % ChannelCount; // Blocks have to be divisible with the channel count
                for (; from < to; from += actualSkip) {
                    WriteBlock(samples, from, Math.Min(to, from + actualSkip));
                }
                return;
            }

            // Don't allow overwriting
            int window = (int)(to - from);
            samplesWritten += window / ChannelCount;
            if (samplesWritten > Length) {
                long extra = samplesWritten - Length;
                window -= (int)(extra * ChannelCount);
                samplesWritten -= extra;
            }

            byte[] output = new byte[window * ((long)Bits >> 3)];
            switch (Bits) {
                case BitDepth.Int8:
                    for (int i = 0; i < window; i++) {
                        output[i] = (byte)(samples[from++] * sbyte.MaxValue);
                    }
                    break;
                case BitDepth.Int16:
                    for (int i = 0; i < window; i++) {
                        short val = (short)(samples[from++] * short.MaxValue);
                        output[2 * i] = (byte)val;
                        output[2 * i + 1] = (byte)(val >> 8);
                    }
                    break;
                case BitDepth.Int24:
                    for (int i = 0; i < window; i++) {
                        QMath.ConverterStruct val = new QMath.ConverterStruct { asInt = (int)((double)samples[from++] * int.MaxValue) };
                        output[3 * i] = val.byte1;
                        output[3 * i + 1] = val.byte2;
                        output[3 * i + 2] = val.byte3;
                    }
                    break;
                case BitDepth.Float32:
                    for (int i = 0; i < window; i++) {
                        QMath.ConverterStruct val = new QMath.ConverterStruct { asFloat = samples[from++] };
                        output[4 * i] = val.byte0;
                        output[4 * i + 1] = val.byte1;
                        output[4 * i + 2] = val.byte2;
                        output[4 * i + 3] = val.byte3;
                    }
                    break;
                default:
                    throw new InvalidBitDepthException(Bits);
            }
            writer.Write(output);
        }

        /// <summary>
        /// Write a block of samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array for a single channel (inclusive)</param>
        /// <param name="to">End position in the input array for a single channel (exclusive)</param>
        public override void WriteBlock(float[][] samples, long from, long to) {
            // Don't allow overwriting
            samplesWritten += to - from;
            if (samplesWritten > Length) {
                long extra = samplesWritten - Length;
                to -= extra;
                samplesWritten -= extra;
            }

            switch (Bits) {
                case BitDepth.Int8:
                    while (from < to) {
                        for (int channel = 0; channel < samples.Length; channel++) {
                            writer.WriteAny((sbyte)(samples[channel][from] * sbyte.MaxValue));
                        }
                        ++from;
                    }
                    break;
                case BitDepth.Int16:
                    while (from < to) {
                        for (int channel = 0; channel < samples.Length; channel++) {
                            writer.WriteAny((short)(samples[channel][from] * short.MaxValue));
                        }
                        ++from;
                    }
                    break;
                case BitDepth.Int24:
                    while (from < to) {
                        for (int channel = 0; channel < samples.Length; channel++) {
                            int src = (int)(samples[channel][from] * BitConversions.int24Max);
                            writer.WriteAny((short)src);
                            writer.WriteAny((byte)(src >> 16));
                        }
                        ++from;
                    }
                    break;
                case BitDepth.Float32:
                    while (from < to) {
                        for (int channel = 0; channel < samples.Length; channel++) {
                            writer.WriteAny(samples[channel][from]);
                        }
                        ++from;
                    }
                    break;
            }
        }

        /// <summary>
        /// Append an extra chunk to the file, without considering its alignment to even bytes.
        /// </summary>
        /// <param name="id">4 byte identifier of the chunk</param>
        /// <param name="data">Raw data of the chunk</param>
        /// <remarks>The <paramref name="id"/> has a different byte order in the file to memory,
        /// refer to <see cref="RIFFWave"/> for samples.</remarks>
        public void WriteChunk(int id, byte[] data) => WriteChunk(id, data, false);

        /// <summary>
        /// Append an extra chunk to the file.
        /// </summary>
        /// <param name="id">4 byte identifier of the chunk</param>
        /// <param name="data">Raw data of the chunk</param>
        /// <param name="dwordPadded">Some RIFF readers only work if all chunks start at an even byte, this is for their support</param>
        /// <remarks>The <paramref name="id"/> has a different byte order in the file to memory,
        /// refer to <see cref="RIFFWave"/> for samples.</remarks>
        public void WriteChunk(int id, byte[] data, bool dwordPadded) {
            writer.WriteAny(id);
            if (data.LongLength > uint.MaxValue) {
                largeChunkSizes ??= new List<Tuple<int, long>>();
                largeChunkSizes.Add(new Tuple<int, long>(id, data.LongLength));
                writer.WriteAny(0xFFFFFFFF);
            } else {
                writer.WriteAny(data.Length);
            }
            writer.Write(data);
            if (dwordPadded && (writer.Position & 1) == 1) {
                writer.WriteAny((byte)0);
            }
        }

        /// <summary>
        /// Update 64-bit size information when needed before closing the file.
        /// </summary>
        public override void Dispose() {
            if (writer == null || !writer.CanWrite) {
                return; // Already disposed
            }

            // Handle when a truncated signal was passed to the writer, fill the rest up with zero bytes
            if (samplesWritten < Length) {
                long zerosToWrite = (Length - samplesWritten) * ChannelCount * (long)Bits;
                while (zerosToWrite > 8) {
                    writer.WriteAny(0L);
                    zerosToWrite -= 8;
                }
                while (zerosToWrite-- > 0) {
                    writer.WriteAny(0L);
                }
            }

            long contentSize = writer.Position - 8;
            if (contentSize > uint.MaxValue || MaxLargeChunks != 0) {
                int largeChunks = 0;
                if (largeChunkSizes != null) {
                    largeChunks = largeChunkSizes.Count;
                }

                // 64-bit sync word
                writer.Position = 0;
                writer.WriteAny(syncWord1_64);
                writer.WriteAny(0xFFFFFFFF);
                writer.Position += 4;

                // 64-bit format header
                writer.WriteAny(ds64Sync);
                writer.WriteAny(junkBaseSize + largeChunks * junkExtraSize);

                // Mandatory sizes
                writer.WriteAny(contentSize);
                writer.WriteAny(DataLength);
                writer.WriteAny(Length);

                // Large chunk sizes
                writer.WriteAny(largeChunks);
                if (largeChunkSizes != null) {
                    for (int i = 0; i < largeChunks; ++i) {
                        writer.WriteAny(largeChunkSizes[i].Item1);
                        writer.WriteAny(largeChunkSizes[i].Item2);
                    }
                }

                // Fill the unused space with junk
                int emptyBytes = (MaxLargeChunks - largeChunks) * junkExtraSize;
                if (emptyBytes != 0) {
                    writer.WriteAny(junkSync);
                    writer.WriteAny(emptyBytes - 8);
                }
            }
            base.Dispose();
        }

        /// <summary>
        /// Additional gross bytes in the format header, its total size is this + <see cref="fmtContentSize"/>.
        /// </summary>
        const byte fmtJunkSize = 20;

        /// <summary>
        /// Bytes of metadata in the format header.
        /// </summary>
        const byte fmtContentSize = 16;

        /// <summary>
        /// The format header metadata length if it includes additional information.
        /// </summary>
        const byte waveformatextensibleSize = fmtContentSize + 24;

        /// <summary>
        /// Minimum size of the temporary header that could be replaced with a size header.
        /// </summary>
        const byte junkBaseSize = 28;

        /// <summary>
        /// Size for one extra header information in the temporary header.
        /// </summary>
        const byte junkExtraSize = 12;

        /// <summary>
        /// Sample format identifier of WAVEFORMATEXTENSIBLE.
        /// </summary>
        const short extensibleSampleFormat = -2;
    }
}
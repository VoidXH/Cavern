using System;
using System.Collections.Generic;
using System.IO;

using Cavern.Format.Consts;

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
        /// Sizes of chunks larger than 4 GB.
        /// </summary>
        List<Tuple<int, long>> largeChunkSizes;

        /// <summary>
        /// Bytes used for the actual PCM data.
        /// </summary>
        long DataLength => Length * ChannelCount * ((int)Bits / 8);

        /// <summary>
        /// Minimal RIFF wave file writer.
        /// </summary>
        /// <param name="writer">File writer object</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public RIFFWaveWriter(BinaryWriter writer, int channelCount, long length, int sampleRate, BitDepth bits) :
            base(writer, channelCount, length, sampleRate, bits) {}

        /// <summary>
        /// Minimal RIFF wave file writer.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public RIFFWaveWriter(string path, int channelCount, long length, int sampleRate, BitDepth bits) :
            base(path, channelCount, length, sampleRate, bits) { }

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
            writer.Dispose();
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
            writer.Dispose();
        }

        /// <summary>
        /// Export an audio file to be played back channel after channel.
        /// </summary>
        /// <param name="path">Output file name</param>
        /// <param name="data">Samples to write in the file</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        /// <param name="period">Channels separated by this many channels are played simultaneously</param>
        public static void WriteOffset(string path, float[][] data, int sampleRate, BitDepth bits, int period = -1) {
            RIFFWaveWriter writer = new RIFFWaveWriter(path, data.Length, data[0].LongLength, sampleRate, bits);
            writer.WriteOffset(data, period);
            writer.Dispose();
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
            writer.Write(RIFFWave.syncWord1);
            bool inConstraints = fmtSize + (uint)DataLength < uint.MaxValue;
            if (inConstraints && MaxLargeChunks == 0) {
                writer.Write(fmtSize + (uint)DataLength);
                writer.Write(RIFFWave.syncWord2);
            } else {
                writer.Write(inConstraints ? fmtSize + (uint)DataLength : 0xFFFFFFFF);
                writer.Write(RIFFWave.syncWord2);
                if (!inConstraints || MaxLargeChunks != 0) {
                    writer.Write(RIFFWave.junkSync);
                    int junkLength = junkBaseSize + MaxLargeChunks * junkExtraSize;
                    writer.Write(junkLength);
                    writer.Write(new byte[junkLength]);
                }
            }

            // Format header
            writer.Write(RIFFWave.formatSync);
            writer.Write(new byte[] { 16, 0, 0, 0 }); // FMT header size
            writer.Write(new byte[] { Bits == BitDepth.Float32 ? (byte)3 : (byte)1, 0 }); // Sample format
            writer.Write(BitConverter.GetBytes((short)ChannelCount)); // Audio channels
            writer.Write(BitConverter.GetBytes(SampleRate)); // Sample rate
            int blockAlign = ChannelCount * ((int)Bits / 8), BPS = SampleRate * blockAlign;
            writer.Write(BitConverter.GetBytes(BPS)); // Bytes per second
            writer.Write(BitConverter.GetBytes((short)blockAlign)); // Block size in bytes
            writer.Write(BitConverter.GetBytes((short)Bits)); // Bit depth

            // Data header
            writer.Write(RIFFWave.dataSync);
            writer.Write(BitConverter.GetBytes((uint)DataLength));
        }

        /// <summary>
        /// Write a block of samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public override void WriteBlock(float[] samples, long from, long to) {
            switch (Bits) {
                case BitDepth.Int8:
                    while (from < to)
                        writer.Write((sbyte)(samples[from++] * sbyte.MaxValue));
                    break;
                case BitDepth.Int16:
                    while (from < to)
                        writer.Write((short)(samples[from++] * short.MaxValue));
                    break;
                case BitDepth.Int24:
                    while (from < to) {
                        int src = (int)(samples[from++] * 8388607f);
                        writer.Write((byte)src);
                        writer.Write((byte)(src >> 8));
                        writer.Write((byte)(src >> 16));
                    }
                    break;
                case BitDepth.Float32:
                    while (from < to)
                        writer.Write(samples[from++]);
                    break;
            }
        }

        /// <summary>
        /// Write a block of samples.
        /// </summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public override void WriteBlock(float[][] samples, long from, long to) {
            switch (Bits) {
                case BitDepth.Int8:
                    while (from < to) {
                        for (int channel = 0; channel < samples.Length; ++channel)
                            writer.Write((sbyte)(samples[channel][from] * sbyte.MaxValue));
                        ++from;
                    }
                    break;
                case BitDepth.Int16:
                    while (from < to) {
                        for (int channel = 0; channel < samples.Length; ++channel)
                            writer.Write((short)(samples[channel][from] * short.MaxValue));
                        ++from;
                    }
                    break;
                case BitDepth.Int24:
                    while (from < to) {
                        for (int channel = 0; channel < samples.Length; ++channel) {
                            int src = (int)(samples[channel][from] * 8388607f);
                            writer.Write((byte)src);
                            writer.Write((byte)(src >> 8));
                            writer.Write((byte)(src >> 16));
                        }
                        ++from;
                    }
                    break;
                case BitDepth.Float32:
                    while (from < to) {
                        for (int channel = 0; channel < samples.Length; ++channel)
                            writer.Write(samples[channel][from]);
                        ++from;
                    }
                    break;
            }
        }

        /// <summary>
        /// Append an extra chunk to the file.
        /// </summary>
        /// <param name="id">4 byte identifier of the chunk</param>
        /// <param name="data">Raw data of the chunk</param>
        /// <remarks>The <paramref name="id"/> has a different byte order in the file to memory,
        /// refer to <see cref="RIFFWave"/> for samples.</remarks>
        public void WriteChunk(int id, byte[] data) {
            if (data.LongLength > uint.MaxValue) {
                if (largeChunkSizes == null) {
                    largeChunkSizes = new List<Tuple<int, long>>();
                }
                largeChunkSizes.Add(new Tuple<int, long>(id, data.LongLength));
            }

            writer.Write(id);
            writer.Write(data.Length);
            writer.Write(data);
        }

        /// <summary>
        /// Update 64-bit size information when needed before closing the file.
        /// </summary>
        public override void Dispose() {
            long contentSize = writer.BaseStream.Position - 8;
            if (contentSize > uint.MaxValue || MaxLargeChunks != 0) {
                int largeChunks = 0;
                if (largeChunkSizes != null)
                    largeChunks = largeChunkSizes.Count;

                // 64-bit sync word
                writer.BaseStream.Position = 0;
                writer.Write(RIFFWave.syncWord1_64);
                writer.Write(0xFFFFFFFF);
                writer.BaseStream.Position += 4;

                // 64-bit format header
                writer.Write(RIFFWave.ds64Sync);
                writer.Write(junkBaseSize + largeChunks * junkExtraSize);

                // Mandatory sizes
                writer.Write(contentSize);
                writer.Write(DataLength);
                writer.Write(Length);

                // Large chunk sizes
                if (largeChunkSizes != null) {
                    writer.Write(largeChunks);
                    for (int i = 0; i < largeChunks; ++i) {
                        writer.Write(largeChunkSizes[i].Item1);
                        writer.Write(largeChunkSizes[i].Item2);
                    }
                } else {
                    writer.Write(0);
                }

                // Fill the unused space with junk
                int emptyBytes = (MaxLargeChunks - largeChunks) * junkExtraSize;
                if (emptyBytes != 0) {
                    writer.Write(RIFFWave.junkSync);
                    writer.Write(emptyBytes - 8);
                }
            }
            base.Dispose();
        }

        /// <summary>
        /// Size of the format header.
        /// </summary>
        const uint fmtSize = 36;

        /// <summary>
        /// Minimum size of the temporary header that could be replaced with a size header.
        /// </summary>
        const int junkBaseSize = 28;

        /// <summary>
        /// Size for one extra header information in the temporary header.
        /// </summary>
        const int junkExtraSize = 12;
    }
}
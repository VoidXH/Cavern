using System;
using System.Collections.Generic;
using System.IO;

using Cavern.Format.Consts;
using Cavern.Remapping;

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
        /// Sizes of chunks larger than 4 GB.
        /// </summary>
        List<Tuple<int, long>> largeChunkSizes;

        /// <summary>
        /// Bytes used for the actual PCM data.
        /// </summary>
        long DataLength => Length * ChannelCount * ((int)Bits / 8);

        /// <summary>
        /// Channels present in the file, see <see cref="WaveExtensibleChannel"/>.
        /// </summary>
        int channelMask = -1;

        /// <summary>
        /// Minimal RIFF Wave file writer.
        /// </summary>
        /// <param name="writer">File writer object</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples per channel</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public RIFFWaveWriter(BinaryWriter writer, int channelCount, long length, int sampleRate, BitDepth bits) :
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
        public RIFFWaveWriter(BinaryWriter writer, ReferenceChannel[] channels, long length, int sampleRate, BitDepth bits) :
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
        /// <param name="period">Channels separated by this many channels are played simultaneously</param>
        public static void WriteOffset(string path, float[][] data, int sampleRate, BitDepth bits, int period = -1) {
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
            writer.Write(syncWord1);
            uint fmtContentLength = channelMask == -1 ? fmtContentSize : waveformatextensibleSize;
            uint fmtSize = (byte)(fmtJunkSize + fmtContentLength);
            bool inConstraints = fmtSize + DataLength < uint.MaxValue;
            if (inConstraints && MaxLargeChunks == 0) {
                writer.Write(fmtSize + (uint)DataLength);
                writer.Write(syncWord2);
            } else {
                writer.Write(inConstraints ? fmtSize + (uint)DataLength : 0xFFFFFFFF);
                writer.Write(syncWord2);
                if (!inConstraints || MaxLargeChunks != 0) {
                    writer.Write(junkSync);
                    int junkLength = junkBaseSize + MaxLargeChunks * junkExtraSize;
                    writer.Write(junkLength);
                    writer.Write(new byte[junkLength]);
                }
            }

            // Format header
            writer.Write(formatSync);
            writer.Write(fmtContentLength);
            if (channelMask == -1) {
                writer.Write(Bits == BitDepth.Float32 ? (short)3 : (short)1); // Sample format...
            } else {
                writer.Write(extensibleSampleFormat); // ...or that it will be in an extended header
            }
            writer.Write(BitConverter.GetBytes((short)ChannelCount)); // Audio channels
            writer.Write(BitConverter.GetBytes(SampleRate)); // Sample rate
            int blockAlign = ChannelCount * ((int)Bits / 8), BPS = SampleRate * blockAlign;
            writer.Write(BitConverter.GetBytes(BPS)); // Bytes per second
            writer.Write(BitConverter.GetBytes((short)blockAlign)); // Block size in bytes
            writer.Write(BitConverter.GetBytes((short)Bits)); // Bit depth

            if (channelMask != -1) { // Use WAVEFORMATEXTENSIBLE
                writer.Write((short)22); // Extensible header size - 1
                writer.Write((short)Bits); // Bit depth
                writer.Write(channelMask); // Channel mask
                writer.Write(Bits == BitDepth.Float32 ? (short)3 : (short)1); // Sample format
                writer.Write(new byte[] { 0, 0, 0, 0, 16, 0, 128, 0, 0, 170, 0, 56, 155, 113 }); // SubFormat GUID
            }

            // Data header
            writer.Write(dataSync);
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
                        int src = (int)(samples[from++] * BitConversions.int24Max);
                        writer.Write((short)src);
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
                            int src = (int)(samples[channel][from] * BitConversions.int24Max);
                            writer.Write((short)src);
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
            if (writer == null) {
                return; // Already disposed
            }

            long contentSize = writer.BaseStream.Position - 8;
            if (contentSize > uint.MaxValue || MaxLargeChunks != 0) {
                int largeChunks = 0;
                if (largeChunkSizes != null)
                    largeChunks = largeChunkSizes.Count;

                // 64-bit sync word
                writer.BaseStream.Position = 0;
                writer.Write(syncWord1_64);
                writer.Write(0xFFFFFFFF);
                writer.BaseStream.Position += 4;

                // 64-bit format header
                writer.Write(ds64Sync);
                writer.Write(junkBaseSize + largeChunks * junkExtraSize);

                // Mandatory sizes
                writer.Write(contentSize);
                writer.Write(DataLength);
                writer.Write(Length);

                // Large chunk sizes
                writer.Write(largeChunks);
                if (largeChunkSizes != null) {
                    for (int i = 0; i < largeChunks; ++i) {
                        writer.Write(largeChunkSizes[i].Item1);
                        writer.Write(largeChunkSizes[i].Item2);
                    }
                }

                // Fill the unused space with junk
                int emptyBytes = (MaxLargeChunks - largeChunks) * junkExtraSize;
                if (emptyBytes != 0) {
                    writer.Write(junkSync);
                    writer.Write(emptyBytes - 8);
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
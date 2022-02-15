using System;
using System.IO;

using Cavern.Format.Common;

namespace Cavern.Format {
    /// <summary>
    /// Minimal RIFF wave file writer.
    /// </summary>
    public class RIFFWaveWriter : AudioWriter {
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
        /// Create the file header.
        /// </summary>
        public override void WriteHeader() {
            // RIFF header
            writer.Write(RIFFWaveUtils.RIFF);
            int dataLength = (int)(Length * ChannelCount * ((int)Bits / 8));
            writer.Write(BitConverter.GetBytes(36 + dataLength)); // File length
            writer.Write(RIFFWaveUtils.WAVE);

            // Format header
            writer.Write(RIFFWaveUtils.fmt);
            writer.Write(new byte[] { 16, 0, 0, 0 }); // FMT header size
            writer.Write(new byte[] { Bits == BitDepth.Float32 ? (byte)3 : (byte)1, 0 }); // Sample format
            writer.Write(BitConverter.GetBytes((short)ChannelCount)); // Audio channels
            writer.Write(BitConverter.GetBytes(SampleRate)); // Sample rate
            int blockAlign = ChannelCount * ((int)Bits / 8), BPS = SampleRate * blockAlign;
            writer.Write(BitConverter.GetBytes(BPS)); // Bytes per second
            writer.Write(BitConverter.GetBytes((short)blockAlign)); // Block size in bytes
            writer.Write(BitConverter.GetBytes((short)Bits)); // Bit depth

            // Data header
            writer.Write(RIFFWaveUtils.data);
            writer.Write(BitConverter.GetBytes(dataLength)); // Data length
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
    }
}
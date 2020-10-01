using System;
using System.IO;

namespace Cavern.Format {
    /// <summary>Minimal RIFF wave file writer.</summary>
    public class RIFFWaveWriter : AudioWriter {
        /// <summary>Minimal RIFF wave file writer.</summary>
        /// <param name="writer">File writer object</param>
        /// <param name="channelCount">Output channel count</param>
        /// <param name="length">Output length in samples</param>
        /// <param name="sampleRate">Output sample rate</param>
        /// <param name="bits">Output bit depth</param>
        public RIFFWaveWriter(BinaryWriter writer, int channelCount, long length, int sampleRate, BitDepth bits) :
            base(writer, channelCount, length, sampleRate, bits) {}

        /// <summary>Create the file header.</summary>
        public override void WriteHeader() {
            // RIFF header
            writer.Write(RIFFWaveUtils.RIFF);
            int dataLength = (int)(length * channelCount * ((int)bits / 8));
            writer.Write(BitConverter.GetBytes(36 + dataLength)); // File length
            writer.Write(RIFFWaveUtils.WAVE);

            // Format header
            writer.Write(RIFFWaveUtils.fmt);
            writer.Write(new byte[] { 16, 0, 0, 0 }); // FMT header size
            writer.Write(new byte[] { bits == BitDepth.Float32 ? (byte)3 : (byte)1, 0 }); // Sample format
            writer.Write(BitConverter.GetBytes((short)channelCount)); // Audio channels
            writer.Write(BitConverter.GetBytes(sampleRate)); // Sample rate
            int blockAlign = channelCount * ((int)bits / 8), BPS = sampleRate * blockAlign;
            writer.Write(BitConverter.GetBytes(BPS)); // Bytes per second
            writer.Write(BitConverter.GetBytes((short)blockAlign)); // Block size in bytes
            writer.Write(BitConverter.GetBytes((short)bits)); // Bit depth

            // Data header
            writer.Write(RIFFWaveUtils.data);
            writer.Write(BitConverter.GetBytes(dataLength)); // Data length
        }

        /// <summary>Write a block of samples.</summary>
        /// <param name="samples">Samples to write</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        public override void WriteBlock(float[] samples, long from, long to) {
            switch (bits) {
                case BitDepth.Int8:
                    while (from < to)
                        writer.Write((byte)((samples[from++] + 1f) * 127f));
                    break;
                case BitDepth.Int16:
                    while (from < to)
                        writer.Write((short)(samples[from++] * 32767f));
                    break;
                case BitDepth.Int24:
                    while (from < to) {
                        int src = (int)(samples[from++] * 8388607f) * 256;
                        writer.Write((byte)(src >> 8));
                        writer.Write((byte)(src >> 16));
                        writer.Write((byte)(src >> 24));
                    }
                    break;
                case BitDepth.Float32:
                    while (from < to)
                        writer.Write(samples[from++]);
                    break;
            }
        }
    }
}
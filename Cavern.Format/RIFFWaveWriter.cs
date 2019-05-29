using System;
using System.IO;

namespace Cavern.Format {
    /// <summary>Minimal RIFF wave file writer.</summary>
    public class RIFFWaveWriter : AudioWriter {
        /// <summary>Minimal RIFF wave file writer.</summary>
        /// <param name="Writer">File writer object</param>
        /// <param name="ChannelCount">Output channel count</param>
        /// <param name="Length">Output length in samples</param>
        /// <param name="SampleRate">Output sample rate</param>
        /// <param name="Bits">Output bit depth</param>
        public RIFFWaveWriter(BinaryWriter Writer, int ChannelCount, long Length, int SampleRate, BitDepth Bits) :
            base(Writer, ChannelCount, Length, SampleRate, Bits) {}

        /// <summary>Create the file header.</summary>
        public override void WriteHeader() {
            // RIFF header
            Writer.Write(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            int DataLength = (int)(Length * ((int)Bits / 8));
            Writer.Write(BitConverter.GetBytes(36 + DataLength)); // File length
            Writer.Write(new byte[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });

            // FMT header
            Writer.Write(new byte[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            Writer.Write(new byte[] { 16, 0, 0, 0 }); // FMT header size
            Writer.Write(new byte[] { Bits == BitDepth.Float32 ? (byte)3 : (byte)1, 0 }); // Sample format
            Writer.Write(BitConverter.GetBytes((short)ChannelCount)); // Audio channels
            Writer.Write(BitConverter.GetBytes(SampleRate)); // Sample rate
            int BlockAlign = ChannelCount * ((int)Bits / 8), BPS = SampleRate * BlockAlign;
            Writer.Write(BitConverter.GetBytes(BPS)); // Bytes per second
            Writer.Write(BitConverter.GetBytes((short)BlockAlign)); // Block size in bytes
            Writer.Write(BitConverter.GetBytes((short)Bits)); // Bit depth

            // Data header
            Writer.Write(new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            Writer.Write(BitConverter.GetBytes(DataLength)); // Data length
        }

        /// <summary>Write a block of samples.</summary>
        /// <param name="Samples">Samples to write</param>
        /// <param name="From">Start position in the input array (inclusive)</param>
        /// <param name="To">End position in the input array (exclusive)</param>
        public override void WriteBlock(float[] Samples, long From, long To) {
            switch (Bits) {
                case BitDepth.Int8:
                    while (From < To)
                        Writer.Write((byte)((Samples[From++] + 1f) * 127f));
                    break;
                case BitDepth.Int16:
                    while (From < To)
                        Writer.Write((short)(Samples[From++] * 32767f));
                    break;
                case BitDepth.Float32:
                    while (From < To)
                        Writer.Write(Samples[From++]);
                    break;
            }
        }
    }
}
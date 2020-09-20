using System.IO;

namespace Cavern.Format {
    /// <summary>Minimal RIFF wave file reader.</summary>
    public class RIFFWaveReader : AudioReader {
        /// <summary>Minimal RIFF wave file reader.</summary>
        /// <param name="reader">File reader object</param>
        public RIFFWaveReader(BinaryReader reader) : base(reader) { }

        /// <summary>Read the file header.</summary>
        public override void ReadHeader() {
            // RIFF header
            BlockTest(RIFFWaveUtils.RIFF);
            reader.ReadInt32(); // File length
            BlockTest(RIFFWaveUtils.WAVE);

            // Format header
            BlockTest(RIFFWaveUtils.fmt);
            reader.ReadInt32(); // Format header length
            short sampleFormat = reader.ReadInt16(); // 1 = int, 3 = float
            ChannelCount = reader.ReadInt16();
            SampleRate = reader.ReadInt32();
            reader.ReadInt32(); // Bytes/sec
            reader.ReadInt16(); // Block size in bytes
            short bitDepth = reader.ReadInt16();
            if (sampleFormat == 1) {
                if (bitDepth == 8)
                    Bits = BitDepth.Int8;
                else if (bitDepth == 16)
                    Bits = BitDepth.Int16;
                else
                    throw new IOException("Unsupported bit depth.");
            } else if (sampleFormat == 3 && bitDepth == 32)
                Bits = BitDepth.Float32;
            else
                throw new IOException("Unsupported bit depth.");

            // Data header
            byte[] cache = new byte[RIFFWaveUtils.data.Length];
            while (!RollingBlockCheck(cache, RIFFWaveUtils.data)) ;
            Length = reader.ReadInt32() * 8 / (int)Bits / ChannelCount;
        }

        /// <summary>Read a block of samples.</summary>
        /// <param name="samples">Input array</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.</remarks>
        public override void ReadBlock(float[] samples, long from, long to) {
            switch (Bits) {
                case BitDepth.Int8:
                    while (from < to)
                        samples[from++] = reader.ReadByte() / 127f - 1f;
                    break;
                case BitDepth.Int16:
                    while (from < to)
                        samples[from++] = reader.ReadInt16() / 32767f;
                    break;
                case BitDepth.Float32:
                    while (from < to)
                        samples[from++] = reader.ReadSingle();
                    break;
            }
        }
    }
}
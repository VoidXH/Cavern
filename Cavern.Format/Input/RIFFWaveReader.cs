using System;
using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Decoders;

namespace Cavern.Format {
    /// <summary>
    /// Minimal RIFF wave file reader.
    /// </summary>
    public class RIFFWaveReader : AudioReader {
        /// <summary>
        /// Minimal RIFF wave file reader.
        /// </summary>
        /// <param name="reader">File reader object</param>
        public RIFFWaveReader(BinaryReader reader) : base(reader) { }

        /// <summary>
        /// Minimal RIFF wave file reader.
        /// </summary>
        /// <param name="path">Input file name</param>
        public RIFFWaveReader(string path) : base(path) { }

        /// <summary>
        /// Read the file header.
        /// </summary>
        public override void ReadHeader() {
            // RIFF header
            BlockTest(RIFFWaveUtils.RIFF);
            reader.ReadInt32(); // File length
            BlockTest(RIFFWaveUtils.WAVE);

            // Format header
            BlockTest(RIFFWaveUtils.fmt);
            reader.ReadInt32(); // Format header length
            short sampleFormat = reader.ReadInt16(); // 1 = int, 3 = float, -2 = WAVE EX
            ChannelCount = reader.ReadInt16();
            SampleRate = reader.ReadInt32();
            reader.ReadInt32(); // Bytes/sec
            reader.ReadInt16(); // Block size in bytes
            short bitDepth = reader.ReadInt16();
            if (sampleFormat == -2) {
                reader.ReadInt16(); // Extension size (22)
                reader.ReadInt16(); // Valid bits per sample
                reader.ReadInt32(); // Channel mask
                sampleFormat = reader.ReadInt16();
                reader.BaseStream.Position += 15; // Skip the rest of the sub format GUID
            }
            if (sampleFormat == 1) {
                Bits = bitDepth switch {
                    8 => BitDepth.Int8,
                    16 => BitDepth.Int16,
                    24 => BitDepth.Int24,
                    _ => throw new IOException($"Unsupported bit depth for signed little endian integer: {bitDepth}.")
                };
            } else if (sampleFormat == 3 && bitDepth == 32)
                Bits = BitDepth.Float32;
            else
                throw new IOException($"Unsupported bit depth ({bitDepth}) for sample format {sampleFormat}.");

            // Data header
            byte[] cache = new byte[RIFFWaveUtils.data.Length];
            while (!RollingBlockCheck(cache, RIFFWaveUtils.data)) ;
            Length = reader.ReadInt32() * 8 / (int)Bits / ChannelCount;
        }

        /// <summary>
        /// Read a block of samples.
        /// </summary>
        /// <param name="samples">Input array</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.
        /// All samples are counted, not just a single channel.</remarks>
        public override void ReadBlock(float[] samples, long from, long to) {
            const long skip = 10 * 1024 * 1024 / sizeof(float); // 10 MB source splits at max to optimize for both memory and IO
            if (to - from > skip) {
                for (; from < to; from += skip)
                    ReadBlock(samples, from, Math.Min(to, from + skip));
                return;
            }

            byte[] source = reader.ReadBytes((int)(to - from) * ((int)Bits >> 3));
            RIFFWaveDecoder.DecodeLittleEndianBlock(source, samples, from, Bits);
        }
    }
}
using System;

using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts a RIFF WAVE bitstream to raw samples.
    /// </summary>
    internal class RIFFWaveDecoder : Decoder {
        /// <summary>
        /// Bit depth of the WAVE file.
        /// </summary>
        readonly BitDepth bits;

        /// <summary>
        /// Converts a RIFF WAVE bitstream to raw samples.
        /// </summary>
        public RIFFWaveDecoder(BlockBuffer<byte> reader, BitDepth bits) : base(reader) => this.bits = bits;

        /// <summary>
        /// Read and decode a given number of samples.
        /// </summary>
        /// <param name="target">Array to decode data into</param>
        /// <param name="from">Start position in the input array (inclusive)</param>
        /// <param name="to">End position in the input array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.
        /// All samples are counted, not just a single channel.</remarks>
        public override void DecodeBlock(float[] target, long from, long to) {
            const long skip = 10 * 1024 * 1024 / sizeof(float); // 10 MB source splits at max to optimize for both memory and IO
            if (to - from > skip) {
                for (; from < to; from += skip)
                    DecodeBlock(target, from, Math.Min(to, from + skip));
                return;
            }

            byte[] source = reader.Read((int)(to - from) * ((int)bits >> 3));
            DecodeLittleEndianBlock(source, target, from, bits);
        }

        /// <summary>
        /// Decode a block of RIFF WAVE data.
        /// </summary>
        public static void DecodeLittleEndianBlock(byte[] source, float[] target, long targetOffset, BitDepth bits) {
            switch (bits) {
                case BitDepth.Int8: {
                    for (int i = 0; i < source.Length; ++i)
                        target[targetOffset++] = source[i] * BitConversions.fromInt8;
                    break;
                }
                case BitDepth.Int16: {
                    for (int i = 0; i < source.Length;)
                        target[targetOffset++] = (short)(source[i++] | source[i++] << 8) * BitConversions.fromInt16;
                    break;
                }
                case BitDepth.Int24: {
                    for (int i = 0; i < source.Length;)
                        target[targetOffset++] = ((source[i++] << 8 | source[i++] << 16 | source[i++] << 24) >> 8) *
                            BitConversions.fromInt24; // This needs to be shifted into overflow for correct sign
                    break;
                }
                case BitDepth.Float32: {
                    if (targetOffset < int.MaxValue / sizeof(float))
                        Buffer.BlockCopy(source, 0, target, (int)targetOffset * sizeof(float), source.Length);
                    else for (int i = 0; i < source.Length; ++i)
                        target[targetOffset++] = BitConverter.ToSingle(source, i * sizeof(float));
                    break;
                }
            }
        }
    }
}
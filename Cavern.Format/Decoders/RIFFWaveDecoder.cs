using System;
using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Consts;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts a RIFF WAVE bitstream to raw samples.
    /// </summary>
    public class RIFFWaveDecoder : Decoder {
        /// <summary>
        /// Bit depth of the WAVE file.
        /// </summary>
        public BitDepth Bits { get; private set; }

        /// <summary>
        /// Content channel count.
        /// </summary>
        public override int ChannelCount => channelCount;
        readonly int channelCount;

        /// <summary>
        /// Content length in samples for a single channel.
        /// </summary>
        public override long Length => length;
        readonly long length;

        /// <summary>
        /// Bitstream sample rate.
        /// </summary>
        public override int SampleRate => sampleRate;
        readonly int sampleRate;

        /// <summary>
        /// Converts a RIFF WAVE bitstream to raw samples.
        /// </summary>
        public RIFFWaveDecoder(BlockBuffer<byte> reader, int channelCount, long length, int sampleRate, BitDepth bits) : base(reader) {
            this.channelCount = channelCount;
            this.length = length;
            this.sampleRate = sampleRate;
            Bits = bits;
        }

        /// <summary>
        /// Converts a RIFF WAVE bitstream with header to raw samples.
        /// </summary>
        public RIFFWaveDecoder(BinaryReader reader) {
            // RIFF header
            if (reader.ReadInt32() != RIFFWave.syncWord1)
                throw new SyncException();
            reader.BaseStream.Position += 4; // File length

            // Format header
            if (reader.ReadInt64() != RIFFWave.syncWord2)
                throw new SyncException();
            reader.BaseStream.Position += 4; // Format header length
            short sampleFormat = reader.ReadInt16(); // 1 = int, 3 = float, -2 = WAVE EX
            channelCount = reader.ReadInt16();
            sampleRate = reader.ReadInt32();
            reader.BaseStream.Position += 4; // Bytes/sec
            reader.BaseStream.Position += 2; // Block size in bytes
            short bitDepth = reader.ReadInt16();
            if (sampleFormat == -2) {
                // Extension size (22) - 2 bytes, valid bits per sample - 2 bytes, channel mask - 4 bytes
                reader.BaseStream.Position += 8;
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
            int header = 0;
            do
                header = (header << 8) | reader.ReadByte();
            while (header != RIFFWave.syncWord3BE && reader.BaseStream.Position < reader.BaseStream.Length);
            length = reader.ReadUInt32() * 8L / (long)Bits / ChannelCount;
            this.reader = BlockBuffer<byte>.Create(reader);
        }

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

            byte[] source = reader.Read((int)(to - from) * ((int)Bits >> 3));
            if (source != null)
                DecodeLittleEndianBlock(source, target, from, Bits);
            else
                Array.Clear(target, (int)from, (int)(to - from));
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
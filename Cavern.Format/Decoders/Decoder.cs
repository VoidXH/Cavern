using System;

using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts a bitstream to raw samples.
    /// </summary>
    public abstract class Decoder {
        /// <summary>
        /// Content channel count.
        /// </summary>
        public int ChannelCount { get; protected set; }

        /// <summary>
        /// Location in the stream in samples. Formats that don't support this feature return -1.
        /// </summary>
        public long Position { get; protected set; }

        /// <summary>
        /// Content length in samples for a single channel. For real-time streams, this value is -1.
        /// </summary>
        public long Length { get; protected set; }

        /// <summary>
        /// Bitstream sample rate.
        /// </summary>
        public int SampleRate { get; protected set; }

        /// <summary>
        /// Stream reader and block regrouping object.
        /// </summary>
        protected BlockBuffer<byte> reader;

        /// <summary>
        /// Converts a bitstream to raw samples.
        /// </summary>
        protected Decoder(BlockBuffer<byte> reader) => this.reader = reader;

        /// <summary>
        /// Gives the possibility of setting <see cref="reader"/> after a derived constructor has read a header.
        /// </summary>
        /// <remarks>Not setting <see cref="reader"/> in all constructors can break a decoder.</remarks>
        protected Decoder() { }

        /// <summary>
        /// Read and decode a given number of samples.
        /// </summary>
        /// <param name="target">Array to decode data into</param>
        /// <param name="from">Start position in the target array (inclusive)</param>
        /// <param name="to">End position in the target array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.
        /// All samples are counted, not just a single channel.</remarks>
        public abstract void DecodeBlock(float[] target, long from, long to);

        /// <summary>
        /// Start the following reads from the selected sample.
        /// </summary>
        /// <param name="sample">The selected sample, for a single channel</param>
        public abstract void Seek(long sample);

        /// <summary>
        /// Decode a block of RIFF WAVE data.
        /// </summary>
        internal static void DecodeLittleEndianBlock(BlockBuffer<byte> reader, float[] target, long from, long to, BitDepth bits) {
            byte[] source = reader.Read((int)(to - from) * ((int)bits >> 3));
            if (source == null) {
                Array.Clear(target, (int)from, (int)(to - from));
                return;
            }

            switch (bits) {
                case BitDepth.Int8: {
                        for (int i = 0; i < source.Length; i++) {
                            target[from++] = source[i] * BitConversions.fromInt8;
                        }
                        break;
                    }
                case BitDepth.Int16: {
                        for (int i = 0; i < source.Length;) {
                            target[from++] = (short)(source[i++] | source[i++] << 8) * BitConversions.fromInt16;
                        }
                        break;
                    }
                case BitDepth.Int24: {
                        for (int i = 0; i < source.Length;) {
                            target[from++] = ((source[i++] << 8 | source[i++] << 16 | source[i++] << 24) >> 8) *
                                BitConversions.fromInt24; // This needs to be shifted into overflow for correct sign
                        }
                        break;
                    }
                case BitDepth.Float32: {
                        if (from < int.MaxValue / sizeof(float)) {
                            Buffer.BlockCopy(source, 0, target, (int)from * sizeof(float), source.Length);
                        } else {
                            for (int i = 0; i < source.Length; i++) {
                                target[from++] = BitConverter.ToSingle(source, i * sizeof(float));
                            }
                        }
                        break;
                    }
            }
        }
    }
}
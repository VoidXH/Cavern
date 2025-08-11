using System;
using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Consts;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Decoders which contain uncompressed interlaced PCM samples.
    /// </summary>
    public abstract class UncompressedDecoder : Decoder {
        /// <summary>
        /// The contained PCM data is in big-endian format.
        /// </summary>
        protected bool bigEndian;

        /// <summary>
        /// The location of the first sample in the file stream. Knowing this allows seeking.
        /// </summary>
        protected long dataStart;

        /// <summary>
        /// Input stream when reading from an uncompressed file. If the stream is null, then only a block buffer is available, whose parent has to be seeked.
        /// </summary>
        protected Stream stream;

        /// <summary>
        /// Gives the possibility of setting <see cref="reader"/> after a derived constructor has read a header.
        /// </summary>
        /// <remarks>Not setting <see cref="reader"/> in all constructors can break a decoder.</remarks>
        protected UncompressedDecoder() { }

        /// <summary>
        /// Decoders which contain uncompressed interlaced PCM samples.
        /// </summary>
        protected UncompressedDecoder(BlockBuffer<byte> reader) : base(reader) { }

        /// <inheritdoc/>
        public override void DecodeBlock(float[] target, long from, long to) {
            const long skip = FormatConsts.blockSize / sizeof(float); // Source split optimization for both memory and IO
            if (to - from > skip) {
                for (; from < to; from += skip) {
                    DecodeBlock(target, from, Math.Min(to, from + skip));
                }
                return;
            }

            if (!bigEndian) {
                DecodeLittleEndianBlock(reader, target, from, to, Bits);
            } else {
                DecodeBigEndianBlock(reader, target, from, to, Bits);
            }
            Position += (to - from) / ChannelCount;
        }

        /// <inheritdoc/>
        public override void Seek(long sample) {
            if (stream == null) {
                throw new StreamingException();
            }
            stream.Position = dataStart + sample * ChannelCount * ((int)Bits >> 3);
            Position = sample;
            reader.Clear();
        }
    }
}

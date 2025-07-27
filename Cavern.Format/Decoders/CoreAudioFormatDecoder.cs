using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Consts;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts a Core Audio Format bitstream with header to raw samples.
    /// </summary>
    public class CoreAudioFormatDecoder : UncompressedDecoder {
        /// <summary>
        /// Converts a Core Audio Format bitstream with header to raw samples.
        /// </summary>
        /// <param name="reader">File reader object</param>
        public CoreAudioFormatDecoder(Stream reader) : this(reader, false) { }

        /// <summary>
        /// Converts a Core Audio Format bitstream with header to raw samples.
        /// </summary>
        /// <param name="reader">File reader object</param>
        /// <param name="skipSyncWord">The sync word from which the format is detected was already read from the stream -
        /// allows for format detection in streams that don't support <see cref="Stream.Position"/></param>
        public CoreAudioFormatDecoder(Stream reader, bool skipSyncWord) {
            // caff header
            if (!skipSyncWord) {
                int sync = reader.ReadInt32();
                if (sync != CoreAudioFormatConsts.syncWord) {
                    throw new SyncException();
                }
            }
            stream = reader;

            stream.ReadUInt16(); // Version
            stream.ReadUInt16(); // Flags

            while (reader.Position < reader.Length) {
                int chunkType = reader.ReadInt32();
                long chunkSize = reader.ReadInt64BE();

                switch (chunkType) {
                    case CoreAudioFormatConsts.audioDescriptionChunk:
                        ParseAudioDescriptionChunk(reader);
                        break;
                    case RIFFWaveConsts.dataSync:
                        Length = chunkSize / ((long)Bits >> 3) / ChannelCount;
                        dataStart = reader.Position + 4; // First 4 bytes are the edit count
                        stream.Position += chunkSize;
                        break;
                    default:
                        stream.Position += chunkSize;
                        break;
                }
            }

            reader.Position = dataStart;
            this.reader = BlockBuffer<byte>.Create(reader, FormatConsts.blockSize);
        }

        /// <summary>
        /// Read the main Core Audio Format header.
        /// </summary>
        void ParseAudioDescriptionChunk(Stream reader) {
            SampleRate = (int)reader.ReadDoubleBE();
            string formatID = reader.ReadASCII(4);
            if (formatID != "lpcm") {
                throw new UnsupportedFormatException(formatID);
            }
            uint formatFlags = reader.ReadUInt32BE(); // Bit 0: is float, bit 1: is little-endian
            bigEndian = (formatFlags & 0x2) == 0;
            reader.ReadUInt32BE(); // Bytes per packet (bytes per sample * channels, redundant for PCM)
            reader.ReadUInt32BE(); // Frames per packet (1 for PCM)
            ChannelCount = reader.ReadInt32BE();
            Bits = (BitDepth)reader.ReadUInt32BE();
        }
    }
}

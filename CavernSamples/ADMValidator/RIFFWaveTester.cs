using Cavern.Format.Common;
using Cavern.Format.Consts;
using Cavern.Format.Transcoders;
using Cavern.Format.Transcoders.AudioDefinitionModelElements;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Reads a WAV header for testing.
    /// </summary>
    public class RIFFWaveTester : Decoder {
        /// <summary>
        /// Object metadata for Broadcast Wave Files.
        /// </summary>
        public AudioDefinitionModel ADM { get; }

        /// <summary>
        /// Dolby Metadata for Dolby Atmos-compatible ADM BWF files.
        /// </summary>
        public DolbyMetadata DBMD { get; }

        /// <summary>
        /// Bit depth of the WAVE file.
        /// </summary>
        public BitDepth Bits { get; private set; }

        /// <summary>
        /// The file size according to the RIFF header.
        /// </summary>
        public long FileLength { get; private set; }

        /// <summary>
        /// All subchunks start at an even byte. Some decoders only work if this is true, so this is a validation option for special cases.
        /// </summary>
        public bool DwordAligned { get; private set; }

        /// <summary>
        /// An overridden size is not marked with 0xFFFFFFFF.
        /// </summary>
        public bool RF64Mismatch { get; private set; }

        /// <summary>
        /// List of all chunks and their sizes for debugging.
        /// </summary>
        public IReadOnlyList<(string chunk, long size)> ChunkSizes => chunkSizes;
        readonly List<(string chunk, long size)> chunkSizes = new List<(string chunk, long size)>();

        /// <summary>
        /// Reads a WAV header for testing.
        /// </summary>
        public RIFFWaveTester(Stream reader) {
            // RIFF header
            int sync = reader.ReadInt32();
            if (sync != RIFFWave.syncWord1 && sync != RIFFWave.syncWord1_64) {
                throw new SyncException();
            }
            FileLength = reader.ReadInt32();
            if (reader.ReadInt32() != RIFFWave.syncWord2) {
                throw new SyncException();
            }

            // Subchunks
            Dictionary<uint, long> sizeOverrides = null;
            ChannelAssignment chna = null;
            DwordAligned = true;
            while (reader.Position < reader.Length) {
                uint headerID = reader.ReadUInt32();
                if (((headerID & 0xFF) == 0) && (reader.Position & 1) == 1) {
                    reader.Position -= 3;
                    continue;
                }
                if (headerID == 0) {
                    continue;
                }
                long headerSize = reader.ReadUInt32();
                if (sizeOverrides != null && sizeOverrides.ContainsKey(headerID)) {
                    if (headerSize != 0xFFFFFFFF) {
                        RF64Mismatch = true;
                    }
                    headerSize = sizeOverrides[headerID];
                }

                if ((reader.Position & 1) == 1) {
                    DwordAligned = false;
                }
                chunkSizes.Add((new string(headerID.ToFourCC().Reverse().ToArray()), headerSize));

                switch (headerID) {
                    case RIFFWave.formatSync:
                        long headerEnd = reader.Position + headerSize;
                        ParseFormatHeader(reader);
                        reader.Position = headerEnd;
                        break;
                    case RIFFWave.ds64Sync:
                        sizeOverrides = new Dictionary<uint, long> {
                            [RIFFWave.syncWord1_64] = FileLength = reader.ReadInt64(),
                            [RIFFWave.dataSync] = reader.ReadInt64()
                        };
                        reader.Position += 8; // Sample count, redundant
                        int additionalSizes = reader.ReadInt32();
                        for (int i = 0; i < additionalSizes; i++) {
                            headerID = reader.ReadUInt32();
                            sizeOverrides[headerID] = reader.ReadInt64();
                        }
                        break;
                    case RIFFWave.axmlSync:
                        ADM = new AudioDefinitionModel(reader, headerSize, false);
                        break;
                    case RIFFWave.chnaSync:
                        chna = new ChannelAssignment(reader);
                        break;
                    case RIFFWave.dbmdSync:
                        DBMD = new DolbyMetadata(reader, headerSize, true);
                        break;
                    case RIFFWave.dataSync:
                        Length = headerSize * 8L / (long)Bits / ChannelCount;
                        long dataStart = reader.Position;
                        if (dataStart + headerSize < reader.Length) { // Read after PCM samples if there are more tags
                            reader.Position = dataStart + headerSize;
                        } else {
                            return;
                        }
                        break;
                    default: // Skip unknown headers
                        reader.Position += headerSize;
                        break;
                }
            }

            if (ADM != null && chna != null) {
                ADM.Assign(chna);
            }

            FileLength += 8; // RIFF and size are not calculated into this
        }

        /// <summary>
        /// Unused function for this header decoder.
        /// </summary>
        public override void DecodeBlock(float[] target, long from, long to) => throw new NotImplementedException();

        /// <summary>
        /// Unused function for this header decoder.
        /// </summary>
        public override void Seek(long sample) => throw new NotImplementedException();

        /// <summary>
        /// Read the main RIFF WAVE header.
        /// </summary>
        void ParseFormatHeader(Stream reader) {
            short sampleFormat = reader.ReadInt16(); // 1 = int, 3 = float, -2 = WAVEFORMATEXTENSIBLE
            ChannelCount = reader.ReadInt16();
            SampleRate = reader.ReadInt32();
            reader.Position += 4; // Bytes/sec
            reader.Position += 2; // Block size in bytes
            short bitDepth = reader.ReadInt16();
            if (sampleFormat == -2) {
                reader.Position += 2; // Extension length in bytes
                bitDepth = reader.ReadInt16();
                reader.Position += 4; // Channel mask
                sampleFormat = reader.ReadInt16();
            }
            if (sampleFormat == 1) {
                Bits = bitDepth switch {
                    8 => BitDepth.Int8,
                    16 => BitDepth.Int16,
                    24 => BitDepth.Int24,
                    _ => throw new IOException($"Unsupported bit depth for signed little endian integer: {bitDepth}.")
                };
            } else if (sampleFormat == 3 && bitDepth == 32) {
                Bits = BitDepth.Float32;
            } else {
                throw new IOException($"Unsupported bit depth ({bitDepth}) for sample format {sampleFormat}.");
            }
        }
    }
}
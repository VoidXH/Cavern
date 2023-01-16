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
        /// Content channel count.
        /// </summary>
        public override int ChannelCount => channelCount;
        int channelCount;

        /// <summary>
        /// Location in the stream in samples.
        /// </summary>
        public override long Position => 0;

        /// <summary>
        /// Content length in samples for a single channel.
        /// </summary>
        public override long Length => length;
        readonly long length;

        /// <summary>
        /// Bitstream sample rate.
        /// </summary>
        public override int SampleRate => sampleRate;
        int sampleRate;

        /// <summary>
        /// Reads a WAV header for testing.
        /// </summary>
        public RIFFWaveTester(Stream reader) {
            // RIFF header
            int sync = reader.ReadInt32();
            if (sync != RIFFWave.syncWord1 && sync != RIFFWave.syncWord1_64) {
                throw new SyncException();
            }
            long fileLength = reader.ReadInt32();
            if (reader.ReadInt32() != RIFFWave.syncWord2) {
                throw new SyncException();
            }

            // Subchunks
            Dictionary<int, long> sizeOverrides = null;
            ChannelAssignment chna = null;
            while (reader.Position < reader.Length) {
                int headerID = reader.ReadInt32();
                if (headerID == 0) {
                    throw new IOException("The file contains zero headers.");
                }
                long headerSize = (uint)reader.ReadInt32();
                if (sizeOverrides != null && sizeOverrides.ContainsKey(headerID)) {
                    headerSize = sizeOverrides[headerID];
                }

                switch (headerID) {
                    case RIFFWave.formatSync:
                        long headerEnd = reader.Position + headerSize;
                        ParseFormatHeader(reader);
                        reader.Position = headerEnd;
                        break;
                    case RIFFWave.ds64Sync:
                        sizeOverrides = new Dictionary<int, long>() {
                            [RIFFWave.syncWord1_64] = fileLength = reader.ReadInt64(),
                            [RIFFWave.dataSync] = reader.ReadInt64()
                        };
                        reader.Position += 8; // Sample count, redundant
                        int additionalSizes = reader.ReadInt32();
                        for (int i = 0; i < additionalSizes; i++) {
                            headerID = reader.ReadInt32();
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
                        length = (uint)headerSize * 8L / (long)Bits / ChannelCount;
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

            fileLength += 8; // RIFF and size are not calculated into this
            if (reader.Position != fileLength) {
                throw new IOException($"The file is truncated. Expected length is {fileLength}, actual length is {reader.Position}.");
            }
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
            channelCount = reader.ReadInt16();
            sampleRate = reader.ReadInt32();
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
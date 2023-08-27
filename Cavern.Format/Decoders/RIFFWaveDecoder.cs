using System;
using System.Collections.Generic;
using System.IO;

using Cavern.Channels;
using Cavern.Format.Common;
using Cavern.Format.Consts;
using Cavern.Format.Transcoders;
using Cavern.Format.Transcoders.AudioDefinitionModelElements;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Converts a RIFF WAVE bitstream to raw samples.
    /// </summary>
    public class RIFFWaveDecoder : Decoder {
        /// <summary>
        /// Object metadata for Broadcast Wave Files.
        /// </summary>
        public AudioDefinitionModel ADM { get; private set; }

        /// <summary>
        /// Bit depth of the WAVE file.
        /// </summary>
        public BitDepth Bits { get; private set; }

        /// <summary>
        /// WAVEFORMATEXTENSIBLE channel mask if available.
        /// </summary>
        int channelMask = -1;

        /// <summary>
        /// The location of the first sample in the file stream. Knowing this allows seeking.
        /// </summary>
        readonly long dataStart;

        /// <summary>
        /// Input stream when reading from a WAV file. If the stream is null, then only a block buffer is available,
        /// whose parent has to be seeked.
        /// </summary>
        readonly Stream stream;

        /// <summary>
        /// Converts a RIFF WAVE bitstream to raw samples.
        /// </summary>
        public RIFFWaveDecoder(BlockBuffer<byte> reader, int channelCount, long length, int sampleRate, BitDepth bits) :
            base(reader) {
            ChannelCount = channelCount;
            Length = length;
            SampleRate = sampleRate;
            Bits = bits;
        }

        /// <summary>
        /// Converts a RIFF WAVE bitstream with header to raw samples.
        /// </summary>
        public RIFFWaveDecoder(Stream reader) {
            // RIFF header
            int sync = reader.ReadInt32();
            if (sync != RIFFWave.syncWord1 && sync != RIFFWave.syncWord1_64) {
                throw new SyncException();
            }
            stream = reader;
            reader.Position += 4; // File length
            if (reader.ReadInt32() != RIFFWave.syncWord2) {
                throw new SyncException();
            }

            // Subchunks
            Dictionary<int, long> sizeOverrides = null;
            ChannelAssignment chna = null;
            while (reader.Position < reader.Length) {
                int headerID = reader.ReadInt32();
                if (((headerID & 0xFF) == 0) && (reader.Position & 1) == 1) {
                    reader.Position -= 3;
                    continue;
                }
                if (headerID == 0) {
                    continue;
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
                        sizeOverrides = new Dictionary<int, long> {
                            [RIFFWave.syncWord1_64] = reader.ReadInt64(),
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
                        ADM = new AudioDefinitionModel(reader, headerSize, true);
                        break;
                    case RIFFWave.chnaSync:
                        chna = new ChannelAssignment(reader);
                        break;
                    case RIFFWave.dataSync:
                        Length = headerSize * 8L / (long)Bits / ChannelCount;
                        dataStart = reader.Position;
                        if (dataStart + headerSize < reader.Length) { // Read after PCM samples if there are more tags
                            reader.Position = dataStart + headerSize;
                        } else {
                            Finalize(reader);
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
            Finalize(reader);
        }

        /// <summary>
        /// Get the custom channel layout or the standard layout corresponding to this file's channel count.
        /// </summary>
        public ReferenceChannel[] GetChannels() {
            if (channelMask == -1) {
                return ChannelPrototype.GetStandardMatrix(ChannelCount);
            } else {
                return RIFFWave.ParseChannelMask(channelMask);
            }
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
            const long skip = FormatConsts.blockSize / sizeof(float); // Source split optimization for both memory and IO
            if (to - from > skip) {
                for (; from < to; from += skip) {
                    DecodeBlock(target, from, Math.Min(to, from + skip));
                }
                return;
            }

            DecodeLittleEndianBlock(reader, target, from, to, Bits);
            Position += (to - from) / ChannelCount;
        }

        /// <summary>
        /// Start the following reads from the selected sample.
        /// </summary>
        /// <param name="sample">The selected sample, for a single channel</param>
        public override void Seek(long sample) {
            if (stream == null) {
                throw new StreamingException();
            }
            stream.Position = dataStart + sample * ChannelCount * ((int)Bits >> 3);
            Position = sample;
            reader.Clear();
        }

        /// <summary>
        /// Finish header reading, start data reading.
        /// </summary>
        void Finalize(Stream reader) {
            reader.Position = dataStart;
            this.reader = BlockBuffer<byte>.Create(reader, FormatConsts.blockSize);
        }

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
                long endPosition = reader.ReadInt16() + reader.Position;
                bitDepth = reader.ReadInt16();
                channelMask = reader.ReadInt32();
                sampleFormat = reader.ReadInt16();
                reader.Position = endPosition;
            }
            if (sampleFormat == 1) {
                Bits = bitDepth switch {
                    8 => BitDepth.Int8,
                    16 => BitDepth.Int16,
                    24 => BitDepth.Int24,
                    _ => throw new InvalidBitDepthException(Bits)
                };
            } else if (sampleFormat == 3 && bitDepth == 32) {
                Bits = BitDepth.Float32;
            } else {
                throw new IOException($"Unsupported bit depth ({bitDepth}) for sample format {sampleFormat}.");
            }
        }
    }
}
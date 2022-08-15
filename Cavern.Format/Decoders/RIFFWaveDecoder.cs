using System;
using System.IO;

using Cavern.Format.Common;
using Cavern.Format.Consts;
using Cavern.Format.Transcoders;
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
        /// Content channel count.
        /// </summary>
        public override int ChannelCount => channelCount;
        int channelCount;

        /// <summary>
        /// Location in the stream in samples.
        /// </summary>
        public override long Position => position;
        long position;

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
            this.channelCount = channelCount;
            this.length = length;
            this.sampleRate = sampleRate;
            Bits = bits;
        }

        /// <summary>
        /// Converts a RIFF WAVE bitstream with header to raw samples.
        /// </summary>
        public RIFFWaveDecoder(Stream reader) {
            // RIFF header
            if (reader.ReadInt32() != RIFFWave.syncWord1)
                throw new SyncException();
            stream = reader;
            reader.Position += 4; // File length
            if (reader.ReadInt32() != RIFFWave.syncWord2)
                throw new SyncException();

            // Specific headers
            bool readHeaders = true;
            bool bwfNeedingHeader = false; // Is this a Broadcast Wave File with the ADM headers after sample data
            while (readHeaders) {
                int headerID = reader.ReadInt32();
                int headerSize = reader.ReadInt32();
                switch (headerID) {
                    case RIFFWave.formatSync:
                        ParseFormatHeader(reader);
                        readHeaders = false;
                        break;
                    case RIFFWave.junkSync:
                        reader.Position += headerSize;
                        bwfNeedingHeader = ADM == null;
                        break;
                    case RIFFWave.axmlSync:
                        ADM = new AudioDefinitionModel(reader, headerSize, sampleRate);
                        bwfNeedingHeader = false;
                        break;
                    default:
                        throw new SyncException();
                }
            }

            // Find where data starts
            int header = 0;
            do
                header = (header << 8) | reader.ReadByte();
            while (header != RIFFWave.syncWord3BE && reader.Position < reader.Length);
            uint dataSize = reader.ReadUInt32();
            length = dataSize * 8L / (long)Bits / ChannelCount;
            dataStart = reader.Position;

            if (bwfNeedingHeader) {
                reader.Position += dataSize;
                if (reader.ReadInt32() == RIFFWave.axmlSync) {
                    ADM = new AudioDefinitionModel(reader, reader.ReadInt32(), sampleRate);
                    reader.Position = dataStart;
                }
            }

            reader.Position = dataStart;
            this.reader = BlockBuffer<byte>.Create(reader, FormatConsts.blockSize);
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
            const long skip = FormatConsts.blockSize / sizeof(float); // source split optimization for both memory and IO
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
            position += (to - from) / channelCount;
        }

        /// <summary>
        /// Decode a block of RIFF WAVE data.
        /// </summary>
        static void DecodeLittleEndianBlock(byte[] source, float[] target, long targetOffset, BitDepth bits) {
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

        /// <summary>
        /// Start the following reads from the selected sample.
        /// </summary>
        /// <param name="sample">The selected sample, for a single channel</param>
        public override void Seek(long sample) {
            if (stream == null)
                throw new StreamingException();
            stream.Position = dataStart + sample * channelCount * ((int)Bits >> 3);
            position = sample;
            reader.Clear();
        }

        /// <summary>
        /// Read the main RIFF WAVE header.
        /// </summary>
        void ParseFormatHeader(Stream reader) {
            short sampleFormat = reader.ReadInt16(); // 1 = int, 3 = float, -2 = WAVE EX
            channelCount = reader.ReadInt16();
            sampleRate = reader.ReadInt32();
            stream.Position += 4; // Bytes/sec
            stream.Position += 2; // Block size in bytes
            short bitDepth = reader.ReadInt16();
            if (sampleFormat == -2) {
                // Extension size (22) - 2 bytes, valid bits per sample - 2 bytes, channel mask - 4 bytes
                stream.Position += 8;
                sampleFormat = reader.ReadInt16();
                stream.Position += 15; // Skip the rest of the sub format GUID
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
        }
    }
}
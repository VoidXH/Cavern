using System;
using System.IO;
using System.Numerics;

using Cavern.Format.Consts;
using Cavern.Format.Environment;
using Cavern.Format.Utilities;
using Cavern.Utilities;

namespace Cavern.Format.Decoders {
    /// <summary>
    /// Limitless Audio Format file reader and metadata parser.
    /// </summary>
    public class LimitlessAudioFormatDecoder : Decoder {
        /// <summary>
        /// Bit depth of the WAVE file.
        /// </summary>
        public BitDepth Bits { get; private set; }

        /// <summary>
        /// Last decoded positions of objects or position of channels if the LAF file is channel-based.
        /// </summary>
        public Vector3[] ObjectPositions { get; }

        /// <summary>
        /// Description of each imported channel/object.
        /// </summary>
        readonly Channel[] channels;

        /// <summary>
        /// Bytes used before each second of samples to determine which channels are actually exported.
        /// </summary>
        readonly int layoutByteCount;

        /// <summary>
        /// Contains which channels contained any data in the last decoded second.
        /// </summary>
        readonly bool[] writtenChannels;

        /// <summary>
        /// The last loaded second, as LAF stores channel availability data every second. This is an interlaced array
        /// of the read channels, and has to be realigned when reading a block from it.
        /// </summary>
        readonly float[] lastReadSecond;

        /// <summary>
        /// Total count of audio tracks, both PCM channels and object position tracks. The first track that contains object
        /// positions is what <see cref="Decoder.ChannelCount"/> equals to. That shouldn't be audible.
        /// </summary>
        readonly int trackCount;

        /// <summary>
        /// Number of actually written tracks in the last second.
        /// </summary>
        int writtenTracks;

        /// <summary>
        /// Read position in <see cref="lastReadSecond"/>.
        /// </summary>
        int copiedSamples;

        /// <summary>
        /// Limitless Audio Format file reader and metadata parser.
        /// </summary>
        public LimitlessAudioFormatDecoder(Stream stream) {
            stream.BlockTest(LimitlessAudioFormat.limitless); // Find Limitless marker
            byte[] cache = new byte[LimitlessAudioFormat.head.Length];
            while (!stream.RollingBlockCheck(cache, LimitlessAudioFormat.head)) {
                // Find header marker, skip metadata
            }

            Bits = stream.ReadByte() switch {
                (byte)LAFMode.Int8 => BitDepth.Int8,
                (byte)LAFMode.Int16 => BitDepth.Int16,
                (byte)LAFMode.Int24 => BitDepth.Int24,
                (byte)LAFMode.Float32 => BitDepth.Float32,
                _ => throw new IOException("Unsupported LAF quality mode.")
            };
            stream.ReadByte(); // Channel mode indicator (skipped)
            trackCount = stream.ReadInt32();
            layoutByteCount = (trackCount & 7) == 0 ? trackCount >> 3 : ((trackCount >> 3) + 1);
            channels = new Channel[trackCount];
            for (int channel = 0; channel < trackCount; channel++) {
                float x = stream.ReadSingle();
                if (float.IsNaN(x) && ChannelCount == 0) {
                    ChannelCount = channel;
                }
                channels[channel] = new Channel(x, stream.ReadSingle(), stream.ReadByte() != 0);
            }
            if (ChannelCount == 0) {
                ChannelCount = trackCount;
            }

            ObjectPositions = new Vector3[ChannelCount];
            for (int i = 0; i < ChannelCount; i++) {
                ObjectPositions[i] = channels[i].SpatialPos;
            }

            SampleRate = stream.ReadInt32();
            Length = stream.ReadInt64();

            writtenChannels = new bool[trackCount];
            lastReadSecond = new float[trackCount * SampleRate];
            reader = BlockBuffer<byte>.Create(stream, FormatConsts.blockSize);
        }

        /// <summary>
        /// Read and decode a given number of samples.
        /// </summary>
        /// <param name="target">Array to decode data into</param>
        /// <param name="from">Start position in the target array (inclusive)</param>
        /// <param name="to">End position in the target array (exclusive)</param>
        /// <remarks>The next to - from samples will be read from the file.
        /// All samples are counted, not just a single channel.</remarks>
        public override void DecodeBlock(float[] target, long from, long to) {
            if (to - from > skip) {
                long actualSkip = skip - skip % ChannelCount; // Blocks have to be divisible with the channel count
                for (; from < to; from += actualSkip) {
                    DecodeBlock(target, from, Math.Min(to, from + actualSkip));
                }
                return;
            }

            while (from < to) {
                if (copiedSamples == 0) {
                    ReadSecond();
                }

                int perChannel = (int)Math.Min((to - from) / ChannelCount, SampleRate - copiedSamples),
                    codedChannel = 0;
                for (int i = 0; i < ChannelCount; i++) {
                    if (writtenChannels[i]) {
                        WaveformUtils.Insert(lastReadSecond, copiedSamples * writtenTracks + codedChannel++, writtenTracks,
                            target, (int)from + i, ChannelCount, perChannel);
                    } else {
                        WaveformUtils.ClearChannel(target, (int)from + i, ChannelCount, perChannel);
                    }
                }
                copiedSamples += perChannel;
                from += perChannel * ChannelCount;
                if (copiedSamples == SampleRate) {
                    copiedSamples = 0;
                }
            }

            if (trackCount != ChannelCount) {
                int positionSource = copiedSamples - copiedSamples % LimitlessAudioFormatEnvironmentWriter.objectStreamRate,
                    firstObjectTrack = writtenTracks - trackCount + ChannelCount;
                for (int obj = 0; obj < ChannelCount; obj++) {
                    int offset = (positionSource + (obj & 0b1111) /* object */ * 3 /* coordinate */) * writtenTracks + // Sample positioning
                        firstObjectTrack + (obj >> 4); // Object track positioning
                    ObjectPositions[obj] = new Vector3(lastReadSecond[offset], lastReadSecond[offset + writtenTracks],
                        lastReadSecond[offset + writtenTracks * 2]);
                }
            }
        }

        /// <summary>
        /// Start the following reads from the selected sample.
        /// </summary>
        /// <param name="sample">The selected sample, for a single channel</param>
        public override void Seek(long sample) => throw new NotImplementedException();

        /// <summary>
        /// Read the next second of audio data.
        /// </summary>
        void ReadSecond() {
            byte[] layoutBytes = reader.Read(layoutByteCount);
            if (layoutBytes.Length == 0) {
                return;
            }
            writtenTracks = 0;
            for (int channel = 0; channel < trackCount; channel++) {
                if (writtenChannels[channel] = ((layoutBytes[channel >> 3] >> (channel & 7)) & 1) != 0) {
                    ++writtenTracks;
                }
            }

            int samplesToRead = (int)Math.Min(Length - Position, SampleRate) * writtenTracks;
            DecodeLittleEndianBlock(reader, lastReadSecond, 0, samplesToRead, Bits);
            Position += SampleRate;
        }

        /// <summary>
        /// Maximum size of each read block. This can balance optimization between memory and IO.
        /// </summary>
        const long skip = FormatConsts.blockSize / sizeof(float);
    }
}
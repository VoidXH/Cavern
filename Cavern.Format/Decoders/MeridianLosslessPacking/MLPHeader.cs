using Cavern.Channels;
using Cavern.Format.Common;
using Cavern.Format.Consts;
using Cavern.Format.Utilities;
using System;

namespace Cavern.Format.Decoders.MeridianLosslessPacking {
    /// <summary>
    /// Parses MLP metadata.
    /// </summary>
    partial class MLPHeader {
        /// <summary>
        /// Audio samples per second.
        /// </summary>
        public int SampleRate { get; private set; }

        /// <summary>
        /// Bed channels of the track: either the actual channels or the downmix of spatial data.
        /// </summary>
        public ReferenceChannel[] Beds { get; private set; }

        /// <summary>
        /// If the stream carries a <=16 channel presentation, this is the actually used channel count, otherwise 0.
        /// </summary>
        public int FullChannelCount { get; private set; }

        /// <summary>
        /// Reads an MLP header from a bitstream.
        /// </summary>
        /// <remarks>Has to read a calculated number of bytes from the source stream.</remarks>
        /// <returns>A <see cref="BitExtractor"/> that continues at the beginning of the first substream
        /// or null if the substream is invalid or the end of stream is reached.</returns>
        public BitExtractor Decode(BlockBuffer<byte> reader) {
            BitExtractor extractor = new BitExtractor(reader.Read(MeridianLosslessPackingConsts.mustDecode));
            extractor.Skip(4); // Check nibble - will be useful later
            int accessUnitLength = extractor.Read(12); // In words
            extractor.Skip(16); // Input timing
            extractor.Expand(reader.Read(accessUnitLength * sizeof(short) - MeridianLosslessPackingConsts.mustDecode));
            ReadMajorSync(extractor);
            return extractor;
        }

        /// <summary>
        /// Parse general stream metadata.
        /// </summary>
        void ReadMajorSync(BitExtractor extractor) {
            if (extractor.Read(32) != MeridianLosslessPackingConsts.syncWord) {
                throw new SyncException();
            }
            ParseSampleRate(extractor.Read(4));
            extractor.Skip(4); // Reserved
            extractor.Skip(4); // Downmix modifiers - Cavern doesn't care
            Beds = ParseChannelMask(extractor.Read(5)); // 6 channel mapping
            extractor.Skip(2); // Same modifiers for 8 channels
            int channelMask8 = extractor.Read(13); // 8 channel mapping
            if (channelMask8 != 0) {
                Beds = ParseChannelMask(channelMask8);
            }

            if (extractor.Read(16) != MeridianLosslessPackingConsts.majorSyncSignature) {
                throw new SyncException();
            }
            extractor.Skip(16); // Flags
            extractor.Skip(16); // Reserved
            extractor.Skip(16); // Data rate

            int substreams = extractor.Read(4);
            extractor.Skip(2); // Reserved
            extractor.Skip(2); // Extended substream info
            int substreamInfo = extractor.Read(8);
            if ((substreamInfo & (1 << 7)) != 0) {
                FullChannelCount = 16; // 16 channel presentation present
            }
            for (int i = 0; i < substreams; i++) {
                extractor.Skip(63); // Channel meaning
                if (extractor.ReadBit()) { // Extra channel meaning
                    int extraChannelMeaningLength = (extractor.Read(4) + 1) * sizeof(short);
                    int readFrom = extractor.Position;
                    if (FullChannelCount != 0) { // 16 channel meaning
                        extractor.Skip(11); // Mixing levels
                        FullChannelCount = extractor.Read(5) + 1;
                        if (extractor.ReadBit()) { // Objects only
                            Beds = extractor.ReadBit() ? // LFE present
                                new ReferenceChannel[] { ReferenceChannel.ScreenLFE } :
                                Array.Empty<ReferenceChannel>();
                        }
                    }
                    extractor.Position = readFrom + extraChannelMeaningLength;
                }
            }
        }
    }
}

using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Remapping;
using static Cavern.Format.InOut.EnhancedAC3;

namespace Cavern.Format.InOut {
    /// <summary>
    /// Read or write an E-AC-3 header.
    /// </summary>
    partial class EnhancedAC3Header {
        /// <summary>
        /// Channel mode ID, determines the channel layout (acmod).
        /// </summary>
        public int ChannelMode { get; private set; }

        /// <summary>
        /// Number of blocks per audio frame (numblks).
        /// </summary>
        public int Blocks { get; private set; }

        /// <summary>
        /// Low Frequency Effects channel enabled (lfeon).
        /// </summary>
        public bool LFE { get; private set; }

        /// <summary>
        /// Decoded sampling rate.
        /// </summary>
        public int SampleRate { get; private set; }

        /// <summary>
        /// Sampling rate code (fscod).
        /// </summary>
        public int SampleRateCode { get; private set; }

        /// <summary>
        /// Number of 16-bit words in this frame (words_per_syncframe).
        /// </summary>
        public int WordsPerSyncframe { get; private set; }

        /// <summary>
        /// Length of <see cref="channelMapping"/>.
        /// </summary>
        const int channelMappingBits = 16;

        /// <summary>
        /// Enable the use of <see cref="channelMapping"/> (chanmape).
        /// </summary>
        bool channelMappingEnabled;

        /// <summary>
        /// Bitstream mode, information about the type of the contained audio data.
        /// </summary>
        int bsmod;

        /// <summary>
        /// One bit for each active channel, the channels are in <see cref="channelMappingTargets"/> (chanmap).
        /// </summary>
        int channelMapping;

        /// <summary>
        /// AC-3 frame size code.
        /// </summary>
        int frmsizecod;

        /// <summary>
        /// Number of the substream. 0 marks the beginning of a new timeslot, incremented values overwrite previous frames.
        /// </summary>
        int substreamid;

        /// <summary>
        /// Type of the last decoded substream.
        /// </summary>
        public StreamTypes StreamType { get; private set; }

        /// <summary>
        /// Used decoder type.
        /// </summary>
        EnhancedAC3.Decoders decoder;

        /// <summary>
        /// Reads an E-AC-3 header from a bitstream.
        /// </summary>
        /// <remarks>Has to read a calculated number of bytes from the source stream.</remarks>
        /// <returns>A <see cref="BitExtractor"/> that continues at the beginning of the audio frame.</returns>
        public BitExtractor Decode(BlockBuffer<byte> reader) {
            BitExtractor extractor = new BitExtractor(reader.Read(mustDecode));
            if (extractor.Read(16) != syncWord)
                throw new SyncException();

            StreamType = (StreamTypes)extractor.Read(2);
            substreamid = extractor.Read(3);
            WordsPerSyncframe = extractor.Read(11) + 1;
            SampleRateCode = extractor.Read(2);
            Blocks = numberOfBlocks[extractor.Read(2)];
            ChannelMode = extractor.Read(3);
            LFE = extractor.ReadBit();
            decoder = ParseDecoder(extractor.Read(5));

            if (decoder != EnhancedAC3.Decoders.EAC3) {
                StreamType = StreamTypes.Repackaged;
                Blocks = 6;
                SampleRateCode = extractor[4] >> 6;
                frmsizecod = extractor[4] & 63;
                WordsPerSyncframe = frameSizes[frmsizecod >> 1];
                if (SampleRateCode == 1) { // 44.1 kHz
                    WordsPerSyncframe = WordsPerSyncframe * 1393 / 1280;
                    if ((frmsizecod & 1) == 1)
                        ++WordsPerSyncframe;
                } else if (SampleRateCode == 2)
                    WordsPerSyncframe = WordsPerSyncframe * 3 / 2;
                bsmod = extractor.Read(3);
                ChannelMode = extractor.Read(3);
            }
            extractor.Expand(reader.Read(WordsPerSyncframe * 2 - mustDecode));

            if (StreamType == StreamTypes.Reserved)
                throw new ReservedValueException("strmtyp");
            if (SampleRateCode == 3)
                throw new ReservedValueException("fscod");
            SampleRate = sampleRates[SampleRateCode];

            switch (decoder) {
                case EnhancedAC3.Decoders.AlternateAC3:
                    HeaderAlternativeAC3(extractor);
                    break;
                case EnhancedAC3.Decoders.AC3:
                    throw new UnsupportedFeatureException("legacy");
                case EnhancedAC3.Decoders.EAC3:
                    HeaderEAC3(extractor);
                    break;
            }

            return extractor;
        }

        /// <summary>
        /// Gets the channels contained in the stream in order.
        /// </summary>
        public ReferenceChannel[] GetChannelArrangement() {
            ReferenceChannel[] channels = (ReferenceChannel[])channelArrangements[ChannelMode].Clone();
            if (channelMappingEnabled) {
                int channel = 0;
                for (int i = channelMappingBits - 1; i > 0; --i) {
                    if (((channelMapping >> i) & 1) == 1) {
                        for (int j = 0; j < channelMappingTargets[i].Length; ++j) {
                            if (channel == channels.Length)
                                throw new CorruptionException("chanmap");
                            channels[channel++] = channelMappingTargets[i][j];
                        }
                    }
                }
            }
            return channels;
        }

        int dialnorm;
        bool compre;
        int compr;
        int dialnorm2;
        bool compr2e;
        int compr2;

        int cmixlev;
        int surmixlev;

        /// <summary>
        /// Decodes the alternative AC-3 header after the ID of the decoder.
        /// </summary>
        void HeaderAlternativeAC3(BitExtractor extractor) {
            if ((ChannelMode & 0x1) != 0 && (ChannelMode != 0x1)) // 3 fronts exist
                cmixlev = extractor.Read(2);
            if ((ChannelMode & 0x4) != 0) // Surrounds exist
                surmixlev = extractor.Read(2);
            if (ChannelMode == 0x2) // Stereo
                dsurmod = extractor.Read(2);
            LFE = extractor.ReadBit();
            dialnorm = extractor.Read(5);
            if (compre = extractor.ReadBit())
                compr = extractor.Read(8);
            // TODO
        }

        bool blkid;
        bool convsync;

        /// <summary>
        /// Decodes the E-AC-3 header after the ID of the decoder.
        /// </summary>
        void HeaderEAC3(BitExtractor extractor) {
            dialnorm = extractor.Read(5);
            if (compre = extractor.ReadBit())
                compr = extractor.Read(8);

            if (ChannelMode == 0) {
                dialnorm2 = extractor.Read(5);
                if (compr2e = extractor.ReadBit())
                    compr2 = extractor.Read(8);
            }

            if (StreamType == StreamTypes.Dependent && (channelMappingEnabled = extractor.ReadBit()))
                channelMapping = extractor.Read(channelMappingBits);
            ReadMixingMetadata(extractor);
            ReadInfoMetadata(extractor);
            if (StreamType == StreamTypes.Independent && Blocks != 6)
                convsync = extractor.ReadBit();
            if (StreamType == StreamTypes.Repackaged && (blkid = Blocks == 6 || extractor.ReadBit()))
                frmsizecod = extractor.Read(6);

            if (extractor.ReadBit()) // Additional bit stream information (addbsie, omitted)
                extractor.Skip((extractor.Read(6) + 1) * 8);
        }

        /// <summary>
        /// Decoder version check.
        /// </summary>
        static EnhancedAC3.Decoders ParseDecoder(int bsid) {
            if (bsid == (int)EnhancedAC3.Decoders.AlternateAC3)
                return EnhancedAC3.Decoders.AlternateAC3;
            if (bsid <= (int)EnhancedAC3.Decoders.AC3)
                return EnhancedAC3.Decoders.AC3;
            if (bsid == (int)EnhancedAC3.Decoders.EAC3)
                return EnhancedAC3.Decoders.EAC3;
            else
                throw new UnsupportedFeatureException("decoder " + bsid);
        }
    }
}
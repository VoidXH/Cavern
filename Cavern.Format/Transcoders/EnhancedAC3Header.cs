using System;

using Cavern.Channels;
using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Utilities;

using static Cavern.Format.Transcoders.EnhancedAC3;

namespace Cavern.Format.Transcoders {
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
        /// Number of the substream (substreamid).
        /// 0 marks the beginning of a new timeslot, incremented values overwrite previous frames.
        /// </summary>
        public int SubstreamID { get; set; }

        /// <summary>
        /// Number of 16-bit words in this frame (words_per_syncframe).
        /// </summary>
        public int WordsPerSyncframe { get; private set; }

        /// <summary>
        /// Length of <see cref="channelMapping"/>.
        /// </summary>
        const int channelMappingBits = 16;

        /// <summary>
        /// Used decoder type.
        /// </summary>
        public EnhancedAC3.Decoders Decoder { get; private set; }

        /// <summary>
        /// Type of the last decoded substream.
        /// </summary>
        public StreamTypes StreamType { get; set; }

        /// <summary>
        /// Type of the last encoded substream.
        /// </summary>
        public StreamTypes StreamTypeOut { get; set; }

        /// <summary>
        /// Bitstream mode, information about the type of the contained audio data.
        /// </summary>
        int bsmod;

        /// <summary>
        /// One bit for each active channel, the channels are in <see cref="channelMappingTargets"/> (chanmap).
        /// </summary>
        /// <remarks>Null, if channel mapping is disabled (chanmape).</remarks>
        int? channelMapping;

        /// <summary>
        /// AC-3 frame size code.
        /// </summary>
        int frmsizecod;

        /// <summary>
        /// Reads an E-AC-3 header from a bitstream.
        /// </summary>
        /// <remarks>Has to read a calculated number of bytes from the source stream.</remarks>
        /// <returns>A <see cref="BitExtractor"/> that continues at the beginning of the audio frame
        /// or null if the frame is invalid or the end of stream is reached.</returns>
        public BitExtractor Decode(BlockBuffer<byte> reader) {
            BitExtractor extractor = new BitExtractor(reader.Read(mustDecode));
            if (!extractor.Readable) {
                return null;
            }

            int readSyncWord = extractor.Read(16);
            if (readSyncWord != syncWord) {
                if (readSyncWord == 0) {
                    return null;
                }
                throw new SyncException();
            }

            StreamType = (StreamTypes)extractor.Read(2);
            SubstreamID = extractor.Read(3);
            WordsPerSyncframe = extractor.Read(11) + 1;
            SampleRateCode = extractor.Read(2);
            Blocks = numberOfBlocks[extractor.Read(2)];
            ChannelMode = extractor.Read(3);
            LFE = extractor.ReadBit();
            Decoder = ParseDecoder(extractor.Read(5));

            if (Decoder != EnhancedAC3.Decoders.EAC3) {
                StreamType = StreamTypes.Repackaged;
                SubstreamID = 0;
                Blocks = 6;
                SampleRateCode = extractor[4] >> 6;
                frmsizecod = extractor[4] & 63;
                WordsPerSyncframe = frameSizes[frmsizecod >> 1];
                if (SampleRateCode == 1) { // 44.1 kHz
                    WordsPerSyncframe = WordsPerSyncframe * 1393 / 1280;
                    if ((frmsizecod & 1) == 1) {
                        ++WordsPerSyncframe;
                    }
                } else if (SampleRateCode == 2) {
                    WordsPerSyncframe += WordsPerSyncframe >> 1;
                }
                bsmod = extractor.Read(3);
                ChannelMode = extractor.Read(3);
            }
            extractor.Expand(reader.Read(WordsPerSyncframe * 2 - mustDecode));

            if (StreamType == StreamTypes.Dependent) {
                SubstreamID += 8; // There can be 8 dependent and independent substreams, both start at 0
            }
            if (StreamType == StreamTypes.Reserved) {
                throw new ReservedValueException("strmtyp");
            }
            if (SampleRateCode == 3) {
                throw new ReservedValueException("fscod");
            }
            SampleRate = sampleRates[SampleRateCode];

            channelMapping = null;
            switch (Decoder) {
                case EnhancedAC3.Decoders.AlternateAC3:
                case EnhancedAC3.Decoders.AC3:
                    ReadBitStreamInformation(extractor);
                    break;
                case EnhancedAC3.Decoders.EAC3:
                    ReadBitStreamInformationEAC3(extractor);
                    break;
            }

            StreamTypeOut = StreamType;
            return extractor;
        }

        /// <summary>
        /// Writes an E-AC-3 header to a bitstream and returns a <see cref="BitPlanter"/> with a header already in place.
        /// </summary>
        public BitPlanter Encode() {
            BitPlanter planter = new BitPlanter();
            planter.Write(syncWord, 16);
            planter.Write((int)StreamTypeOut, 2);
            planter.Write(StreamTypeOut == StreamTypes.Dependent ? SubstreamID - 8 : SubstreamID, 3);
            planter.Write(WordsPerSyncframe - 1, 11);
            planter.Write(SampleRateCode, 2);
            planter.Write(numberOfBlocks.IndexOf((byte)Blocks), 2);
            planter.Write(ChannelMode, 3);
            planter.Write(LFE);
            planter.Write((int)Decoder, 5);

            if (Decoder != EnhancedAC3.Decoders.EAC3) {
                planter.Overwrite(32, SampleRateCode, 2);
                planter.Overwrite(34, frmsizecod, 6);
                planter.Write(bsmod, 3);
                planter.Write(ChannelMode, 3);
            }

            switch (Decoder) {
                case EnhancedAC3.Decoders.AlternateAC3:
                case EnhancedAC3.Decoders.AC3:
                    WriteBitStreamInformation(planter);
                    break;
                case EnhancedAC3.Decoders.EAC3:
                    WriteBitStreamInformationEAC3(planter);
                    break;
            }
            return planter;
        }

        /// <summary>
        /// Gets the channels contained in the stream in order.
        /// </summary>
        public ReferenceChannel[] GetChannelArrangement() {
            ReferenceChannel[] channels = channelArrangements[ChannelMode].FastClone();
            if (channelMapping.HasValue) {
                int channel = 0;
                for (int i = channelMappingBits - 1; i > 0; --i) {
                    if (((channelMapping >> i) & 1) == 1) {
                        for (int j = 0; j < channelMappingTargets[i].Length; ++j) {
                            if (channel == channels.Length) {
                                throw new CorruptionException("chanmap");
                            }
                            channels[channel++] = channelMappingTargets[i][j];
                        }
                    }
                }
            }
            return channels;
        }

        /// <summary>
        /// Overrides the channel mapping of the stream.
        /// </summary>
        public void SetChannelArrangement(ReferenceChannel[] layout) {
            // Check for match with the standard arrangement, which doesn't contain the LFE
            if (channelArrangements[ChannelMode].IsSubsetOf(layout)) {
                channelMapping = null;
                return;
            }

            ReferenceChannel[] invalids = new ReferenceChannel[0];
            int mapping = 0;
            for (int i = 0; i < layout.Length; i++) {
                bool found = false;
                for (int bit = 0; bit < channelMappingTargets.Length; bit++) {
                    if (channelMappingTargets[bit].Contains(layout[i])) {
                        int addition = 1 << bit;
                        if (addition > mapping && mapping != 0) {
                            throw new InvalidChannelOrderException();
                        }
                        if ((mapping & addition) != 0) {
                            addition = 0;
                        }

                        mapping += addition;
                        found = true;
                        break;
                    }
                }
                if (!found) {
                    Array.Resize(ref invalids, invalids.Length + 1);
                    invalids[^1] = layout[i];
                }
            }
            if (invalids.Length != 0) {
                throw new InvalidChannelException(invalids);
            }

            if (!channelMapping.HasValue) {
                WordsPerSyncframe += 2; // 17 bits have to be added, this makes sure there's enough space for that
                channelMapping = mapping;
            }
        }

        /// <summary>
        /// Decoder version check.
        /// </summary>
        static EnhancedAC3.Decoders ParseDecoder(int bsid) {
            if (bsid == (int)EnhancedAC3.Decoders.AlternateAC3) {
                return EnhancedAC3.Decoders.AlternateAC3;
            }
            if (bsid <= (int)EnhancedAC3.Decoders.AC3) {
                return EnhancedAC3.Decoders.AC3;
            }
            if (bsid == (int)EnhancedAC3.Decoders.EAC3) {
                return EnhancedAC3.Decoders.EAC3;
            }
            throw new UnsupportedFeatureException("decoder " + bsid);
        }
    }
}
using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Header {
        int? langcod;
        int? langcod2;

        /// <summary>
        /// Additional downmixing information (dmixmod, ltrtcmixlev, ltrtsurmixlev, lorocmixlev, lorosurmixlev).
        /// </summary>
        /// <remarks>Contains timecod1, if the header is not alternative.</remarks>
        int? additionalDownmixInfo;

        /// <summary>
        /// Additional mixing information (dsurexmod, dheadphonmod, adconvtyp, xbsi2, encinfo).
        /// </summary>
        /// <remarks>Contains timecod2, if the header is not alternative.</remarks>
        int? additionalMixInfo;

        /// <summary>
        /// Decodes the legacy or alternative AC-3 header after the ID of the decoder.
        /// </summary>
        void ReadBitStreamInformation(BitExtractor extractor) {
            if ((ChannelMode & 0x1) != 0 && (ChannelMode != 0x1)) { // 3 fronts exist
                centerDownmix = (byte)extractor.Read(2);
            }
            if ((ChannelMode & 0x4) != 0) { // Surrounds exist
                surroundDownmix = (byte)extractor.Read(2);
            }
            if (ChannelMode == 0x2) { // Stereo
                dsurmod = extractor.Read(2);
            }

            LFE = extractor.ReadBit();
            dialnorm = extractor.Read(5);
            compr = extractor.ReadConditional(8);
            langcod = extractor.ReadConditional(8);
            if (audprodie = extractor.ReadBit()) {
                mixlevel = (byte)extractor.Read(5);
                roomtyp = extractor.Read(2);
            }

            if (ChannelMode == 0) {
                dialnorm2 = extractor.Read(5);
                compr2 = extractor.ReadConditional(8);
                langcod2 = extractor.ReadConditional(8);
                if (audprodi2e = extractor.ReadBit()) {
                    mixlevel2 = extractor.Read(5);
                    roomtyp2 = extractor.Read(2);
                }
            }

            copyrightBit = extractor.ReadBit();
            originalBitstream = extractor.ReadBit();
            additionalDownmixInfo = extractor.ReadConditional(14);
            additionalMixInfo = extractor.ReadConditional(14);
            if (addbsie = extractor.ReadBit()) {
                addbsilen = 0;
                extractor.ReadBytesInto(ref addbsi, ref addbsilen, extractor.Read(6) + 1);
            }
        }

        /// <summary>
        /// Encodes the legacy or alternative AC-3 header after the ID of the decoder.
        /// </summary>
        void WriteBitStreamInformation(BitPlanter planter) {
            if ((ChannelMode & 0x1) != 0 && (ChannelMode != 0x1)) { // 3 fronts exist
                planter.Write(centerDownmix, 2);
            }
            if ((ChannelMode & 0x4) != 0) { // Surrounds exist
                planter.Write(surroundDownmix, 2);
            }
            if (ChannelMode == 0x2) { // Stereo
                planter.Write(dsurmod, 2);
            }

            planter.Write(LFE);
            planter.Write(dialnorm, 5);
            planter.Write(compr, 8);
            planter.Write(langcod, 8);
            planter.Write(audprodie);
            if (audprodie) {
                planter.Write(mixlevel, 5);
                planter.Write(roomtyp, 2);
            }

            if (ChannelMode == 0) {
                planter.Write(dialnorm2, 5);
                planter.Write(compr2, 8);
                planter.Write(langcod2, 8);
                planter.Write(audprodi2e);
                if (audprodi2e) {
                    planter.Write(mixlevel2, 5);
                    planter.Write(roomtyp2, 2);
                }
            }

            planter.Write(copyrightBit);
            planter.Write(originalBitstream);
            planter.Write(additionalDownmixInfo, 14);
            planter.Write(additionalMixInfo, 14);
            planter.Write(addbsie);
            if (addbsie) {
                planter.Write(addbsi, addbsilen);
            }
        }
    }
}
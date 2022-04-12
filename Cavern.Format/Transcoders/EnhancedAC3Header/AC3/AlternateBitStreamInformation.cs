using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Header {
        int? langcod;
        int? langcod2;

        /// <summary>
        /// Additional downmixing information (dmixmod, ltrtcmixlev, ltrtsurmixlev, lorocmixlev, lorosurmixlev).
        /// </summary>
        int? additionalDownmixInfo;

        /// <summary>
        /// Additional mixing information (dsurexmod, dheadphonmod, adconvtyp, xbsi2, encinfo).
        /// </summary>
        int? additionalMixInfo;

        /// <summary>
        /// Decodes the alternative AC-3 header after the ID of the decoder.
        /// </summary>
        void AlternateBitStreamInformation(BitExtractor extractor) {
            if ((ChannelMode & 0x1) != 0 && (ChannelMode != 0x1)) // 3 fronts exist
                centerDownmix = extractor.Read(2);
            if ((ChannelMode & 0x4) != 0) // Surrounds exist
                surroundDownmix = extractor.Read(2);
            if (ChannelMode == 0x2) // Stereo
                dsurmod = extractor.Read(2);

            LFE = extractor.ReadBit();
            dialnorm = extractor.Read(5);
            compr = extractor.ReadConditional(8);
            langcod = extractor.ReadConditional(8);
            if (audprodie = extractor.ReadBit()) {
                mixlevel = extractor.Read(5);
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

            copyrightb = extractor.ReadBit();
            origbs = extractor.ReadBit();
            additionalDownmixInfo = extractor.ReadConditional(14);
            additionalMixInfo = extractor.ReadConditional(14);
            if (addbsie = extractor.ReadBit())
                addbsi = extractor.ReadBytes(extractor.Read(6) + 1);
        }
    }
}
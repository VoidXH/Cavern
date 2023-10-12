using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Header {
        /// <summary>
        /// The informational metadata block is contained in this header.
        /// </summary>
        bool informationalMetadataEnabled;

        /// <summary>
        /// The content is copyright protected.
        /// </summary>
        bool copyrightBit;

        /// <summary>
        /// The bitstream is unaltered.
        /// </summary>
        bool originalBitstream;

        int dsurmod;
        int dheadphonmod;
        int dsurexmod;
        bool audprodie;
        byte mixlevel;
        int roomtyp;
        bool adconvtyp;
        bool audprodi2e;
        int mixlevel2;
        int roomtyp2;
        bool adconvtyp2;
        bool sourcefscod;

        /// <summary>
        /// Parse informational metadata.
        /// </summary>
        void ReadInfoMetadata(BitExtractor extractor) {
            if (!(informationalMetadataEnabled = extractor.ReadBit())) {
                return;
            }

            bsmod = extractor.Read(3);
            copyrightBit = extractor.ReadBit();
            originalBitstream = extractor.ReadBit();
            if (ChannelMode == 2) {
                dsurmod = extractor.Read(2);
                dheadphonmod = extractor.Read(2);
            } else if (ChannelMode >= 6) {
                dsurexmod = extractor.Read(2);
            }
            if (audprodie = extractor.ReadBit()) {
                mixlevel = (byte)extractor.Read(5);
                roomtyp = extractor.Read(2);
                adconvtyp = extractor.ReadBit();
            }
            if (ChannelMode == 0) {
                if (audprodi2e = extractor.ReadBit()) {
                    mixlevel2 = extractor.Read(5);
                    roomtyp2 = extractor.Read(2);
                    adconvtyp2 = extractor.ReadBit();
                }
            }
            if (SampleRateCode < 3) {
                sourcefscod = extractor.ReadBit();
            }
        }

        /// <summary>
        /// Export informational metadata.
        /// </summary>
        void WriteInfoMetadata(BitPlanter planter) {
            planter.Write(informationalMetadataEnabled);
            if (!informationalMetadataEnabled) {
                return;
            }

            planter.Write(bsmod, 3);
            planter.Write(copyrightBit);
            planter.Write(originalBitstream);
            if (ChannelMode == 2) {
                planter.Write(dsurmod, 2);
                planter.Write(dheadphonmod, 2);
            } else if (ChannelMode >= 6) {
                planter.Write(dsurexmod, 2);
            }
            planter.Write(audprodie);
            if (audprodie) {
                planter.Write(mixlevel, 5);
                planter.Write(roomtyp, 2);
                planter.Write(adconvtyp);
            }
            if (ChannelMode == 0) {
                planter.Write(audprodi2e);
                if (audprodi2e) {
                    planter.Write(mixlevel2, 5);
                    planter.Write(roomtyp2, 2);
                    planter.Write(adconvtyp2);
                }
            }
            if (SampleRateCode < 3) {
                planter.Write(sourcefscod);
            }
        }
    }
}
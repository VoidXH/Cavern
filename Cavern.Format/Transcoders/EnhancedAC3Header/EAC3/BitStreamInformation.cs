using Cavern.Format.Utilities;
using static Cavern.Format.Transcoders.EnhancedAC3;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Header {
        int dialnorm;
        int? compr;
        int dialnorm2;
        int? compr2;
        bool blkid;
        bool convsync;
        bool addbsie;
        byte[] addbsi;

        /// <summary>
        /// Decodes the E-AC-3 header after the ID of the decoder.
        /// </summary>
        void BitStreamInformationEAC3(BitExtractor extractor) {
            dialnorm = extractor.Read(5);
            compr = extractor.ReadConditional(8);

            if (ChannelMode == 0) {
                dialnorm2 = extractor.Read(5);
                compr2 = extractor.ReadConditional(8);
            }

            if (StreamType == StreamTypes.Dependent)
                channelMapping = extractor.ReadConditional(channelMappingBits);
            ReadMixingMetadata(extractor);
            ReadInfoMetadata(extractor);
            if (StreamType == StreamTypes.Independent && Blocks != 6)
                convsync = extractor.ReadBit();
            if (StreamType == StreamTypes.Repackaged && (blkid = Blocks == 6 || extractor.ReadBit()))
                frmsizecod = extractor.Read(6);

            if (addbsie = extractor.ReadBit())
                addbsi = extractor.ReadBytes(extractor.Read(6) + 1);
        }
    }
}
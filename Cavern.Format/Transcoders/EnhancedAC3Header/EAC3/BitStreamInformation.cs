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
        byte[] addbsi = new byte[0];
        int addbsilen;

        /// <summary>
        /// Decodes the E-AC-3 header after the ID of the decoder.
        /// </summary>
        void ReadBitStreamInformationEAC3(BitExtractor extractor) {
            dialnorm = extractor.Read(5);
            compr = extractor.ReadConditional(8);

            if (ChannelMode == 0) {
                dialnorm2 = extractor.Read(5);
                compr2 = extractor.ReadConditional(8);
            }

            if (StreamType == StreamTypes.Dependent) {
                channelMapping = extractor.ReadConditional(channelMappingBits);
            }
            ReadMixingMetadata(extractor);
            ReadInfoMetadata(extractor);
            if (StreamType == StreamTypes.Independent && Blocks != 6) {
                convsync = extractor.ReadBit();
            }
            if (StreamType == StreamTypes.Repackaged && (blkid = Blocks == 6 || extractor.ReadBit())) {
                frmsizecod = extractor.Read(6);
            }

            if (addbsie = extractor.ReadBit()) {
                addbsilen = 0;
                extractor.ReadBytesInto(ref addbsi, ref addbsilen, extractor.Read(6) + 1);
            }
        }

        /// <summary>
        /// Encodes the E-AC-3 header after the ID of the decoder.
        /// </summary>
        void WriteBitStreamInformationEAC3(BitPlanter planter) {
            planter.Write(dialnorm, 5);
            planter.Write(compr, 8);

            if (ChannelMode == 0) {
                planter.Write(dialnorm2, 5);
                planter.Write(compr2, 8);
            }

            if (StreamTypeOut == StreamTypes.Dependent) {
                planter.Write(channelMapping, channelMappingBits);
            }
            WriteMixingMetadata(planter);
            WriteInfoMetadata(planter);
            if (StreamTypeOut == StreamTypes.Independent && Blocks != 6) {
                planter.Write(convsync);
            }
            if (StreamTypeOut == StreamTypes.Repackaged) {
                if (Blocks != 6) {
                    planter.Write(blkid);
                }
                if (blkid) {
                    planter.Write(frmsizecod, 6);
                }
            }

            planter.Write(addbsie);
            if (addbsie) {
                planter.Write(addbsi, addbsilen);
            }
        }
    }
}
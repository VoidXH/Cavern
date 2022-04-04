using Cavern.Format.Common;
using Cavern.Format.Utilities;
using static Cavern.Format.InOut.EnhancedAC3;

namespace Cavern.Format.InOut {
    partial class EnhancedAC3Header {
        bool mixmdate;
        int dmixmod;
        int ltrtcmixlev;
        int lorocmixlev;
        int ltrtsurmixlev;
        int lorosurmixlev;
        bool lfemixlevcode;
        int lfemixlevcod;
        bool pgmscle;
        int pgmscl;
        bool pgmscl2e;
        int pgmscl2;
        bool extpgmscle;
        int extpgmscl;
        int mixdef;
        bool frmmixcfginfoe;
        int[] blkmixcfginfo = new int[0];

        /// <summary>
        /// Parse mixing and mapping metadata.
        /// </summary>
        void ReadMixingMetadata(BitExtractor extractor) {
            if (!(mixmdate = extractor.ReadBit()))
                return;

            if (ChannelMode > 2)
                dmixmod = extractor.Read(2);
            if (((ChannelMode & 1) != 0) && (ChannelMode > 2)) { // 3 front channels present
                ltrtcmixlev = extractor.Read(3);
                lorocmixlev = extractor.Read(3);
            }
            if ((ChannelMode & 0x4) != 0) { // Surround present
                ltrtsurmixlev = extractor.Read(3);
                lorosurmixlev = extractor.Read(3);
            }
            if (LFE && (lfemixlevcode = extractor.ReadBit())) // LFE present
                lfemixlevcod = extractor.Read(5);

            if (StreamType == StreamTypes.Independent) {
                if (pgmscle = extractor.ReadBit())
                    pgmscl = extractor.Read(6);
                if (ChannelMode == 0 && (pgmscl2e = extractor.ReadBit()))
                    pgmscl2 = extractor.Read(6);
                if (extpgmscle = extractor.ReadBit())
                    extpgmscl = extractor.Read(6);
                mixdef = extractor.Read(2);
                if (mixdef != 0)
                    throw new UnsupportedFeatureException("mixdef");
                if (ChannelMode < 2)
                    throw new UnsupportedFeatureException("mono");
                if (frmmixcfginfoe = extractor.ReadBit()) { // Mixing configuration information
                    if (blkmixcfginfo.Length != Blocks)
                        blkmixcfginfo = new int[Blocks];
                    if (Blocks == 1)
                        blkmixcfginfo[0] = extractor.Read(5);
                    else
                        for (int block = 0; block < Blocks; ++block)
                            blkmixcfginfo[block] = extractor.ReadBit() ? extractor.Read(5) : 0;
                }
            }
        }
    }
}
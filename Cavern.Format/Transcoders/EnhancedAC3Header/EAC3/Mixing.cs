using Cavern.Format.Utilities;
using static Cavern.Format.Transcoders.EnhancedAC3;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Header {
        /// <summary>
        /// Mixing metadata is present and should be used (mixmdate).
        /// </summary>
        bool mixingEnabled;

        /// <summary>
        /// Center downmixing level (cmixlev).
        /// When using E-AC-3, both Lt/Rt and Lo/Ro modes are contained (ltrtcmixlev and lorocmixlev).
        /// </summary>
        byte centerDownmix;

        /// <summary>
        /// Surround downmixing level (surmixlev).
        /// When using E-AC-3, both Lt/Rt and Lo/Ro modes are contained (ltrtsurmixlev and lorosurmixlev).
        /// </summary>
        byte surroundDownmix;

        int dmixmod;
        int? lfemixlevcod;
        int? pgmscl;
        int? pgmscl2;
        int? extpgmscl;
        int mixdef;
        int mixdata;
        byte[] mixdataLarge = new byte[0];
        int mixdataLargeLen;
        bool paninfoe;
        int panmean;
        int paninfo;
        bool paninfo2e;
        int panmean2;
        int paninfo2;
        bool frmmixcfginfoe;
        int?[] blkmixcfginfo = new int?[0];

        /// <summary>
        /// Parse mixing and mapping metadata.
        /// </summary>
        void ReadMixingMetadata(BitExtractor extractor) {
            if (!(mixingEnabled = extractor.ReadBit())) {
                return;
            }

            if (ChannelMode > 2) {
                dmixmod = extractor.Read(2);
            }
            if (((ChannelMode & 1) != 0) && (ChannelMode > 2)) { // 3 front channels present
                centerDownmix = (byte)extractor.Read(6);
            }
            if ((ChannelMode & 0x4) != 0) { // Surround present
                surroundDownmix = (byte)extractor.Read(6);
            }
            if (LFE) {
                lfemixlevcod = extractor.ReadConditional(5);
            }

            if (StreamType == StreamTypes.Independent) {
                pgmscl = extractor.ReadConditional(6);
                if (ChannelMode == 0) {
                    pgmscl2 = extractor.ReadConditional(6);
                }
                extpgmscl = extractor.ReadConditional(6);

                mixdef = extractor.Read(2);
                if (mixdef == 1) {
                    mixdata = extractor.Read(5);
                } else if (mixdef == 2) {
                    mixdata = extractor.Read(12);
                } else if (mixdef == 3) {
                    mixdataLargeLen = 0;
                    extractor.ReadBytesInto(ref mixdataLarge, ref mixdataLargeLen, extractor.Read(5) + 2);
                }

                if (ChannelMode < 2) {
                    if (paninfoe = extractor.ReadBit()) {
                        panmean = extractor.Read(8);
                        paninfo = extractor.Read(6);
                    }
                    if (ChannelMode == 0 && (paninfo2e = extractor.ReadBit())) {
                        panmean2 = extractor.Read(8);
                        paninfo2 = extractor.Read(6);
                    }
                }

                // Mixing configuration information
                if (frmmixcfginfoe = extractor.ReadBit()) {
                    if (blkmixcfginfo.Length != Blocks) {
                        blkmixcfginfo = new int?[Blocks];
                    }
                    if (Blocks == 1) {
                        blkmixcfginfo[0] = extractor.Read(5);
                    } else {
                        for (int block = 0; block < Blocks; block++) {
                            blkmixcfginfo[block] = extractor.ReadConditional(5);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Export mixing and mapping metadata.
        /// </summary>
        void WriteMixingMetadata(BitPlanter planter) {
            planter.Write(mixingEnabled);
            if (!mixingEnabled) {
                return;
            }

            if (ChannelMode > 2) {
                planter.Write(dmixmod, 2);
            }
            if (((ChannelMode & 1) != 0) && (ChannelMode > 2)) { // 3 front channels present
                planter.Write(centerDownmix, 6);
            }
            if ((ChannelMode & 0x4) != 0) { // Surround present
                planter.Write(surroundDownmix, 6);
            }
            if (LFE) {
                planter.Write(lfemixlevcod, 5);
            }

            if (StreamTypeOut == StreamTypes.Independent) {
                planter.Write(pgmscl, 6);
                if (ChannelMode == 0) {
                    planter.Write(pgmscl2, 6);
                }
                planter.Write(extpgmscl, 6);

                planter.Write(mixdef, 2);
                if (mixdef == 1) {
                    planter.Write(mixdata, 5);
                } else if (mixdef == 2) {
                    planter.Write(mixdata, 12);
                } else if (mixdef == 3) {
                    planter.Write(mixdataLarge, mixdataLargeLen);
                }

                if (ChannelMode < 2) {
                    planter.Write(paninfoe);
                    if (paninfoe) {
                        planter.Write(panmean, 8);
                        planter.Write(paninfo, 6);
                    }
                    if (ChannelMode == 0) {
                        planter.Write(paninfo2e);
                        if (paninfo2e) {
                            planter.Write(panmean2, 8);
                            planter.Write(paninfo2, 6);
                        }
                    }
                }

                // Mixing configuration information
                planter.Write(frmmixcfginfoe);
                if (frmmixcfginfoe) {
                    if (Blocks == 1) {
                        planter.Write(blkmixcfginfo[0].Value, 5);
                    } else {
                        for (int block = 0; block < Blocks; block++) {
                            planter.Write(blkmixcfginfo[block], 5);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get the center downmix gain when downmixing in Lt/Rt mode.
        /// </summary>
        float GetCenterDownmixLtRt() => Decoder == EnhancedAC3.Decoders.EAC3 ? GetCenterDownmix(centerDownmix >> 3) : GetCenterDownmixAC3();

        /// <summary>
        /// Get the center downmix gain when downmixing in Lt/Rt mode.
        /// </summary>
        float GetCenterDownmixLoRo() => Decoder == EnhancedAC3.Decoders.EAC3 ? GetCenterDownmix(centerDownmix & 0x7) : GetCenterDownmixAC3();

        /// <summary>
        /// Get the center downmix gain from an E-AC3 code.
        /// </summary>
        float GetCenterDownmix(int code) {
            return code switch {
                0 => 1.414f,
                2 => 1.189f,
                3 => 1,
                4 => .841f,
                5 => .707f,
                6 => .595f,
                7 => .5f,
                _ => 0
            };
        }

        /// <summary>
        /// Get the center downmix gain from an AC3 code.
        /// </summary>
        float GetCenterDownmixAC3() {
            return centerDownmix switch {
                0 => .707f,
                2 => .5f,
                _ => .595f
            };
        }
    }
}
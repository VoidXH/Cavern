using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Body {
        /// <summary>
        /// Read spectral extension metadata for a given <paramref name="block"/>.
        /// </summary>
        void ReadSPX(int block) {
            if (spxstre = block == 0 || extractor.ReadBit()) {
                if (spxinu = extractor.ReadBit()) {
                    if (header.ChannelMode == 1) {
                        chinspx[0] = true;
                    } else {
                        for (int channel = 0; channel < channels.Length; channel++) {
                            chinspx[channel] = extractor.ReadBit();
                        }
                    }
                    spxstrtf = extractor.Read(2);
                    spxbegf = extractor.Read(3);
                    spxendf = extractor.Read(3);
                    spx_begin_subbnd = spxbegf < 6 ? spxbegf + 2 : (spxbegf * 2 - 3);
                    spx_end_subbnd = spxendf < 3 ? spxendf + 5 : (spxendf * 2 + 3);
                    if (spxbndstrce = extractor.ReadBit()) {
                        spxbndstrc = new bool[spx_end_subbnd];
                        for (int band = spx_begin_subbnd + 1; band < spx_end_subbnd; band++) {
                            spxbndstrc[band] = extractor.ReadBit();
                        }
                    }

                    ParseSPX();
                } else {
                    ClearSPX();
                }
            }

            // Coordinates
            if (spxinu) {
                for (int channel = 0; channel < channels.Length; channel++) {
                    if (chinspx[channel]) {
                        if (firstspxcos[channel]) {
                            spxcoe[channel] = true;
                            firstspxcos[channel] = false;
                        } else {
                            spxcoe[channel] = extractor.ReadBit();
                        }

                        if (spxcoe[channel]) {
                            spxblnd[channel] = extractor.Read(5);
                            mstrspxco[channel] = extractor.Read(2);
                            for (int band = 0; band < nspxbnds; band++) {
                                spxcoexp[channel][band] = extractor.Read(4);
                                spxcomant[channel][band] = extractor.Read(2);
                            }
                        }
                    } else {
                        firstspxcos[channel] = true;
                    }
                }
            }
        }

        /// <summary>
        /// Write spectral extension metadata for a given <paramref name="block"/>.
        /// </summary>
        void WriteSPX(BitPlanter planter, int block) {
            if (block != 0) {
                planter.Write(spxstre);
            }
            if (spxstre) {
                planter.Write(spxinu);
                if (spxinu) {
                    if (header.ChannelMode != 1) {
                        for (int channel = 0; channel < channels.Length; channel++) {
                            planter.Write(chinspx[channel]);
                        }
                    }
                    planter.Write(spxstrtf, 2);
                    planter.Write(spxbegf, 3);
                    planter.Write(spxendf, 3);
                    planter.Write(spxbndstrce);
                    if (spxbndstrce) {
                        spxbndstrc = new bool[spx_end_subbnd];
                        for (int band = spx_begin_subbnd + 1; band < spx_end_subbnd; band++) {
                            planter.Write(spxbndstrc[band]);
                        }
                    }
                }
            }

            // Coordinates
            if (spxinu) {
                for (int channel = 0; channel < channels.Length; channel++) {
                    if (chinspx[channel]) {
                        if (!firstspxcos[channel]) {
                            planter.Write(spxcoe[channel]);
                        }

                        if (spxcoe[channel]) {
                            planter.Write(spxblnd[channel], 5);
                            planter.Write(mstrspxco[channel], 2);
                            for (int band = 0; band < nspxbnds; band++) {
                                planter.Write(spxcoexp[channel][band], 4);
                                planter.Write(spxcomant[channel][band], 2);
                            }
                        }
                    } else {
                        firstspxcos[channel] = true;
                    }
                }
            }
        }

        /// <summary>
        /// Set up SPX as unused.
        /// </summary>
        void ClearSPX() {
            for (int channel = 0; channel < channels.Length; channel++) {
                chinspx[channel] = false;
                firstspxcos[channel] = true;
            }
        }
    }
}
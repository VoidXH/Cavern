namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Body {
        /// <summary>
        /// Read spectral extension metadata for a given <paramref name="block"/>.
        /// </summary>
        void ReadSPX(int block) {
            if (spxstre = block == 0 || extractor.ReadBit()) {
                if (extractor.ReadBit()) {
                    spxinu = true;
                    if (header.ChannelMode == 1) {
                        chinspx[0] = true;
                    } else {
                        for (int channel = 0; channel < channels.Length; ++channel) {
                            chinspx[channel] = extractor.ReadBit();
                        }
                    }
                    spxstrtf = extractor.Read(2);
                    spxbegf = extractor.Read(3);
                    spxendf = extractor.Read(3);
                    spx_begin_subbnd = spxbegf < 6 ? spxbegf + 2 : (spxbegf * 2 - 3);
                    spx_end_subbnd = spxendf < 3 ? spxendf + 5 : (spxendf * 2 + 3);
                    spxbndstrce = extractor.ReadBit();
                    if (spxbndstrce) {
                        spxbndstrc = new bool[spx_end_subbnd];
                        for (int band = spx_begin_subbnd + 1; band < spx_end_subbnd; ++band) {
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
                for (int channel = 0; channel < channels.Length; ++channel) {
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
                            for (int band = 0; band < nspxbnds; ++band) {
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
        /// Set up SPX as unused.
        /// </summary>
        void ClearSPX() {
            spxinu = false;
            for (int channel = 0; channel < channels.Length; ++channel) {
                chinspx[channel] = false;
                firstspxcos[channel] = true;
            }
        }
    }
}
namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Body {
        /// <summary>
        /// Parse spectral extension data.
        /// </summary>
        void ParseSPX() {
            nspxbnds = 1;
            spxbndsztab = new int[spx_end_subbnd];
            spxbndsztab[0] = 12;
            spxbndstrc = new bool[spx_end_subbnd];
            for (int bnd = spx_begin_subbnd + 1; bnd < spx_end_subbnd; ++bnd) {
                if (!spxbndstrc[bnd]) {
                    spxbndsztab[nspxbnds] = 12;
                    ++nspxbnds;
                } else {
                    spxbndsztab[nspxbnds - 1] += 12;
                }
            }
            for (int channel = 0; channel < channels.Length; ++channel) {
                spxcoexp[channel] = new int[nspxbnds];
                spxcomant[channel] = new int[nspxbnds];
            }
        }

        /// <summary>
        /// Set endmant and nchgrps.
        /// </summary>
        void ParseParametricBitAllocation(int block) {
            if (cplinu[block]) {
                cplstrtmant = 37 + 12 * cplbegf;
                cplendmant = 37 + 12 * (cplendf + 3);
                if (cplexpstr[block] != ExpStrat.Reuse) {
                    if (ecplinu) {
                        ecplstartmant = ecplsubbndtab[ecpl_begin_subbnd];
                        ecplendmant = ecplsubbndtab[ecpl_end_subbnd];
                        ncplgrps = (ecplendmant - ecplstartmant) / groupDiv[(int)cplexpstr[block] - 1];
                    } else {
                        ncplgrps = (cplendmant - cplstrtmant) / groupDiv[(int)cplexpstr[block] - 1];
                    }
                }
            }

            for (int channel = 0; channel < channels.Length; ++channel) {
                if (ecplinu) {
                    endmant[channel] = ecplsubbndtab[ecpl_begin_subbnd];
                } else {
                    if (spxinu && !cplinu[block]) {
                        endmant[channel] = spx_begin_subbnd * 12 + 25;
                    } else if (chincpl[channel]) {
                        endmant[channel] = cplstrtmant;
                    } else {
                        endmant[channel] = (chbwcod[channel] + 12) * 3 + 37;
                    }
                }

                int strat = (int)chexpstr[block][channel];
                if (strat != 0) {
                    nchgrps[channel] = (endmant[channel] + groupAdd[strat - 1]) / groupDiv[strat - 1];
                }
            }
        }

        /// <summary>
        /// Addition for each <see cref="ExpStrat"/> to calculate group sizes.
        /// </summary>
        static readonly sbyte[] groupAdd = { -1, 2, 8 };

        /// <summary>
        /// Divider of each <see cref="ExpStrat"/> to calculate group sizes.
        /// </summary>
        static readonly byte[] groupDiv = { 3, 6, 12 };
    }
}
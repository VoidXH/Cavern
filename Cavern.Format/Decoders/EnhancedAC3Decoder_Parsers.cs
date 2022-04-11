namespace Cavern.Format.Decoders {
    partial class EnhancedAC3Decoder {
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
                } else
                    spxbndsztab[nspxbnds - 1] += 12;
            }
            for (int channel = 0; channel < channels.Length; ++channel) {
                spxcoexp[channel] = new int[nspxbnds];
                spxcomant[channel] = new int[nspxbnds];
            }
        }

        int ParseSpxbandtable(int band) => band * 12 + 25;

        /// <summary>
        /// Set endmant and nchgrps.
        /// </summary>
        void ParseParametricBitAllocation(int block) {
            if (cplinu[block]) {
                cplstrtmant = 37 + 12 * cplbegf;
                cplendmant = 37 + 12 * (cplendf + 3);
                if (cplexpstr[block] != ExpStrat.Reuse)
                    ncplgrps = (cplendmant - cplstrtmant) / groupDiv[(int)cplexpstr[block] - 1];
            }

            for (int channel = 0; channel < channels.Length; ++channel) {
                strtmant[channel] = 0;
                if (ecplinu)
                    endmant[channel] = ecplsubbndtab[ecpl_begin_subbnd];
                else {
                    if (spxinu && !cplinu[block])
                        endmant[channel] = ParseSpxbandtable(spx_begin_subbnd);
                    else if (cplinu[block])
                        endmant[channel] = cplstrtmant;
                    else
                        endmant[channel] = (chbwcod[channel] + 12) * 3 + 37;
                }

                switch (chexpstr[block][channel]) {
                    case ExpStrat.D15:
                        nchgrps[channel] = (endmant[channel] - 1) / 3;
                        break;
                    case ExpStrat.D25:
                        nchgrps[channel] = (endmant[channel] + 2) / 6;
                        break;
                    case ExpStrat.D45:
                        nchgrps[channel] = (endmant[channel] + 8) / 12;
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// Divider of each <see cref="ExpStrat"/> for <see cref="ncplgrps"/>.
        /// </summary>
        static readonly byte[] groupDiv = { 3, 6, 12 };
    }
}
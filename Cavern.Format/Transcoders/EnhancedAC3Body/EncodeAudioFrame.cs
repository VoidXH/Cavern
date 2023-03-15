using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Utilities;

using static Cavern.Format.Transcoders.EnhancedAC3;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Body {
        /// <summary>
        /// Write the combined block header that's present in E-AC-3.
        /// </summary>
        void EncodeAudioFrame(BitPlanter planter) {
            if (header.Blocks == 6) {
                planter.Write(expstre);
                planter.Write(ahte);
            }
            planter.Write(snroffststr, 2);
            planter.Write(transproce);
            planter.Write(blkswe);
            planter.Write(dithflage);
            planter.Write(bamode);
            planter.Write(frmfgaincode);
            planter.Write(dbaflde);
            planter.Write(skipFieldSyntaxEnabled);
            planter.Write(spxattene);

            if (header.ChannelMode > 1) { // Not mono
                planter.Write(cplinu[0]);
                for (int block = 1; block < cplstre.Length; block++) {
                    planter.Write(cplstre[block]);
                    if (cplstre[block]) {
                        planter.Write(cplinu[block]);
                    }
                }
            }

            // Exponent strategy data init
            if (expstre) {
                for (int block = 0; block < cplexpstr.Length; block++) {
                    if (cplinu[block]) {
                        planter.Write((int)cplexpstr[block], 2);
                    }
                    for (int channel = 0; channel < channels.Length; channel++) {
                        planter.Write((int)chexpstr[block][channel], 2);
                    }
                }
            } else {
                int ncplblks = 0;
                for (int block = 0; block < cplinu.Length; block++) {
                    if (cplinu[block]) {
                        ++ncplblks;
                    }
                }
                if (header.ChannelMode > 1 && ncplblks > 0) {
                    planter.Write(frmcplexpstr, 5);
                }
                for (int channel = 0; channel < channels.Length; channel++) {
                    planter.Write(frmchexpstr[channel], 5);
                }
            }

            if (header.LFE) {
                for (int block = 0; block < lfeexpstr.Length; block++) {
                    planter.Write(lfeexpstr[block]);
                }
            }

            // Converter exponent strategy data
            if (header.StreamTypeOut == StreamTypes.Independent) {
                if (header.Blocks != 6) {
                    planter.Write(convexpstre);
                }
                if (convexpstre) {
                    for (int channel = 0; channel < channels.Length; channel++) {
                        planter.Write(convexpstr[channel], 5);
                    }
                }
            }

            // AHT data
            if (ahte) {
                throw new UnsupportedFeatureException("AHT");
            }

            // Audio frame SNR offset data
            if (snroffststr == 0) {
                planter.Write(frmcsnroffst, 6);
                planter.Write(frmfsnroffst, 4);
            }

            // Transient pre-noise processing data
            if (transproce) {
                for (int channel = 0; channel < channels.Length; channel++) {
                    planter.Write(chintransproc[channel]);
                    if (chintransproc[channel]) {
                        planter.Write(transprocloc[channel], 10);
                        planter.Write(transproclen[channel], 8);
                    }
                }
            }

            // Spectral extension attenuation data
            if (spxattene) {
                for (int ch = 0; ch < channels.Length; ch++) {
                    planter.Write(chinspxatten[ch]);
                    if (chinspxatten[ch]) {
                        planter.Write(spxattencod[ch], 5);
                    }
                }
            }

            if (header.Blocks != 1) {
                planter.Write(blkstrtinfoe);
                if (blkstrtinfoe) {
                    planter.Write(blkstrtinfo, (header.Blocks - 1) * (4 + QMath.Log2Ceil(header.WordsPerSyncframe)));
                }
            }

            // Syntax state init
            for (int channel = 0; channel < channels.Length; channel++) {
                firstspxcos[channel] = true;
                firstcplcos[channel] = true;
            }
            firstcplleak = true;
        }
    }
}
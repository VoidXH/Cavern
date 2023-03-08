using Cavern.Format.Common;
using Cavern.Utilities;

using static Cavern.Format.Transcoders.EnhancedAC3;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Body {
        /// <summary>
        /// For E-AC-3, data for multiple blocks is included in an audio frame header.
        /// </summary>
        void DecodeAudioFrame() {
            expstre = header.Blocks != 6 || extractor.ReadBit();
            ahte = header.Blocks == 6 && extractor.ReadBit();
            snroffststr = extractor.Read(2);
            transproce = extractor.ReadBit();
            blkswe = extractor.ReadBit();
            dithflage = extractor.ReadBit();
            bamode = extractor.ReadBit();
            frmfgaincode = extractor.ReadBit();
            dbaflde = extractor.ReadBit();
            skipFieldSyntaxEnabled = extractor.ReadBit();
            spxattene = extractor.ReadBit();

            if (header.ChannelMode > 1) { // Not mono
                cplstre[0] = true;
                cplinu[0] = extractor.ReadBit();
                for (int block = 1; block < cplstre.Length; block++) {
                    if (cplstre[block] = extractor.ReadBit()) {
                        cplinu[block] = extractor.ReadBit();
                    } else {
                        cplinu[block] = cplinu[block - 1];
                    }
                }
            } else {
                for (int block = 1; block < cplstre.Length; block++) {
                    cplinu[block] = false;
                }
            }

            // Exponent strategy data init
            if (expstre) {
                for (int block = 0; block < cplexpstr.Length; block++) {
                    if (cplinu[block]) {
                        cplexpstr[block] = (ExpStrat)extractor.Read(2);
                    }
                    for (int channel = 0; channel < channels.Length; channel++) {
                        chexpstr[block][channel] = (ExpStrat)extractor.Read(2);
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
                    frmcplexpstr = extractor.Read(5);
                }
                for (int channel = 0; channel < channels.Length; channel++) {
                    frmchexpstr[channel] = extractor.Read(5);
                }

                for (int block = 0; block < cplexpstr.Length; block++) {
                    cplexpstr[block] = frmcplexpstr_tbl[frmcplexpstr][block];
                    for (int channel = 0; channel < channels.Length; channel++) {
                        chexpstr[block][channel] = frmcplexpstr_tbl[frmchexpstr[channel]][block];
                    }
                }
            }

            if (header.LFE) {
                for (int block = 0; block < lfeexpstr.Length; block++) {
                    lfeexpstr[block] = extractor.ReadBit();
                }
            }

            // Converter exponent strategy data
            if (header.StreamType == StreamTypes.Independent &&
                (convexpstre = header.Blocks == 6 || extractor.ReadBit())) {
                for (int channel = 0; channel < channels.Length; channel++) {
                    convexpstr[channel] = extractor.Read(5);
                }
            }

            // AHT data
            if (ahte) {
                throw new UnsupportedFeatureException("AHT");
            }

            // Audio frame SNR offset data
            if (snroffststr == 0) {
                frmcsnroffst = extractor.Read(6);
                frmfsnroffst = extractor.Read(4);
            }

            // Transient pre-noise processing data
            if (transproce) {
                for (int channel = 0; channel < channels.Length; channel++) {
                    if (chintransproc[channel] = extractor.ReadBit()) {
                        transprocloc[channel] = extractor.Read(10);
                        transproclen[channel] = extractor.Read(8);
                    }
                }
            }

            // Spectral extension attenuation data
            if (spxattene) {
                for (int ch = 0; ch < channels.Length; ch++) {
                    if (chinspxatten[ch] = extractor.ReadBit()) {
                        spxattencod[ch] = extractor.Read(5);
                    }
                }
            }

            if (blkstrtinfoe = header.Blocks != 1 && extractor.ReadBit()) {
                int nblkstrtbits = (header.Blocks - 1) * (4 + QMath.Log2Ceil(header.WordsPerSyncframe));
                blkstrtinfo = extractor.Read(nblkstrtbits);
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
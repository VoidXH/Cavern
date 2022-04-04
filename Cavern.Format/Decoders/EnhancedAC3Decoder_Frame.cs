using System;

using Cavern.Format.Common;
using Cavern.Utilities;
using static Cavern.Format.InOut.EnhancedAC3;

namespace Cavern.Format.Decoders {
    // Audio frame header parsing for E-AC-3
    partial class EnhancedAC3Decoder {
        void AudioFrame() {
            expstre = true;
            if (header.Blocks == 6) {
                expstre = extractor.ReadBit();
                ahte = extractor.ReadBit();
            }

            snroffststr = extractor.Read(2);
            transproce = extractor.ReadBit();
            blkswe = extractor.ReadBit();
            dithflage = extractor.ReadBit();
            bamode = extractor.ReadBit();
            frmfgaincode = extractor.ReadBit();
            dbaflde = extractor.ReadBit();
            skipflde = extractor.ReadBit();
            spxattene = extractor.ReadBit();

            if (header.ChannelMode > 1) { // Not mono
                cplstre[0] = true;
                cplinu[0] = extractor.ReadBit();
                for (int block = 1; block < cplstre.Length; ++block) {
                    cplstre[block] = extractor.ReadBit();
                    if (cplstre[block])
                        cplinu[block] = extractor.ReadBit();
                    else
                        cplinu[block] = cplinu[block - 1];
                }
            }

            // Exponent strategy data init
            if (expstre) {
                for (int block = 0; block < cplexpstr.Length; ++block) {
                    if (cplinu[block])
                        cplexpstr[block] = (ExpStrat)extractor.Read(2);
                    for (int channel = 0; channel < channels.Length; ++channel)
                        chexpstr[block][channel] = (ExpStrat)extractor.Read(2);
                }
            } else {
                int ncplblks = 0;
                for (int block = 0; block < cplinu.Length; ++block)
                    if (cplinu[block])
                        ++ncplblks;
                if (header.ChannelMode > 1 && ncplblks > 0)
                    frmcplexpstr = extractor.Read(5);
                for (int channel = 0; channel < channels.Length; ++channel)
                    frmchexpstr[channel] = extractor.Read(5);

                for (int block = 0; block < cplexpstr.Length; ++block) {
                    cplexpstr[block] = frmcplexpstr_tbl[frmcplexpstr][block];
                    for (int channel = 0; channel < channels.Length; ++channel)
                        chexpstr[block][channel] = frmcplexpstr_tbl[frmchexpstr[channel]][block];
                }
            }

            if (header.LFE)
                for (int block = 0; block < lfeexpstr.Length; ++block)
                    lfeexpstr[block] = extractor.ReadBit();

            // Converter exponent strategy data
            if (header.StreamType == StreamTypes.Independent &&
                (convexpstre = header.Blocks == 6 || extractor.ReadBit()))
                for (int channel = 0; channel < channels.Length; ++channel)
                    convexpstr[channel] = extractor.Read(5);

            // AHT data
            if (ahte)
                throw new UnsupportedFeatureException("AHT");

            // Audio frame SNR offset data
            if (snroffststr == 0) {
                frmcsnroffst = extractor.Read(6);
                frmfsnroffst = extractor.Read(4);
            }

            // Transient pre-noise processing data
            if (transproce) {
                for (int channel = 0; channel < channels.Length; ++channel) {
                    if (chintransproc[channel] = extractor.ReadBit()) {
                        transprocloc[channel] = extractor.Read(10);
                        transproclen[channel] = extractor.Read(8);
                    }
                }
            }

            // Spectral extension attenuation data
            if (spxattene)
                for (int ch = 0; ch < channels.Length; ++ch)
                    if (chinspxatten[ch] = extractor.ReadBit())
                        spxattencod[ch] = extractor.Read(5);

            blkstrtinfoe = header.Blocks != 1 && extractor.ReadBit();
            if (blkstrtinfoe) {
                int nblkstrtbits = (header.Blocks - 1) * (4 + QMath.Log2Ceil(header.WordsPerSyncframe));
                blkstrtinfo = extractor.Read((byte)nblkstrtbits);
            }

            // Syntax state init
            for (int channel = 0; channel < channels.Length; ++channel) {
                firstspxcos[channel] = true;
                firstcplcos[channel] = true;
            }
            firstcplleak = true;

            // Clear per-frame reuse data
            Array.Clear(bap, 0, bap.Length);
            lfebap = null;
        }
    }
}
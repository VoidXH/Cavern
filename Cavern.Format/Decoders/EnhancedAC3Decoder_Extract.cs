using Cavern.Format.Common;
using Cavern.Format.Utilities;
using Cavern.Remapping;
using Cavern.Utilities;

namespace Cavern.Format.Decoders {
    // Bitstream data extraction
    internal partial class EnhancedAC3Decoder {
        void BitstreamInfo(ref BitExtractor extractor) {
            strmtyp = extractor.Read(2);
            substreamid = extractor.Read(3);
            frmsiz = extractor.Read(11);
            fscod = extractor.Read(2);
            numblkscod = extractor.Read(2);

            acmod = extractor.Read(3);
            lfeon = extractor.ReadBit();

            words_per_syncframe = frmsiz + 1;
            SampleRate = sampleRates[fscod];
            blocks = numberOfBlocks[numblkscod];
            channels = ChannelPrototype.Get(channelArrangements[acmod]);
            CreateCacheTables(blocks, channels.Length);
            extractor = new BitExtractor(reader.Read(words_per_syncframe * 2 - mustDecode));

            decoder = ParseDecoder(extractor.Read(5));
            dialnorm = extractor.Read(5);
            if (compre = extractor.ReadBit())
                compr = extractor.Read(8);

            if (acmod == 0) {
                dialnorm2 = extractor.Read(5);
                if (compr2e = extractor.ReadBit())
                    compr2 = extractor.Read(8);
            }

            if (strmtyp == 1 && (chanmape = extractor.ReadBit()))
                chanmap = extractor.Read(16);

            // Mixing and mapping metadata
            if (mixmdate = extractor.ReadBit()) {
                if (acmod > 2)
                    dmixmod = extractor.Read(2);
                if (((acmod & 1) != 0) && (acmod > 2)) { // 3 front channels present
                    ltrtcmixlev = extractor.Read(3);
                    lorocmixlev = extractor.Read(3);
                }
                if ((acmod & 0x4) != 0) { // Surround present
                    ltrtsurmixlev = extractor.Read(3);
                    lorosurmixlev = extractor.Read(3);
                }
                if (lfeon && (lfemixlevcode = extractor.ReadBit())) // LFE present
                    lfemixlevcod = extractor.Read(5);
                if (strmtyp == 0) {
                    if (pgmscle = extractor.ReadBit())
                        pgmscl = extractor.Read(6);
                    if (acmod == 0 && (pgmscl2e = extractor.ReadBit()))
                        pgmscl2 = extractor.Read(6);
                    if (extpgmscle = extractor.ReadBit())
                        extpgmscl = extractor.Read(6);
                    mixdef = extractor.Read(2);
                    if (mixdef != 0)
                        throw new UnsupportedFeatureException("mixdef");
                    if (acmod < 2)
                        throw new UnsupportedFeatureException("mono");
                    if (frmmixcfginfoe = extractor.ReadBit()) { // Mixing configuration information
                        if (numblkscod == 0)
                            blkmixcfginfo[0] = extractor.Read(5);
                        else
                            for (int block = 0; block < blocks; ++block)
                                blkmixcfginfo[block] = extractor.ReadBit() ? extractor.Read(5) : 0;
                    }
                }
            }

            if (infomdate = extractor.ReadBit()) { // Informational metadata
                bsmod = extractor.Read(3);
                if (bsmod != 0)
                    throw new UnsupportedFeatureException("bit stream modes");
                extractor.Skip(2); // Copyright & original bitstream bits
                if (acmod == 2)
                    throw new UnsupportedFeatureException("stereo");
                if (acmod >= 6 && extractor.Read(2) != 0)
                    throw new UnsupportedFeatureException("ProLogic");
                if (audprodie = extractor.ReadBit()) {
                    mixlevel = extractor.Read(5);
                    roomtyp = extractor.Read(2);
                    adconvtyp = extractor.ReadBit();
                }
                if (acmod == 0) {
                    if (audprodie2 = extractor.ReadBit()) {
                        mixlevel2 = extractor.Read(5);
                        roomtyp2 = extractor.Read(2);
                        adconvtyp2 = extractor.ReadBit();
                    }
                }
                if (fscod < 3)
                    sourcefscod = extractor.ReadBit();
            }

            if ((strmtyp == 0) && (numblkscod != 3))
                convsync = extractor.ReadBit();

            if (strmtyp == 2) {
                blkid = numblkscod == 3 || extractor.ReadBit();
                if (blkid)
                    frmsizecod = extractor.Read(6);
            }

            if (addbsie = extractor.ReadBit()) { // Additional bit stream information (omitted)
                int addbsil = extractor.Read(6);
                addbsi = extractor.ReadBytes(addbsil + 1);
            }
        }

        void AudioFrame(BitExtractor extractor) {
            expstre = true;
            if (numblkscod == 3) {
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

            if (acmod > 1) { // Not mono
                cplstre[0] = true;
                cplinu[0] = extractor.ReadBit();
                for (int block = 1; block < blocks; ++block) {
                    cplstre[block] = extractor.ReadBit();
                    if (cplstre[block])
                        cplinu[block] = extractor.ReadBit();
                    else
                        cplinu[block] = cplinu[block - 1];
                }
            }

            // Exponent strategy data init
            if (expstre) {
                for (int block = 0; block < blocks; ++block) {
                    if (cplinu[block])
                        cplexpstr[block] = (ExpStrat)extractor.Read(2);
                    for (int channel = 0; channel < channels.Length; ++channel)
                        chexpstr[block][channel] = (ExpStrat)extractor.Read(2);
                }
            } else {
                int ncplblks = 0;
                for (int block = 0; block < blocks; ++block)
                    if (cplinu[block])
                        ++ncplblks;
                if (acmod > 1 && ncplblks > 0)
                    frmcplexpstr = extractor.Read(5);
                for (int channel = 0; channel < channels.Length; ++channel)
                    frmchexpstr[channel] = extractor.Read(5);

                for (int block = 0; block < blocks; ++block) {
                    cplexpstr[block] = frmcplexpstr_tbl[frmcplexpstr][block];
                    for (int channel = 0; channel < channels.Length; ++channel)
                        chexpstr[block][channel] = frmcplexpstr_tbl[frmchexpstr[channel]][block];
                }
            }

            if (lfeon)
                for (int block = 0; block < blocks; ++block)
                    lfeexpstr[block] = extractor.ReadBit();

            // Converter exponent strategy data
            if (strmtyp == 0) {
                convexpstre = numblkscod == 3 || extractor.ReadBit();
                if (convexpstre)
                    for (int channel = 0; channel < channels.Length; ++channel)
                        convexpstr[channel] = extractor.Read(5);
            }

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
                throw new UnsupportedFeatureException("spxatten");

            blkstrtinfoe = numblkscod != 0 && extractor.ReadBit();
            if (blkstrtinfoe) {
                int nblkstrtbits = (blocks - 1) * (4 + QMath.Log2Ceil(words_per_syncframe));
                blkstrtinfo = extractor.Read(nblkstrtbits);
            }

            // Syntax state init
            for (int channel = 0; channel < channels.Length; ++channel) {
                firstspxcos[channel] = true;
                firstcplcos[channel] = true;
            }
            firstcplleak = true;
        }
    }
}
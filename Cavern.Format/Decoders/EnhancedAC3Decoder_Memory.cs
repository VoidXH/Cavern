﻿using Cavern.Remapping;

namespace Cavern.Format.Decoders {
    // These are the stored variables for the decoder. They can be infinitely reused between frames.
    internal partial class EnhancedAC3Decoder {
        bool dynrnge;
        int dynrng;
        int blkfsnroffst;
        int cplfsnroffst;
        bool fgaincode;
        int cplfgaincod;
        bool convsnroffste;
        int[] endmant;
        bool dynrng2e;
        int dynrng2;
        bool spxstre;
        bool cplbndstrce;
        bool ecplbndstrce;
        bool baie;
        int sdcycod;
        int fdcycod;
        int sgaincod;
        int dbpbcod;
        int floorcod;
        bool snroffste;
        int lfefsnroffst;
        int csnroffst;
        int lfefgaincod;
        int[] fsnroffst;
        int[] fgaincod;
        // TODO: alphabetically
        bool adconvtyp;
        bool adconvtyp2;
        bool addbsie;
        bool ahte;
        bool audprodie;
        bool audprodie2;
        bool bamode;
        bool blkid;
        bool blkstrtinfoe;
        bool blkswe;
        bool chanmape;
        bool compr2e;
        bool compre;
        bool convexpstre;
        bool convsync;
        bool dbaflde;
        bool dithflage;
        bool ecplinu;
        bool expstre;
        bool extpgmscle;
        bool firstcplleak;
        bool frmfgaincode;
        bool frmmixcfginfoe;
        bool infomdate;
        bool lfemixlevcode;
        bool lfeon;
        bool mixmdate;
        bool pgmscl2e;
        bool pgmscle;
        bool phsflginu;
        bool skipflde;
        bool sourcefscod;
        bool spxattene;
        bool spxbndstrce;
        bool spxinu;
        bool transproce;
        bool[] blksw;
        bool[] chincpl;
        bool[] chinspx;
        bool[] chintransproc;
        bool[] cplbndstrc;
        bool[] cplinu;
        bool[] cplstre;
        bool[] dithflag;
        bool[] ecplbndstrc;
        bool[] firstcplcos;
        bool[] firstspxcos;
        bool[] lfeexpstr;
        bool[] spxbndstrc;
        bool[] spxcoe;
        byte[] addbsi;
        ChannelPrototype[] channels;
        Decoder decoder;
        ExpStrat[] cplexpstr;
        ExpStrat[][] chexpstr;
        int acmod;
        int blkstrtinfo;
        int blocks;
        int bsmod;
        int chanmap;
        int compr;
        int compr2;
        int convsnroffst;
        int cplbegf;
        int cplendf;
        int dialnorm;
        int dialnorm2;
        int dmixmod;
        int ecpl_begin_subbnd;
        int ecpl_end_subbnd;
        int ecplbegf;
        int ecplendf;
        int extpgmscl;
        int frmcplexpstr;
        int frmcsnroffst;
        int frmfsnroffst;
        int frmsiz;
        int frmsizecod;
        int fscod;
        int lfemixlevcod;
        int lorocmixlev;
        int lorosurmixlev;
        int ltrtcmixlev;
        int ltrtsurmixlev;
        int mixdef;
        int mixlevel;
        int mixlevel2;
        int nspxbnds;
        int numblkscod;
        int pgmscl;
        int pgmscl2;
        int roomtyp;
        int roomtyp2;
        int snroffststr;
        int spx_begin_subbnd;
        int spx_end_subbnd;
        int spxbegf;
        int spxendf;
        int spxstrtf;
        int strmtyp;
        int substreamid;
        int words_per_syncframe;
        int[] blkmixcfginfo;
        int[] chahtinu;
        int[] chbwcod;
        int[] convexpstr;
        int[] frmchexpstr;
        int[] gainrng;
        int[] lfeexps;
        int[] lfemant;
        int[] mstrspxco;
        int[] nchgrps;
        int[] nchmant;
        int[] spxblnd;
        int[] spxbndsztab;
        int[] transproclen;
        int[] transprocloc;
        int[][] chmant;
        int[][] exps;
        int[][] spxcoexp;
        int[][] spxcomant;

        void CreateCacheTables(int blocks, int channels) {
            blkmixcfginfo = new int[blocks];
            blksw = new bool[channels];
            chahtinu = new int[channels];
            chbwcod = new int[channels];
            chexpstr = new ExpStrat[blocks][];
            chincpl = new bool[channels];
            chinspx = new bool[channels];
            chintransproc = new bool[channels];
            chmant = new int[channels][];
            convexpstr = new int[channels];
            cplexpstr = new ExpStrat[blocks];
            cplinu = new bool[blocks];
            cplstre = new bool[blocks];
            dithflag = new bool[channels];
            endmant = new int[channels];
            exps = new int[channels][];
            fgaincod = new int[channels];
            firstcplcos = new bool[channels];
            firstspxcos = new bool[channels];
            frmchexpstr = new int[channels];
            fsnroffst = new int[channels];
            gainrng = new int[channels];
            lfeexps = new int[nlfegrps + 1];
            lfeexpstr = new bool[blocks];
            lfemant = new int[nlfemant];
            mstrspxco = new int[channels];
            nchgrps = new int[channels];
            nchmant = endmant;
            spxblnd = new int[channels];
            spxcoe = new bool[channels];
            spxcoexp = new int[channels][];
            spxcomant = new int[channels][];
            transproclen = new int[channels];
            transprocloc = new int[channels];

            for (int block = 0; block < blocks; ++block) {
                chexpstr[block] = new ExpStrat[channels];
            }

            for (int channel = 0; channel < channels; ++channel) {
                spxcoexp[channel] = new int[nspxbnds];
                spxcomant[channel] = new int[nspxbnds];
            }
        }
    }
}
using Cavern.Remapping;

namespace Cavern.Format.Decoders {
    // These are the stored variables for the decoder. They can be infinitely reused between frames.
    partial class EnhancedAC3Decoder {
        const int lfestrtmant = 0;
        const int lfeendmant = 7;

        /// <summary>
        /// Used full bandwidth channels.
        /// </summary>
        public ReferenceChannel[] Channels { get; private set; }

        /// <summary>
        /// Number of total output channels.
        /// </summary>
        public int ChannelCount => outputs.Count;

        /// <summary>
        /// Type of the last decoded substream.
        /// </summary>
        StreamTypes streamType;

#pragma warning disable IDE0052 // Remove unread private members
        bool adconvtyp;
        bool adconvtyp2;
        bool addbsie;
        bool ahte;
        bool audprodie;
        bool audprodie2;
        bool baie;
        bool bamode;
        bool blkid;
        bool blkstrtinfoe;
        bool blkswe;
        bool compr2e;
        bool compre;
        bool convexpstre;
        bool convsnroffste;
        bool convsync;
        bool cplbndstrce;
        bool dbaflde;
        bool dithflage;
        bool dynrng2e;
        bool dynrnge;
        bool ecplbndstrce;
        bool ecplinu;
        bool expstre;
        bool extpgmscle;
        bool fgaincode;
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
        bool snroffste;
        bool sourcefscod;
        bool spxattene;
        bool spxbndstrce;
        bool spxinu;
        bool spxstre;
        bool transproce;
        bool[] blksw;
        bool[] chincpl;
        bool[] chinspx;
        bool[] chinspxatten;
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
        byte[][] bap;
        byte[] lfebap;
        Decoders decoder;
        ExpStrat[] cplexpstr;
        ExpStrat[][] chexpstr;
        int acmod;
        int blkfsnroffst;
        int blkstrtinfo;
        int blocks;
        int bsmod;
        int chanmap;
        int cmixlev;
        int compr;
        int compr2;
        int convsnroffst;
        int cplbegf;
        int cplendf;
        int cplendmant;
        int cplfgaincod;
        int cplfsnroffst;
        int cplstrtmant;
        int csnroffst;
        int dbpbcod;
        int dheadphonmod;
        int dialnorm;
        int dialnorm2;
        int dmixmod;
        int dsurexmod;
        int dsurmod;
        int dynrng;
        int dynrng2;
        int ecpl_begin_subbnd;
        int ecpl_end_subbnd;
        int ecplbegf;
        int ecplendf;
        int extpgmscl;
        int fdcycod;
        int floorcod;
        int frmcplexpstr;
        int frmcsnroffst;
        int frmfsnroffst;
        int fscod;
        int lfeahtinu;
        int lfefgaincod;
        int lfefsnroffst;
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
        int sdcycod;
        int sgaincod;
        int snroffststr;
        int spx_begin_subbnd;
        int spx_end_subbnd;
        int spxbegf;
        int spxendf;
        int spxstrtf;
        int substreamid;
        int surmixlev;
        int words_per_syncframe;
        int[] blkmixcfginfo;
        int[] chahtinu;
        int[] chbwcod;
        int[] convexpstr;
        int[] endmant;
        int[] fgaincod;
        int[] frmchexpstr;
        int[] fsnroffst;
        int[] gainrng;
        int[] lfeexps;
        int[] lfemant;
        int[] mstrspxco;
        int[] nchgrps;
        int[] nchmant;
        int[] spxattencod;
        int[] spxblnd;
        int[] spxbndsztab;
        int[] strtmant;
        int[] transproclen;
        int[] transprocloc;
        int[][] chmant;
        int[][] exps;
        int[][] spxcoexp;
        int[][] spxcomant;
#pragma warning restore IDE0052 // Remove unread private members

        void CreateCacheTables(int blocks, int channels) {
            bap = new byte[channels][];
            blkmixcfginfo = new int[blocks];
            blksw = new bool[channels];
            chahtinu = new int[channels];
            chbwcod = new int[channels];
            chexpstr = new ExpStrat[blocks][];
            chincpl = new bool[channels];
            chinspx = new bool[channels];
            chinspxatten = new bool[channels];
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
            spxattencod = new int[channels];
            spxblnd = new int[channels];
            spxcoe = new bool[channels];
            spxcoexp = new int[channels][];
            spxcomant = new int[channels][];
            strtmant = new int[channels];
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
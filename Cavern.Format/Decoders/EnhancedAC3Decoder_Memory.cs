using Cavern.Format.Decoders.EnhancedAC3;
using Cavern.Remapping;

namespace Cavern.Format.Decoders {
    // These are the stored variables for the decoder. They can be infinitely reused between frames.
    partial class EnhancedAC3Decoder {
        const int lfestrtmant = 0;
        const int lfeendmant = 7;

        /// <summary>
        /// Used full bandwidth channels.
        /// </summary>
        ReferenceChannel[] channels;

        /// <summary>
        /// Per-channel bit allocation data.
        /// </summary>
        Allocation[] allocation;

        /// <summary>
        /// Coupling channel bit allocation data.
        /// </summary>
        Allocation couplingAllocation;

        /// <summary>
        /// LFE channel bit allocation data.
        /// </summary>
        Allocation lfeAllocation;

#pragma warning disable IDE0052 // Remove unread private members
        bool ahte;
        bool baie;
        bool bamode;
        bool blkstrtinfoe;
        bool blkswe;
        bool convexpstre;
        bool convsnroffste;
        bool cplbndstrce;
        bool dbaflde;
        bool deltbaie;
        bool dithflage;
        bool dynrng2e;
        bool dynrnge;
        bool ecplbndstrce;
        bool ecplinu;
        bool expstre;
        bool fgaincode;
        bool firstcplleak;
        bool frmfgaincode;
        bool phsflginu;
        bool skipflde;
        bool snroffste;
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
        bool[] cplcoe;
        bool[] cplinu;
        bool[] cplstre;
        bool[] dithflag;
        bool[] ecplbndstrc;
        bool[] firstcplcos;
        bool[] firstspxcos;
        bool[] lfeexpstr;
        bool[] spxbndstrc;
        bool[] spxcoe;
        byte[] cplbap;
        byte[] lfebap;
        byte[][] bap;
        DeltaBitAllocation cpldeltba;
        DeltaBitAllocation lfedeltba;
        DeltaBitAllocation[] deltba;
        ExpStrat[] cplexpstr;
        ExpStrat[][] chexpstr;
        int blkfsnroffst;
        int blkstrtinfo;
        int convsnroffst;
        int cplabsexp;
        int cplahtinu;
        int cplbegf;
        int cplendf;
        int cplendmant;
        int cplfgaincod;
        int cplfleak;
        int cplfsnroffst;
        int cplsleak;
        int cplstrtmant;
        int csnroffst;
        int dbpbcod;
        int dynrng;
        int dynrng2;
        int ecpl_begin_subbnd;
        int ecpl_end_subbnd;
        int ecplbegf;
        int ecplendf;
        int fdcycod;
        int floorcod;
        int frmcplexpstr;
        int frmcsnroffst;
        int frmfsnroffst;
        int lfeahtinu;
        int lfefgaincod;
        int lfefsnroffst;
        int ncplbnd;
        int ncplgrps;
        int ncplsubnd;
        int nspxbnds;
        int sdcycod;
        int sgaincod;
        int snroffststr;
        int spx_begin_subbnd;
        int spx_end_subbnd;
        int spxbegf;
        int spxendf;
        int spxstrtf;
        int[] chahtinu;
        int[] chbwcod;
        int[] convexpstr;
        int[] cplexps;
        int[] cplmant;
        int[] endmant;
        int[] fgaincod;
        int[] frmchexpstr;
        int[] fsnroffst;
        int[] gainrng;
        int[] lfeexps;
        int[] lfemant;
        int[] mstrcplco;
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
        int[][] cplcoexp;
        int[][] cplcomant;
        int[][] exps;
        int[][] spxcoexp;
        int[][] spxcomant;
#pragma warning restore IDE0052 // Remove unread private members

        void CreateCacheTables(int blocks, int channels) {
            bap = new byte[channels][];
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
            cplcoe = new bool[channels];
            cplcoexp = new int[channels][];
            cplcomant = new int[channels][];
            cplexpstr = new ExpStrat[blocks];
            cplinu = new bool[blocks];
            cplstre = new bool[blocks];
            deltba = new DeltaBitAllocation[channels];
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
            mstrcplco = new int[channels];
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

            const int maxAllocationSize = 384;

            allocation = new Allocation[channels];
            couplingAllocation = new Allocation(maxAllocationSize);
            lfeAllocation = new Allocation(maxAllocationSize);
            cpldeltba.Reset();
            lfedeltba.Reset();
            for (int channel = 0; channel < channels; ++channel) {
                allocation[channel] = new Allocation(maxAllocationSize);
                deltba[channel].Reset();
            }
        }
    }
}
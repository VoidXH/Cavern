using System;

using Cavern.Utilities;

namespace Cavern.Format.Transcoders {
    // These are the stored variables for the decoder. They can be infinitely reused between frames.
    partial class EnhancedAC3Body {
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

        /// <summary>
        /// Skip field (some bytes of <see cref="auxData"/>) can be present.
        /// </summary>
        bool skipFieldSyntaxEnabled;

        /// <summary>
        /// Skip field (some bytes of <see cref="auxData"/>) is present.
        /// </summary>
        bool skipLengthEnabled;

        /// <summary>
        /// Size of the skip field (this many bytes of <see cref="auxData"/> are contained in this frame.
        /// </summary>
        int skipLength;

        /// <summary>
        /// Last written byte in <see cref="auxData"/>.
        /// </summary>
        int auxDataPos;

        /// <summary>
        /// Unprocessed auxillary data fields are added here.
        /// </summary>
        byte[] auxData = new byte[0];

        bool ahte;
        bool baie;
        bool bamode;
        bool blkstrtinfoe;
        bool blkswe;
        bool convexpstre;
        bool convsnroffste;
        bool cplbndstrce;
        bool cplleake;
        bool dbaflde;
        bool deltbaie;
        bool dithflage;
        bool ecplbndstrce;
        bool ecplinu;
        bool expstre;
        bool fgaincode;
        bool firstcplleak;
        bool frmfgaincode;
        bool phsflginu;
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
        bool[] cplcoe;
        bool[] cplinu;
        bool[] cplstre;
        bool[] dithflag;
        bool[] firstcplcos;
        bool[] firstspxcos;
        bool[] lfeexpstr;
        bool[] spxbndstrc;
        bool[] spxcoe;
        DeltaBitAllocation cpldeltba;
        DeltaBitAllocation lfedeltba;
        DeltaBitAllocation[] deltba;
        ExpStrat[] cplexpstr;
        ExpStrat[][] chexpstr;
        int blkfsnroffst;
        int blkstrtinfo;
        int convsnroffst;
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
        int ecpl_begin_subbnd;
        int ecpl_end_subbnd;
        int ecplbegf;
        int ecplendf;
        int ecplendmant;
        int ecplstartmant;
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
        int? dynrng;
        int? dynrng2;
        int[] chahtinu;
        int[] chbwcod;
        int[] convexpstr;
        int[] cplexps;
        int[] endmant;
        int[] fgaincod;
        int[] frmchexpstr;
        int[] fsnroffst;
        int[] mstrcplco;
        int[] mstrspxco;
        int[] nchgrps;
        int[] spxattencod;
        int[] spxblnd;
        int[] spxbndsztab;
        int[] transproclen;
        int[] transprocloc;
        int[][] cplco;
        int[][] cplcoexp;
        int[][] cplcomant;
        int[][] spxcoexp;
        int[][] spxcomant;

        /// <summary>
        /// PCM output data of each channel.
        /// </summary>
        float[][] channelOutput;

        /// <summary>
        /// Transform coefficients for the coupling channel's last read audio block.
        /// </summary>
        readonly float[] couplingTransformCoeffs = new float[maxAllocationSize];

        /// <summary>
        /// PCM output data for the LFE channel's last read audio block.
        /// </summary>
        readonly float[] lfeOutput = new float[maxAllocationSize];

        readonly bool[] cplbndstrc = { false, false, false, false, false, false, false, false, true, false,
            true, true, false, true, true, true, true, true }; // defcplbndstrc
        readonly bool[] ecplbndstrc = { false, false, false, false, false, false, false, false, true, false,
            true, false, true, false, true, true, true, false, true, true, true }; // defecplbndstrc

        /// <summary>
        /// Some fields are changing mid-block, this have to be replicated while transcoding a body. To prevent overhead in regular decoding,
        /// a backup is created, and restored after each block, for fields that need it.
        /// </summary>
        public EnhancedAC3Body CreateEncodeBackup() {
            return new EnhancedAC3Body(null) {
                cplcoe = cplcoe.FastClone(),
                ecplinu = ecplinu,
                firstcplcos = firstcplcos.FastClone(),
                firstcplleak = firstcplleak,
                firstspxcos = firstspxcos.FastClone(),
                phsflginu = phsflginu
            };
        }

        /// <summary>
        /// Reverts the state to one created with <see cref="CreateEncodeBackup"/>.
        /// </summary>
        public void RestoreEncodeBackup(EnhancedAC3Body backup) {
            Array.Copy(cplcoe, backup.cplcoe, cplcoe.Length);
            ecplinu = backup.ecplinu;
            Array.Copy(firstcplcos, backup.firstcplcos, firstcplcos.Length);
            firstcplleak = backup.firstcplleak;
            Array.Copy(firstspxcos, backup.firstspxcos, firstspxcos.Length);
            phsflginu = backup.phsflginu;
        }

        void CreateCacheTables(int blocks, int channels) {
            blksw = new bool[channels];
            chahtinu = new int[channels];
            chbwcod = new int[channels];
            chexpstr = new ExpStrat[blocks][];
            chincpl = new bool[channels];
            chinspx = new bool[channels];
            chinspxatten = new bool[channels];
            chintransproc = new bool[channels];
            channelOutput = new float[channels][];
            convexpstr = new int[channels];
            cplcoe = new bool[channels];
            cplco = new int[channels][];
            cplcoexp = new int[channels][];
            cplcomant = new int[channels][];
            cplexps = new int[maxAllocationSize];
            cplexpstr = new ExpStrat[blocks];
            cplinu = new bool[blocks];
            cplstre = new bool[blocks];
            deltba = new DeltaBitAllocation[channels];
            dithflag = new bool[channels];
            endmant = new int[channels];
            fgaincod = new int[channels];
            firstcplcos = new bool[channels];
            firstspxcos = new bool[channels];
            frmchexpstr = new int[channels];
            fsnroffst = new int[channels];
            lfeexpstr = new bool[blocks];
            mstrcplco = new int[channels];
            mstrspxco = new int[channels];
            nchgrps = new int[channels];
            spxattencod = new int[channels];
            spxblnd = new int[channels];
            spxcoe = new bool[channels];
            spxcoexp = new int[channels][];
            spxcomant = new int[channels][];
            transproclen = new int[channels];
            transprocloc = new int[channels];

            for (int block = 0; block < blocks; ++block) {
                chexpstr[block] = new ExpStrat[channels];
            }

            allocation = new Allocation[channels];
            couplingAllocation = new Allocation(this, maxAllocationSize);
            lfeAllocation = new Allocation(this, maxAllocationSize);
            cpldeltba.Reset();
            lfedeltba.Reset();
            for (int channel = 0; channel < channels; ++channel) {
                allocation[channel] = new Allocation(this, maxAllocationSize);
                channelOutput[channel] = new float[maxAllocationSize];
                cplco[channel] = new int[cplbndstrc.Length];
                cplcoexp[channel] = new int[cplbndstrc.Length];
                cplcomant[channel] = new int[cplbndstrc.Length];
                deltba[channel].Reset();
            }
        }

        /// <summary>
        /// Absolute maximum band. If arrays are allocated to this size, they can't be overrun.
        /// </summary>
        const int maxAllocationSize = 256;

        /// <summary>
        /// First mantissa of the LFE channel.
        /// </summary>
        const int lfestrtmant = 0;

        /// <summary>
        /// Last mantissa of the LFE channel.
        /// </summary>
        const int lfeendmant = 7;
    }
}
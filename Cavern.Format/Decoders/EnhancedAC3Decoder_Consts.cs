using Cavern.Remapping;

namespace Cavern.Format.Decoders {
    partial class EnhancedAC3Decoder {
        /// <summary>
        /// Meaning of values for chexpstr[ch], cplexpstr, and lfeexpstr.
        /// </summary>
        enum ExpStrat {
            Reuse = 0,
            D15,
            D25,
            D45
        }

        /// <summary>
        /// Number of LFE groups.
        /// </summary>
        const int nlfegrps = 2;

        /// <summary>
        /// Fixed LFE mantissa count.
        /// </summary>
        const int nlfemant = 7;

        /// <summary>
        /// Sub-band transform start coefficients.
        /// </summary>
        static readonly int[] ecplsubbndtab = new int[23]
            { 13, 19, 25, 31, 37, 49, 61, 73, 85, 97, 109, 121, 133, 145, 157, 169, 181, 193, 205, 217, 229, 241, 253 };

        /// <summary>
        /// Frame exponent strategy combinations.
        /// </summary>
        static readonly ExpStrat[][] frmcplexpstr_tbl = new ExpStrat[32][] {
            new ExpStrat[6] { ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse},
            new ExpStrat[6] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45},
        };
    }
}
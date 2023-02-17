namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Body {
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
        static readonly int[] ecplsubbndtab =
            { 13, 19, 25, 31, 37, 49, 61, 73, 85, 97, 109, 121, 133, 145, 157, 169, 181, 193, 205, 217, 229, 241, 253 };

        /// <summary>
        /// Frame exponent strategy combinations.
        /// </summary>
        static readonly ExpStrat[][] frmcplexpstr_tbl = {
            new[] { ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse},
            new[] { ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45},
            new[] { ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse},
            new[] { ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45},
            new[] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse},
            new[] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45},
            new[] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.D45, ExpStrat.D25, ExpStrat.Reuse },
            new[] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.D45, ExpStrat.D45, ExpStrat.D45 },
            new[] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse},
            new[] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45},
            new[] { ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse},
            new[] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.D25, ExpStrat.Reuse, ExpStrat.D45, ExpStrat.D45 },
            new[] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.D45, ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse },
            new[] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.D45, ExpStrat.D25, ExpStrat.Reuse, ExpStrat.D45 },
            new[] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.D45, ExpStrat.D45, ExpStrat.D25, ExpStrat.Reuse },
            new[] { ExpStrat.D25, ExpStrat.Reuse, ExpStrat.D45, ExpStrat.D45, ExpStrat.D45, ExpStrat.D45 },
            new[] { ExpStrat.D45,   ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse},
            new[] { ExpStrat.D45,   ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45},
            new[] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse},
            new[] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45},
            new[] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse},
            new[] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45},
            new[] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse},
            new[] { ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D45},
            new[] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D15, ExpStrat.Reuse, ExpStrat.Reuse, ExpStrat.Reuse},
            new[] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse,   ExpStrat.D45},
            new[] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D25, ExpStrat.Reuse},
            new[] { ExpStrat.D45,   ExpStrat.D45,   ExpStrat.D25, ExpStrat.Reuse,   ExpStrat.D45,   ExpStrat.D45},
            new[] { ExpStrat.D45, ExpStrat.D45, ExpStrat.D45, ExpStrat.D25, ExpStrat.Reuse, ExpStrat.Reuse },
            new[] { ExpStrat.D45, ExpStrat.D45, ExpStrat.D45, ExpStrat.D25, ExpStrat.Reuse, ExpStrat.D45 },
            new[] { ExpStrat.D45, ExpStrat.D45, ExpStrat.D45, ExpStrat.D45, ExpStrat.D25, ExpStrat.Reuse },
            new[] { ExpStrat.D45, ExpStrat.D45, ExpStrat.D45, ExpStrat.D45, ExpStrat.D45, ExpStrat.D45 },
        };

        /// <summary>
        /// Slow decay table.
        /// </summary>
        static readonly byte[] slowdec = { 0x0f, 0x11, 0x13, 0x15 };

        /// <summary>
        /// Fast decay table.
        /// </summary>
        static readonly byte[] fastdec = { 0x3f, 0x53, 0x67, 0x7b };

        /// <summary>
        /// Slow gain table.
        /// </summary>
        static readonly short[] slowgain = { 0x540, 0x4d8, 0x478, 0x410 };

        /// <summary>
        /// dB/bit table.
        /// </summary>
        static readonly short[] dbpbtab = { 0x000, 0x700, 0x900, 0xb00 };

        /// <summary>
        /// Floor table.
        /// </summary>
        static readonly short[] floortab = { 0x2f0, 0x2b0, 0x270, 0x230, 0x1f0, 0x170, 0x0f0, -2048 };

        /// <summary>
        /// Fast gain table.
        /// </summary>
        static readonly short[] fastgain = { 0x080, 0x100, 0x180, 0x200, 0x280, 0x300, 0x380, 0x400 };

        /// <summary>
        /// Banding structure tables.
        /// </summary>
        static readonly byte[] bndtab = {
             1,  2,  3,   4,   5,   6,   7,   8,   9,  10,
            11, 12, 13,  14,  15,  16,  17,  18,  19,  20,
            21, 22, 23,  24,  25,  26,  27,  28,  31,  34,
            37, 40, 43,  46,  49,  55,  61,  67,  73,  79,
            85, 97, 109, 121, 133, 157, 181, 205, 229, 253
        };

        /// <summary>
        /// Bin number to band number table.
        /// </summary>
        static readonly byte[] masktab = {
             0,  1,  2,  3,  4,  5,  6,  7,  8,  9,
            10, 11, 12, 13, 14, 15, 16, 17, 18, 19,
            20, 21, 22, 23, 24, 25, 26, 27, 28, 28,
            28, 29, 29, 29, 30, 30, 30, 31, 31, 31,
            32, 32, 32, 33, 33, 33, 34, 34, 34, 35,
            35, 35, 35, 35, 35, 36, 36, 36, 36, 36,
            36, 37, 37, 37, 37, 37, 37, 38, 38, 38,
            38, 38, 38, 39, 39, 39, 39, 39, 39, 40,
            40, 40, 40, 40, 40, 41, 41, 41, 41, 41,
            41, 41, 41, 41, 41, 41, 41, 42, 42, 42,
            42, 42, 42, 42, 42, 42, 42, 42, 42, 43,
            43, 43, 43, 43, 43, 43, 43, 43, 43, 43,
            43, 44, 44, 44, 44, 44, 44, 44, 44, 44,
            44, 44, 44, 45, 45, 45, 45, 45, 45, 45,
            45, 45, 45, 45, 45, 45, 45, 45, 45, 45,
            45, 45, 45, 45, 45, 45, 45, 46, 46, 46,
            46, 46, 46, 46, 46, 46, 46, 46, 46, 46,
            46, 46, 46, 46, 46, 46, 46, 46, 46, 46,
            46, 47, 47, 47, 47, 47, 47, 47, 47, 47,
            47, 47, 47, 47, 47, 47, 47, 47, 47, 47,
            47, 47, 47, 47, 47, 48, 48, 48, 48, 48,
            48, 48, 48, 48, 48, 48, 48, 48, 48, 48,
            48, 48, 48, 48, 48, 48, 48, 48, 48, 49,
            49, 49, 49, 49, 49, 49, 49, 49, 49, 49,
            49, 49, 49, 49, 49, 49, 49, 49, 49, 49,
            49, 49, 49, 0, 0, 0
        };

        /// <summary>
        /// Log-addition table.
        /// </summary>
        static readonly byte[] latab = {
            0x40, 0x3f, 0x3e, 0x3d, 0x3c, 0x3b, 0x3a, 0x39, 0x38, 0x37,
            0x36, 0x35, 0x34, 0x34, 0x33, 0x32, 0x31, 0x30, 0x2f, 0x2f,
            0x2e, 0x2d, 0x2c, 0x2c, 0x2b, 0x2a, 0x29, 0x29, 0x28, 0x27,
            0x26, 0x26, 0x25, 0x24, 0x24, 0x23, 0x23, 0x22, 0x21, 0x21,
            0x20, 0x20, 0x1f, 0x1e, 0x1e, 0x1d, 0x1d, 0x1c, 0x1c, 0x1b,
            0x1b, 0x1a, 0x1a, 0x19, 0x19, 0x18, 0x18, 0x17, 0x17, 0x16,
            0x16, 0x15, 0x15, 0x15, 0x14, 0x14, 0x13, 0x13, 0x13, 0x12,
            0x12, 0x12, 0x11, 0x11, 0x11, 0x10, 0x10, 0x10, 0x0f, 0x0f,
            0x0f, 0x0e, 0x0e, 0x0e, 0x0d, 0x0d, 0x0d, 0x0d, 0x0c, 0x0c,
            0x0c, 0x0c, 0x0b, 0x0b, 0x0b, 0x0b, 0x0a, 0x0a, 0x0a, 0x0a,
            0x0a, 0x09, 0x09, 0x09, 0x09, 0x09, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x06, 0x06,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x05, 0x05, 0x05, 0x05,
            0x05, 0x05, 0x05, 0x05, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x04, 0x04, 0x04, 0x04, 0x04, 0x03, 0x03, 0x03, 0x03, 0x03,
            0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x03, 0x02,
            0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02,
            0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x02, 0x01, 0x01,
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01, 0x01,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x00, 0x00, 0x00, 0x00, 0x00, 0x00
        };

        /// <summary>
        /// Hearing threshold table.
        /// </summary>
        static readonly int[][] hth = {
            new[] {
                0x04d0, 0x04d0, 0x0440, 0x0400, 0x03e0,
                0x03c0, 0x03b0, 0x03b0, 0x03a0, 0x03a0,
                0x03a0, 0x03a0, 0x03a0, 0x0390, 0x0390,
                0x0390, 0x0380, 0x0380, 0x0370, 0x0370,
                0x0360, 0x0360, 0x0350, 0x0350, 0x0340,
                0x0340, 0x0330, 0x0320, 0x0310, 0x0300,
                0x02f0, 0x02f0, 0x02f0, 0x02f0, 0x0300,
                0x0310, 0x0340, 0x0390, 0x03e0, 0x0420,
                0x0460, 0x0490, 0x04a0, 0x0460, 0x0440,
                0x0440, 0x0520, 0x0800, 0x0840, 0x0840
            },
            new[] {
                0x04f0, 0x04f0, 0x0460, 0x0410, 0x03e0,
                0x03d0, 0x03c0, 0x03b0, 0x03b0, 0x03a0,
                0x03a0, 0x03a0, 0x03a0, 0x03a0, 0x0390,
                0x0390, 0x0390, 0x0380, 0x0380, 0x0380,
                0x0370, 0x0370, 0x0360, 0x0360, 0x0350,
                0x0350, 0x0340, 0x0340, 0x0320, 0x0310,
                0x0300, 0x02f0, 0x02f0, 0x02f0, 0x02f0,
                0x0300, 0x0320, 0x0350, 0x0390, 0x03e0,
                0x0420, 0x0450, 0x04a0, 0x0490, 0x0460,
                0x0440, 0x0480, 0x0630, 0x0840, 0x0840
            },
            new[] {
                0x0580, 0x0580, 0x04b0, 0x0450, 0x0420,
                0x03f0, 0x03e0, 0x03d0, 0x03c0, 0x03b0,
                0x03b0, 0x03b0, 0x03a0, 0x03a0, 0x03a0,
                0x03a0, 0x03a0, 0x03a0, 0x03a0, 0x03a0,
                0x0390, 0x0390, 0x0390, 0x0390, 0x0380,
                0x0380, 0x0380, 0x0370, 0x0360, 0x0350,
                0x0340, 0x0330, 0x0320, 0x0310, 0x0300,
                0x02f0, 0x02f0, 0x02f0, 0x0300, 0x0310,
                0x0330, 0x0350, 0x03c0, 0x0410, 0x0470,
                0x04a0, 0x0460, 0x0440, 0x0450, 0x04e0
            }
        };

        /// <summary>
        /// Bit allocation pointer table.
        /// </summary>
        static readonly byte[] baptab = {
             0,  1,  1,  1,  1,  1,  2,  2,  3,  3,
             3,  4,  4,  5,  5,  6,  6,  6,  6,  7,
             7,  7,  7,  8,  8,  8,  8,  9,  9,  9,
             9, 10, 10, 10, 10, 11, 11, 11, 11, 12,
            12, 12, 12, 13, 13, 13, 13, 14, 14, 14,
            14, 14, 14, 14, 14, 15, 15, 15, 15, 15,
            15, 15, 15, 15
        };

        /// <summary>
        /// Number of bits to read. Corresponds to each value of a BAP table.
        /// </summary>
        static readonly byte[] bitsToRead = { 0, bap1Bits, bap2Bits, bap3Bits, bap4Bits, bap5Bits, 5, 6, 7, 8, 9, 10, 11, 12, 14, 16 };

        const byte bap1Bits = 5;
        const byte bap2Bits = 7;
        const byte bap3Bits = 3;
        const byte bap4Bits = 7;
        const byte bap5Bits = 4;
    }
}
namespace Cavern.Format.Decoders.EnhancedAC3 {
    enum HuffmanType {
        /// <summary>
        /// Matrix.
        /// </summary>
        MTX,
        /// <summary>
        /// Vector.
        /// </summary>
        VEC,
        /// <summary>
        /// Index.
        /// </summary>
        IDX
    }

    /// <summary>
    /// Hardcoded values for JOC decoding.
    /// </summary>
    static class JointObjectCodingTables {
        /// <summary>
        /// Implementation of joc_get_huff_code, gets the corresponding Huffman table.
        /// </summary>
        /// <remarks>Does not handle invalid modes correctly.</remarks>
        public static int[][] GetHuffCodeTable(int mode, HuffmanType type) => type switch {
            HuffmanType.MTX => mode == 1 ? joc_huff_code_fine_generic : joc_huff_code_coarse_generic,
            HuffmanType.VEC => mode == 1 ? joc_huff_code_fine_coeff_sparse : joc_huff_code_coarse_coeff_sparse,
            _ => mode == 7 ? joc_huff_code_7ch_pos_index_sparse : joc_huff_code_5ch_pos_index_sparse,
        };

        public static readonly byte[] joc_num_bands = { 1, 3, 5, 7, 9, 12, 15, 23 };

        public static readonly byte[][] parameterBandMapping = {
            new byte[1]  { 0 },
            new byte[3]  { 0, 3, 14 },
            new byte[5]  { 0, 1, 3, 9, 23 },
            new byte[7]  { 0, 1, 2, 4, 8, 14, 23 },
            new byte[9]  { 0, 1, 2, 3, 5, 7, 9, 14, 23 },
            new byte[12] { 0, 1, 2, 3, 4, 6, 8, 11, 14, 18, 23, 35 },
            new byte[15] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 11, 14, 18, 23, 35 }
        };

        static readonly int[][] joc_huff_code_coarse_generic = {
            new int[] {  -1,   1}, new int[] {   2,  -2}, new int[] { -96,   3}, new int[] {   4,  -3}, new int[] { -95,   5},
            new int[] {   6,   7}, new int[] {  -4, -94}, new int[] {   8,   9}, new int[] {  -5, -93}, new int[] {  10,  11},
            new int[] {  -6, -92}, new int[] {  12,  13}, new int[] {  -7, -91}, new int[] {  14,  15}, new int[] {  16, -90},
            new int[] {  -8,  17}, new int[] {  18, -89}, new int[] {  -9,  19}, new int[] {  20,  21}, new int[] { -88, -10},
            new int[] {  22,  23}, new int[] { -11, -87}, new int[] {  24,  25}, new int[] {  26, -86}, new int[] { -12,  27},
            new int[] {  28, -85}, new int[] { -13,  29}, new int[] {  30,  31}, new int[] {  32, -84}, new int[] { -14,  33},
            new int[] {  34, -15}, new int[] { -83,  35}, new int[] {  36,  37}, new int[] { -16,  38}, new int[] { -17, -82},
            new int[] {  39,  40}, new int[] {  41, -81}, new int[] {  42,  43}, new int[] {  44,  45}, new int[] {  46,  47},
            new int[] {  48,  49}, new int[] {  50,  51}, new int[] {  52, -18}, new int[] { -78,  53}, new int[] { -19,  54},
            new int[] {  55,  56}, new int[] {  57,  58}, new int[] { -22,  59}, new int[] {  60,  61}, new int[] {  62,  63},
            new int[] {  64,  65}, new int[] {  66,  67}, new int[] {  68, -20}, new int[] { -21, -79}, new int[] { -80, -25},
            new int[] {  69,  70}, new int[] { -26,  71}, new int[] {  72,  73}, new int[] {  74,  75}, new int[] {  76,  77},
            new int[] {  78,  79}, new int[] {  80,  81}, new int[] {  82,  83}, new int[] {  84,  85}, new int[] {  86,  87},
            new int[] {  88,  89}, new int[] {  90,  91}, new int[] {  92,  93}, new int[] {  94, -23}, new int[] { -74, -75},
            new int[] { -72, -73}, new int[] { -76, -77}, new int[] { -34, -35}, new int[] { -32, -33}, new int[] { -38, -39},
            new int[] { -36, -37}, new int[] { -30, -31}, new int[] { -28, -29}, new int[] { -50, -51}, new int[] { -48, -49},
            new int[] { -54, -55}, new int[] { -52, -53}, new int[] { -42, -43}, new int[] { -40, -41}, new int[] { -46, -47},
            new int[] { -44, -45}, new int[] { -66, -67}, new int[] { -64, -65}, new int[] { -70, -71}, new int[] { -68, -69},
            new int[] { -58, -59}, new int[] { -56, -57}, new int[] { -62, -63}, new int[] { -60, -61}, new int[] { -24, -27}
        };

        static readonly int[][] joc_huff_code_fine_generic = {
            new int[] {  -1,   1}, new int[] {   2,   3}, new int[] {  -2,-192}, new int[] {   4,   5}, new int[] {   6,  -3},
            new int[] {-191,   7}, new int[] {   8,   9}, new int[] {  -4,-190}, new int[] {  10,  11}, new int[] {  -5,-189},
            new int[] {  12,  13}, new int[] {  -6,  14}, new int[] {-188,  15}, new int[] {  16,  -7}, new int[] {-187,  17},
            new int[] {  18,  -8}, new int[] {-186,  19}, new int[] {  20,  -9}, new int[] {-185,  21}, new int[] {  22, -10},
            new int[] {-184,  23}, new int[] {  24, -11}, new int[] {  25,-183}, new int[] {  26,  27}, new int[] { -12,-182},
            new int[] {  28,  29}, new int[] { -13,-181}, new int[] {  30,  31}, new int[] {-180, -14}, new int[] {  32,  33},
            new int[] {  34,-179}, new int[] { -15,  35}, new int[] {  36,-178}, new int[] { -16,  37}, new int[] {  38,-177},
            new int[] {  39, -17}, new int[] {  40,  41}, new int[] {-176,  42}, new int[] { -18,  43}, new int[] { -19,  44},
            new int[] {-175,  45}, new int[] {  46,-174}, new int[] { -20,  47}, new int[] {-173,  48}, new int[] {  49, -21},
            new int[] {  50,  51}, new int[] {  52, -22}, new int[] {  53,  54}, new int[] {-172,  55}, new int[] {-171, -23},
            new int[] {  56,  57}, new int[] {  58,-170}, new int[] {  59, -24}, new int[] { -25,  60}, new int[] {-169,  61},
            new int[] {  62,  63}, new int[] {  64,  65}, new int[] {  66,  67}, new int[] {-168,  68}, new int[] { -26,  69},
            new int[] {-167, -27}, new int[] {  70,-166}, new int[] {-165,  71}, new int[] { -29,  72}, new int[] {  73,  74},
            new int[] { -30,  75}, new int[] {  76,  77}, new int[] {  78,  79}, new int[] {  80, -28}, new int[] {  81,  82},
            new int[] {  83,-163}, new int[] { -31, -33}, new int[] {-164,-161}, new int[] {  84,  85}, new int[] {  86,  87},
            new int[] {  88,  89}, new int[] {  90,  91}, new int[] {  92,  93}, new int[] {  94,  95}, new int[] {  96,  97},
            new int[] {  98,  99}, new int[] { -32,-162}, new int[] { 100, 101}, new int[] { 102, 103}, new int[] { 104, 105},
            new int[] { 106, 107}, new int[] { 108, 109}, new int[] { 110, 111}, new int[] {-160, 112}, new int[] { -36, -38},
            new int[] { 113, 114}, new int[] { 115, 116}, new int[] { 117, 118}, new int[] { 119, 120}, new int[] { 121, 122},
            new int[] { 123, 124}, new int[] { 125, 126}, new int[] { 127, 128}, new int[] { 129, 130}, new int[] { 131, 132},
            new int[] { 133, -35}, new int[] {-158, 134}, new int[] {-155,-156}, new int[] { -37, -42}, new int[] { 135, 136},
            new int[] { 137, 138}, new int[] { 139, 140}, new int[] { 141, 142}, new int[] { 143, 144}, new int[] { 145, 146},
            new int[] { 147, 148}, new int[] { 149, 150}, new int[] { 151, 152}, new int[] { 153, 154}, new int[] { 155, 156},
            new int[] { 157, 158}, new int[] { 159, 160}, new int[] { 161, 162}, new int[] { 163, 164}, new int[] { 165, 166},
            new int[] { 167, 168}, new int[] { 169, 170}, new int[] { 171, 172}, new int[] { 173, 174}, new int[] { 175, 176},
            new int[] { 177, 178}, new int[] { 179, 180}, new int[] { 181, 182}, new int[] { 183, 184}, new int[] {-157, 185},
            new int[] { -45, -48}, new int[] { 186, 187}, new int[] { 188, 189}, new int[] { -34, -41}, new int[] { 190, -39},
            new int[] { -60, -61}, new int[] { -58, -59}, new int[] { -64, -65}, new int[] { -62, -63}, new int[] { -52, -53},
            new int[] { -50, -51}, new int[] { -56, -57}, new int[] { -54, -55}, new int[] { -76, -77}, new int[] { -74, -75},
            new int[] { -80, -81}, new int[] { -78, -79}, new int[] { -68, -69}, new int[] { -66, -67}, new int[] { -72, -73},
            new int[] { -70, -71}, new int[] { -47, -49}, new int[] { -44, -46}, new int[] {-124,-125}, new int[] {-122,-123},
            new int[] {-128,-129}, new int[] {-126,-127}, new int[] {-116,-117}, new int[] {-114,-115}, new int[] {-120,-121},
            new int[] {-118,-119}, new int[] {-140,-141}, new int[] {-138,-139}, new int[] {-144,-145}, new int[] {-142,-143},
            new int[] {-132,-133}, new int[] {-130,-131}, new int[] {-136,-137}, new int[] {-134,-135}, new int[] { -92, -93},
            new int[] { -90, -91}, new int[] { -96, -97}, new int[] { -94, -95}, new int[] { -84, -85}, new int[] { -82, -83},
            new int[] { -88, -89}, new int[] { -86, -87}, new int[] {-108,-109}, new int[] {-106,-107}, new int[] {-112,-113},
            new int[] {-110,-111}, new int[] {-100,-101}, new int[] { -98, -99}, new int[] {-104,-105}, new int[] {-102,-103},
            new int[] {-154,-159}, new int[] {-148,-149}, new int[] {-146,-147}, new int[] {-152,-153}, new int[] {-150,-151},
            new int[] { -40, -43}
        };

        static readonly int[][] joc_huff_code_coarse_coeff_sparse = {
            new int[] {  -1,   1}, new int[] {   2,   3}, new int[] {  -2, -96}, new int[] {   4,   5}, new int[] {   6, -95},
            new int[] {  -3,   7}, new int[] {   8,   9}, new int[] {  -4,  10}, new int[] { -94,  11}, new int[] {  12,  -5},
            new int[] { -93,  13}, new int[] {  14,  15}, new int[] {  -6, -92}, new int[] {  16,  17}, new int[] {  18,  -7},
            new int[] { -91,  19}, new int[] {  20,  -8}, new int[] { -90,  21}, new int[] {  22,  23}, new int[] {  -9, -89},
            new int[] {  24,  25}, new int[] {  26, -10}, new int[] { -88,  27}, new int[] {  28,  29}, new int[] {  30, -11},
            new int[] { -87,  31}, new int[] {  32,  33}, new int[] {  34,  35}, new int[] { -12, -86}, new int[] {  36,  37},
            new int[] {  38, -13}, new int[] {  39, -85}, new int[] {  40,  41}, new int[] {  42,  43}, new int[] { -14, -84},
            new int[] {  44,  45}, new int[] {  46,  47}, new int[] { -83, -15}, new int[] {  48,  49}, new int[] {  50, -16},
            new int[] {  51,  52}, new int[] { -82,  53}, new int[] {  54, -81}, new int[] {  55,  56}, new int[] { -17,  57},
            new int[] {  58, -80}, new int[] {  59,  60}, new int[] { -18,  61}, new int[] {  62,  63}, new int[] { -79,  64},
            new int[] { -19, -78}, new int[] {  65,  66}, new int[] {  67,  68}, new int[] {  69, -20}, new int[] { -77, -21},
            new int[] {  70,  71}, new int[] {  72,  73}, new int[] {  74, -76}, new int[] {  75, -22}, new int[] {  76,  77},
            new int[] { -75,  78}, new int[] {  79,  80}, new int[] { -54, -74}, new int[] { -73,  81}, new int[] { -23,  82},
            new int[] { -50, -24}, new int[] { -55, -25}, new int[] {  83, -47}, new int[] { -49, -44}, new int[] { -71,  84},
            new int[] { -48, -51}, new int[] {  85, -72}, new int[] { -26, -53}, new int[] { -70, -27}, new int[] {  86, -45},
            new int[] {  87,  88}, new int[] { -68,  89}, new int[] { -29, -43}, new int[] {  90, -30}, new int[] { -46, -69},
            new int[] {  91, -28}, new int[] { -52, -31}, new int[] {  92, -32}, new int[] {  93, -64}, new int[] { -67,  94},
            new int[] { -36, -33}, new int[] { -63, -37}, new int[] { -65, -61}, new int[] { -66, -59}, new int[] { -34, -38},
            new int[] { -41, -42}, new int[] { -35, -60}, new int[] { -39, -57}, new int[] { -56, -40}, new int[] { -62, -58}
        };

        static readonly int[][] joc_huff_code_fine_coeff_sparse = {
            new int[] {   1,  -1}, new int[] {   2,   3}, new int[] {   4,  -2}, new int[] {-192,   5}, new int[] {   6,   7},
            new int[] {   8,  -3}, new int[] {-191,   9}, new int[] {  10,  11}, new int[] {  12,-190}, new int[] {  -4,  13},
            new int[] {  14,  15}, new int[] {-189,  -5}, new int[] {  16,  17}, new int[] {  18,  -6}, new int[] {-188,  19},
            new int[] {  20,  21}, new int[] {  -7,-187}, new int[] {  22,  23}, new int[] {  -8,  24}, new int[] {-186,  25},
            new int[] {  -9,  26}, new int[] {  27,-185}, new int[] {  28, -10}, new int[] {  29,  30}, new int[] {-184,  31},
            new int[] { -11,  32}, new int[] {  33,-183}, new int[] {  34, -12}, new int[] {  35,-182}, new int[] {  36,  37},
            new int[] {  38, -13}, new int[] {-181,  39}, new int[] {  40, -14}, new int[] {  41,-180}, new int[] {  42,  43},
            new int[] {-179, -15}, new int[] {  44, -16}, new int[] {  45,-178}, new int[] {  46,  47}, new int[] {  48,  49},
            new int[] {  50,-177}, new int[] { -17,  51}, new int[] { -18,  52}, new int[] {-176,  53}, new int[] {  54,  55},
            new int[] {-175, -19}, new int[] {  56,  57}, new int[] {  58, -20}, new int[] {  59,-174}, new int[] {  60,  61},
            new int[] { -21,  62}, new int[] {  63,-173}, new int[] {  64,  65}, new int[] {  66,-172}, new int[] {  67,  68},
            new int[] { -22,  69}, new int[] {  70,  71}, new int[] { -23,  72}, new int[] {-171,  73}, new int[] {  74,  75},
            new int[] {  76, -24}, new int[] {  77,-170}, new int[] { -25,  78}, new int[] {  79,  80}, new int[] {  81,-169},
            new int[] {  82,  83}, new int[] {  84, -26}, new int[] {  85,-168}, new int[] {  86,  87}, new int[] {  88,  89},
            new int[] {-167,  90}, new int[] { -27,  91}, new int[] {  92, -28}, new int[] {  93,-166}, new int[] {  94, -29},
            new int[] {  95,  96}, new int[] {  97,  98}, new int[] {-165,  99}, new int[] { 100, -30}, new int[] {-164, 101},
            new int[] { 102, 103}, new int[] { 104, 105}, new int[] {-163, 106}, new int[] { -31, 107}, new int[] { -32, 108},
            new int[] { 109, 110}, new int[] {-161, 111}, new int[] {-160,-162}, new int[] { 112, -34}, new int[] { -33, 113},
            new int[] { 114, 115}, new int[] { 116, 117}, new int[] { 118, 119}, new int[] { 120,-159}, new int[] { 121, 122},
            new int[] { 123,-158}, new int[] { 124, 125}, new int[] { -36,-155}, new int[] { 126, 127}, new int[] { -35, 128},
            new int[] { 129, 130}, new int[] {-157, 131}, new int[] {-156, 132}, new int[] { -37, 133}, new int[] { 134, 135},
            new int[] {-154, -38}, new int[] { 136, 137}, new int[] { -39, -41}, new int[] { 138,-153}, new int[] { 139, -40},
            new int[] {-149, 140}, new int[] { 141, 142}, new int[] { 143, 144}, new int[] {-151, 145}, new int[] { 146, 147},
            new int[] { 148, -42}, new int[] { -43, 149}, new int[] { 150, 151}, new int[] {-152, 152}, new int[] { -46, -98},
            new int[] { 153, 154}, new int[] { 155,-147}, new int[] { 156, 157}, new int[] { 158,-107}, new int[] { 159, 160},
            new int[] {-145,-150}, new int[] { -96, 161}, new int[] { 162, -45}, new int[] {-146, 163}, new int[] { 164, -97},
            new int[] {-108,-105}, new int[] {-148,-106}, new int[] { -44, 165}, new int[] { -94,-141}, new int[] { -99, 166},
            new int[] { -89, 167}, new int[] { -50, -95}, new int[] {-100, -48}, new int[] {-144, 168}, new int[] { 169, 170},
            new int[] { -51,-142}, new int[] { -90, -91}, new int[] { -47, -49}, new int[] { 171, -53}, new int[] { -93,-143},
            new int[] {-137,-138}, new int[] { -55,-101}, new int[] { 172, 173}, new int[] { -54, -86}, new int[] { -88, -87},
            new int[] {-103, 174}, new int[] { 175, -61}, new int[] {-109, 176}, new int[] { 177, 178}, new int[] { -52,-139},
            new int[] { -57,-140}, new int[] { 179, 180}, new int[] { -56,-136}, new int[] { -58,-102}, new int[] { 181, 182},
            new int[] { -60,-135}, new int[] { 183,-104}, new int[] {-128,-134}, new int[] { -92, 184}, new int[] { -59, -62},
            new int[] { 185, 186}, new int[] { -71,-133}, new int[] { 187,-127}, new int[] {-126, 188}, new int[] { -63, -64},
            new int[] { -85,-132}, new int[] { 189, -66}, new int[] {-121,-125}, new int[] { 190, -68}, new int[] { -74, -75},
            new int[] { -70, -73}, new int[] { -81, -65}, new int[] {-118,-131}, new int[] { -72,-110}, new int[] {-119,-120},
            new int[] { -76, -84}, new int[] {-122,-130}, new int[] { -83,-117}, new int[] { -69, -78}, new int[] { -80, -82},
            new int[] {-123,-124}, new int[] { -67,-116}, new int[] {-129, -77}, new int[] {-113,-114}, new int[] {-112,-115},
            new int[] { -79,-111}
        };

        static readonly int[][] joc_huff_code_5ch_pos_index_sparse = {
            new int[] {  -1,   1}, new int[] {   2,   3}, new int[] {  -4,  -3}, new int[] {  -2,  -5}
        };

        static readonly int[][] joc_huff_code_7ch_pos_index_sparse = {
            new int[] {  -1,   1}, new int[] {   2,   3}, new int[] {   4,   5}, new int[] {  -4,  -3}, new int[] {  -2,  -5},
            new int[] {  -6,  -7}
        };
    }
}
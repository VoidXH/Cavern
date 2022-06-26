using System;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    // Reusable arrays
    partial class JointObjectCoding {
        /// <summary>
        /// Maximum number of objects to render.
        /// </summary>
        const int maxObjects = 64;

        /// <summary>
        /// Previous JOC mixing matrix values.
        /// </summary>
        readonly float[][][] prevMatrix;

        /// <summary>
        /// Indexing value for given <see cref="joc_num_bands"/> values in cache tables.
        /// </summary>
        byte[] joc_num_bands_idx;

        /// <summary>
        /// Number of processed bands of each object.
        /// </summary>
        byte[] joc_num_bands;

        /// <summary>
        /// Number of data points for each object.
        /// </summary>
        int[] dataPoints;

        bool[] b_joc_sparse;
        bool[] joc_num_quant_idx;
        bool[] joc_slope_idx;
        float[][][][] joc_mix_mtx;
        float[][][][] joc_mix_mtx_interp;
        int[][] joc_offset_ts;
        int[][][] joc_channel_idx;
        int[][][] joc_vec;
        int[][][][] joc_mtx;

        /// <summary>
        /// Create a JOC decoder. Always reuse a previous one as history data is required for decoding.
        /// </summary>
        public JointObjectCoding() {
            int maxChannels = JointObjectCodingTables.inputMatrix.Length;
            prevMatrix = new float[maxObjects][][];
            for (int obj = 0; obj < maxObjects; ++obj) {
                prevMatrix[obj] = new float[maxChannels][];
                for (int ch = 0; ch < maxChannels; ++ch)
                    prevMatrix[obj][ch] = new float[QuadratureMirrorFilterBank.subbands];
            }
        }

        /// <summary>
        /// Checks if the cache is ready for the given number of objects and channels, and fixes if it's not.
        /// </summary>
        public void UpdateCache() {
            if (ObjectActive.Length == ObjectCount && joc_mtx[0][0].Length == ChannelCount)
                return;

            if (JointObjectCodingTables.parameterBandMapping[0].Length == 1)
                SetupStaticCache();

            ObjectActive = new bool[ObjectCount];
            joc_num_bands_idx = new byte[ObjectCount];
            joc_num_bands = new byte[ObjectCount];
            b_joc_sparse = new bool[ObjectCount];
            joc_num_quant_idx = new bool[ObjectCount];
            joc_slope_idx = new bool[ObjectCount];
            joc_mix_mtx = new float[ObjectCount][][][];
            joc_mix_mtx_interp = new float[ObjectCount][][][];
            dataPoints = new int[ObjectCount];
            joc_offset_ts = new int[ObjectCount][];
            joc_channel_idx = new int[ObjectCount][][];
            joc_vec = new int[ObjectCount][][];
            joc_mtx = new int[ObjectCount][][][];

            int maxBands = JointObjectCodingTables.joc_num_bands[^1];
            const int maxDataPoints = 2;
            const int maxTimeslots = 1536 / QuadratureMirrorFilterBank.subbands;
            for (int obj = 0; obj < ObjectCount; ++obj) {
                joc_mix_mtx[obj] = new float[maxDataPoints][][];
                joc_mix_mtx_interp[obj] = new float[maxTimeslots][][];
                joc_offset_ts[obj] = new int[maxDataPoints];
                joc_channel_idx[obj] = new int[maxDataPoints][];
                joc_vec[obj] = new int[maxDataPoints][];
                joc_mtx[obj] = new int[maxDataPoints][][];
                for (int dp = 0; dp < maxDataPoints; ++dp) {
                    joc_mix_mtx[obj][dp] = new float[ChannelCount][];
                    joc_channel_idx[obj][dp] = new int[maxBands];
                    joc_vec[obj][dp] = new int[maxBands];
                    joc_mtx[obj][dp] = new int[ChannelCount][];
                    for (int ch = 0; ch < ChannelCount; ++ch) {
                        joc_mix_mtx[obj][dp][ch] = new float[QuadratureMirrorFilterBank.subbands];
                        joc_mtx[obj][dp][ch] = new int[maxBands];
                    }
                }
                for (int ts = 0; ts < maxTimeslots; ++ts) {
                    joc_mix_mtx_interp[obj][ts] = new float[ChannelCount][];
                    for (int ch = 0; ch < ChannelCount; ++ch)
                        joc_mix_mtx_interp[obj][ts][ch] = new float[QuadratureMirrorFilterBank.subbands];
                }
            }
        }

        /// <summary>
        /// Create fast LUTs from compressed data.
        /// </summary>
        static void SetupStaticCache() {
            byte[][] mapping = JointObjectCodingTables.parameterBandMapping;
            for (int i = 0; i < mapping.Length; ++i) {
                byte[] expanded = new byte[QuadratureMirrorFilterBank.subbands];
                for (byte sb = 0; sb < expanded.Length; ++sb) {
                    int pb = Array.BinarySearch(mapping[i], sb);
                    if (pb < 0)
                        pb = ~pb - 1;
                    expanded[sb] = (byte)pb;
                }
                mapping[i] = expanded;
            }
        }
    }
}
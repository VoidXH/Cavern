namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Helps reuse large memory allocations for <see cref="JointObjectCoding"/>.
    /// </summary>
    class JointObjectCodingCache {
        public int[][] joc_offset_ts;
        public int[][][] joc_channel_idx;
        public int[][][] joc_vec;
        public int[][][][] joc_mtx;

        /// <summary>
        /// Create reusable arrays for the given number of <paramref name="objects"/>.
        /// </summary>
        public JointObjectCodingCache(int objects) => RecreateCache(objects);

        /// <summary>
        /// Checks if the cache is ready for the given number of <paramref name="objects"/>, and fixes if it's not.
        /// </summary>
        public void Update(int objects) {
            if (joc_offset_ts.Length != objects)
                RecreateCache(objects);
        }

        /// <summary>
        /// Generate arrays for the given number of <paramref name="objects"/>.
        /// </summary>
        void RecreateCache(int objects) {
            int maxBands = JointObjectCodingTables.joc_num_bands[^1];
            int maxChannels = JointObjectCodingTables.inputMatrix.Length;
            const int maxDataPoints = 2;
            joc_offset_ts = new int[objects][];
            joc_channel_idx = new int[objects][][];
            joc_vec = new int[objects][][];
            joc_mtx = new int[objects][][][];

            for (int obj = 0; obj < objects; ++obj) {
                joc_channel_idx[obj] = new int[maxDataPoints][];
                joc_vec[obj] = new int[maxDataPoints][];
                joc_mtx[obj] = new int[maxDataPoints][][];
                for (int dp = 0; dp < maxDataPoints; ++dp) {
                    joc_channel_idx[obj][dp] = new int[maxBands];
                    joc_vec[obj][dp] = new int[maxBands];
                    joc_mtx[obj][dp] = new int[maxChannels][];
                    for (int ch = 0; ch < maxChannels; ++ch)
                        joc_mtx[obj][dp][ch] = new int[maxBands];
                }
            }
        }
    }
}
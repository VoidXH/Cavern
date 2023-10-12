using System;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    // Reusable arrays
    partial class JointObjectCoding {
        /// <summary>
        /// Previous JOC mixing matrix values.
        /// </summary>
        readonly float[][][] prevMatrix;

        /// <summary>
        /// If true, the temporal extension transition will not be linearly interpolated, only stepped (joc_slope_idx).
        /// </summary>
        bool[] steepSlope;

        /// <summary>
        /// Indexing value for given <see cref="bands"/> values in cache tables (joc_num_bands_idx).
        /// </summary>
        byte[] bandsIndex;

        /// <summary>
        /// Number of processed bands of each object (joc_num_bands).
        /// </summary>
        byte[] bands;

        /// <summary>
        /// Index of the used quantization table for each object.
        /// </summary>
        byte[] quantizationTable;

        /// <summary>
        /// Number of data points for each object.
        /// </summary>
        int[] dataPoints;

        /// <summary>
        /// The given object is using sparse coding (<see cref="jocChannel"/> and <see cref="jocVector"/>) instead
        /// of a fully encoded matrix (<see cref="jocMatrix"/>) (b_joc_sparse).
        /// </summary>
        bool[] sparseCoded;

        /// <summary>
        /// Source channel indexes in sparse mode (one object is only sourced from one channel).
        /// </summary>
        int[][][] jocChannel;

        /// <summary>
        /// Quantized data for each source marked by <see cref="jocChannel"/>.
        /// </summary>
        int[][][] jocVector;

        /// <summary>
        /// Quantized and differentially coded JOC mixing matrix.
        /// </summary>
        int[][][][] jocMatrix;

        /// <summary>
        /// Decoded JOC matrix.
        /// </summary>
        float[][][][] mixMatrix;

        /// <summary>
        /// Decoded JOC matrix with fading.
        /// </summary>
        float[][][][] interpolatedMatrix;

        /// <summary>
        /// Timeslot indexes where the source matrix changes for specific encodings
        /// (like when steep slopes switch between previous and current matrices).
        /// </summary>
        int[][] timeslotOffsets;

        /// <summary>
        /// Create a JOC decoder. Always reuse a previous one as history data is required for decoding.
        /// </summary>
        public JointObjectCoding() {
            int maxChannels = JointObjectCodingTables.inputMatrix.Length;
            prevMatrix = new float[maxObjects][][];
            for (int obj = 0; obj < maxObjects; ++obj) {
                float[][] objMatrix = prevMatrix[obj] = new float[maxChannels][];
                for (int ch = 0; ch < maxChannels; ++ch) {
                    objMatrix[ch] = new float[QuadratureMirrorFilterBank.subbands];
                }
            }
        }

        /// <summary>
        /// Checks if the cache is ready for the given number of objects and channels, and fixes if it's not.
        /// </summary>
        public void UpdateCache() {
            if (ObjectActive.Length == ObjectCount && jocMatrix[0][0].Length == ChannelCount) {
                return;
            }

            if (JointObjectCodingTables.parameterBandMapping[0].Length == 1) {
                SetupStaticCache();
            }

            ObjectActive = new bool[ObjectCount];
            bandsIndex = new byte[ObjectCount];
            bands = new byte[ObjectCount];
            sparseCoded = new bool[ObjectCount];
            quantizationTable = new byte[ObjectCount];
            steepSlope = new bool[ObjectCount];
            mixMatrix = new float[ObjectCount][][][];
            interpolatedMatrix = new float[ObjectCount][][][];
            dataPoints = new int[ObjectCount];
            timeslotOffsets = new int[ObjectCount][];
            jocChannel = new int[ObjectCount][][];
            jocVector = new int[ObjectCount][][];
            jocMatrix = new int[ObjectCount][][][];

            int maxBands = JointObjectCodingTables.joc_num_bands[^1];
            const int maxDataPoints = 2;
            const int maxTimeslots = 1536 / QuadratureMirrorFilterBank.subbands;
            for (int obj = 0; obj < ObjectCount; ++obj) {
                mixMatrix[obj] = new float[maxDataPoints][][];
                interpolatedMatrix[obj] = new float[maxTimeslots][][];
                timeslotOffsets[obj] = new int[maxDataPoints];
                jocChannel[obj] = new int[maxDataPoints][];
                jocVector[obj] = new int[maxDataPoints][];
                jocMatrix[obj] = new int[maxDataPoints][][];
                for (int dp = 0; dp < maxDataPoints; ++dp) {
                    mixMatrix[obj][dp] = new float[ChannelCount][];
                    jocChannel[obj][dp] = new int[maxBands];
                    jocVector[obj][dp] = new int[maxBands];
                    jocMatrix[obj][dp] = new int[ChannelCount][];
                    for (int ch = 0; ch < ChannelCount; ++ch) {
                        mixMatrix[obj][dp][ch] = new float[QuadratureMirrorFilterBank.subbands];
                        jocMatrix[obj][dp][ch] = new int[maxBands];
                    }
                }
                for (int ts = 0; ts < maxTimeslots; ++ts) {
                    interpolatedMatrix[obj][ts] = new float[ChannelCount][];
                    for (int ch = 0; ch < ChannelCount; ++ch) {
                        interpolatedMatrix[obj][ts][ch] = new float[QuadratureMirrorFilterBank.subbands];
                    }
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
                    if (pb < 0) {
                        pb = ~pb - 1;
                    }
                    expanded[sb] = (byte)pb;
                }
                mapping[i] = expanded;
            }
        }

        /// <summary>
        /// Maximum number of objects to render.
        /// </summary>
        const int maxObjects = 64;
    }
}
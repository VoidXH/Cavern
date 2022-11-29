namespace Cavern.Format.Transcoders {
    // Contains the last read common data shared between all allocations.
    partial class EnhancedAC3Body {
        /// <summary>
        /// Index of the next mantissa in <see cref="bap1Next"/>.
        /// </summary>
        public int bap1Pos;

        /// <summary>
        /// Index of the next mantissa in <see cref="bap2Next"/>.
        /// </summary>
        public int bap2Pos;

        /// <summary>
        /// Index of the next mantissa in <see cref="bap4Next"/>.
        /// </summary>
        public int bap4Pos;

        /// <summary>
        /// Next mantissa values in case the bap is 1.
        /// </summary>
        public int[] bap1Next;

        /// <summary>
        /// Next mantissa values in case the bap is 2.
        /// </summary>
        public int[] bap2Next;

        /// <summary>
        /// Next mantissa values in case the bap is 4.
        /// </summary>
        public int[] bap4Next;
    }
}
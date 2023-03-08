using Cavern.Format.Utilities;

namespace Cavern.Format.Transcoders {
    partial class EnhancedAC3Body {
        /// <summary>
        /// Handling of delta bit allocation data.
        /// </summary>
        enum DeltaBitAllocationMode {
            /// <summary>
            /// Use the old delta bit allocation data for the current block too.
            /// </summary>
            Reuse = 0,
            /// <summary>
            /// Read new delta bit allocation data.
            /// </summary>
            NewInfoFollows,
            /// <summary>
            /// Don't use delta bit allocation, even if it's read.
            /// </summary>
            NoAllocation,
            /// <summary>
            /// Reserved value, which is handled by muting the stream.
            /// </summary>
            MuteOutput
        }

        /// <summary>
        /// Delta bit allocation information.
        /// </summary>
        struct DeltaBitAllocation {
            /// <summary>
            /// Handling of delta bit allocation data (cpldeltba, deltba).
            /// </summary>
            public DeltaBitAllocationMode enabled;

            /// <summary>
            /// First bit allocation band (cpldeltoffst, deltoffst).
            /// </summary>
            public int[] Offset { get; private set; }

            /// <summary>
            /// Bit allocation band span (cpldeltlen, deltlen).
            /// </summary>
            public int[] Length { get; private set; }

            /// <summary>
            /// Adjustment of the bit allocation mask (cpldeltba, deltba).
            /// </summary>
            public int[] BitAllocation { get; private set; }

            /// <summary>
            /// Set the default values to mark an unused delta bit allocation.
            /// </summary>
            public void Reset() {
                enabled = DeltaBitAllocationMode.NoAllocation;
                Offset = new int[0];
                Length = null;
                BitAllocation = null;
            }

            /// <summary>
            /// Read the delta bit allocation from the bitstream.
            /// </summary>
            public void Read(BitExtractor extractor) {
                int segments = extractor.Read(3) + 1;
                if (Offset.Length != segments) {
                    Offset = new int[segments];
                    Length = new int[segments];
                    BitAllocation = new int[segments];
                }
                for (int segment = 0; segment < segments; segment++) {
                    Offset[segment] = extractor.Read(5);
                    Length[segment] = extractor.Read(4);
                    BitAllocation[segment] = extractor.Read(3);
                }
            }

            /// <summary>
            /// Write the delta bit allocation to the bitstream.
            /// </summary>
            public void Write(BitPlanter planter) {
                planter.Write(Offset.Length - 1, 3);
                for (int segment = 0; segment < Offset.Length; segment++) {
                    planter.Write(Offset[segment], 5);
                    planter.Write(Length[segment], 4);
                    planter.Write(BitAllocation[segment], 3);
                }
            }
        }
    }
}
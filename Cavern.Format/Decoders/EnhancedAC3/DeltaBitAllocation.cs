using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Handling of delta bit allocation data.
    /// </summary>
    enum DeltaBitAllocationMode {
        Reuse = 0,
        NewInfoFollows,
        NoAllocation,
        MuteOutput
    }

    /// <summary>
    /// Delta bit allocation information.
    /// </summary>
    struct DeltaBitAllocation {
        public DeltaBitAllocationMode enabled;
        public int[] Offset { get; private set; }
        public int[] Length { get; private set; }
        public int[] BitAllocation { get; private set; }

        public void Reset() {
            enabled = DeltaBitAllocationMode.NoAllocation;
            Offset = new int[0];
            Length = null;
            BitAllocation = null;
        }

        public void Read(BitExtractor extractor) {
            int segments = extractor.Read(3) + 1;
            Offset = new int[segments];
            Length = new int[segments];
            BitAllocation = new int[segments];
            for (int segment = 0; segment < segments; ++segment) {
                Offset[segment] = extractor.Read(5);
                Length[segment] = extractor.Read(4);
                BitAllocation[segment] = extractor.Read(3);
            }
        }
    }
}
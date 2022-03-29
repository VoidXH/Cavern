using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Decodes a single payload from an EMDF stream.
    /// </summary>
    class ExtensibleMetadataPayload {
        /// <summary>
        /// Payload identifier.
        /// </summary>
        public int ID { get; }

        /// <summary>
        /// This payload applies this many samples later.
        /// </summary>
        public int SampleOffset { get; private set; }

        /// <summary>
        /// Raw payload data.
        /// </summary>
        public byte[] Payload { get; private set; }

        /// <summary>
        /// Decodes a single payload from an EMDF stream.
        /// </summary>
        public ExtensibleMetadataPayload(int id, BitExtractor extractor) {
            ID = id;

            bool sampleOffset;
            if (sampleOffset = extractor.ReadBit()) {
                SampleOffset = extractor.Read(11);
                extractor.Skip(1);
            }

            if (extractor.ReadBit())
                extractor.VariableBits(11);
            if (extractor.ReadBit())
                extractor.VariableBits(2);
            if (extractor.ReadBit())
                extractor.Skip(8);

            if (!extractor.ReadBit()) {
                bool frameAligned = false;
                if (!sampleOffset) {
                    frameAligned = extractor.ReadBit();
                    if (frameAligned)
                        extractor.Skip(2);
                }
                if (sampleOffset || frameAligned)
                    extractor.Skip(7);
            }

            Payload = extractor.ReadBytes(extractor.VariableBits(8));
        }
    }
}
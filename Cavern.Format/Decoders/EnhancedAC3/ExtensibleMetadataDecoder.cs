using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Decodes Extensible Metadata Delivery Format (EMDF) from the reserved fields of an E-AC-3 bitstream.
    /// </summary>
    class ExtensibleMetadataDecoder {
        /// <summary>
        /// The last EBML frame contained object data.
        /// </summary>
        public bool HasObjects { get; private set; }

        /// <summary>
        /// The Joint Object Coding data in this frame.
        /// </summary>
        /// <remarks>There can be only one JOC payload in every E-AC-3 frame.</remarks>
        public JointObjectCoding JOC { get; } = new JointObjectCoding();

        /// <summary>
        /// The Object Audio Metadata in this frame.
        /// </summary>
        /// <remarks>There can be only one OAMD payload in every E-AC-3 frame.</remarks>
        public ObjectAudioMetadata OAMD { get; } = new ObjectAudioMetadata();

        /// <summary>
        /// Decode the next EMDF frame from a bitstream.
        /// </summary>
        public void Decode(BitExtractor extractor) {
            HasObjects = false;
            int syncword = 0;
            while (extractor.Position < extractor.BackPosition - 32) {
                syncword = ((syncword << 8) & 0xFFFF) + extractor.Read(8); // Syncwords are byte-padded
                if (syncword == syncWord && DecodeBlock(extractor)) {
                    break;
                }
            }
        }

        /// <summary>
        /// Tries to decode an EMDF block, returns if succeeded.
        /// </summary>
        /// <remarks>Assumes the sync word was already read.</remarks>
        bool DecodeBlock(BitExtractor extractor) {
            int length = extractor.Read(16);
            int frameEndPos = extractor.Position + length * 8;
            if (frameEndPos > extractor.BackPosition) {
                return false;
            }

            int version = extractor.Read(2);
            if (version == 3) {
                version += extractor.VariableBits(2);
            }
            int key = extractor.Read(3);
            if (key == 7) {
                key += extractor.VariableBits(3);
            }
            if (version != 0 || key != 0) {
                return false;
            }

            int payloadID;
            while (extractor.Position < frameEndPos && (payloadID = extractor.Read(5)) != 0) {
                if (payloadID == 0x1F) {
                    payloadID += extractor.VariableBits(5);
                }
                if (payloadID > jocPayloadID) {
                    return false;
                }

                bool hasSampleOffset;
                int sampleOffset = 0;
                if (hasSampleOffset = extractor.ReadBit()) {
                    sampleOffset = extractor.Read(12) >> 1; // Skip 1 bit
                }

                if (extractor.ReadBit()) {
                    extractor.VariableBits(11);
                }
                if (extractor.ReadBit()) {
                    extractor.VariableBits(2);
                }
                if (extractor.ReadBit()) {
                    extractor.Skip(8);
                }

                if (!extractor.ReadBit()) {
                    bool frameAligned = false;
                    if (!hasSampleOffset) {
                        frameAligned = extractor.ReadBit();
                        if (frameAligned) {
                            extractor.Skip(2);
                        }
                    }
                    if (hasSampleOffset || frameAligned) {
                        extractor.Skip(7);
                    }
                }

                int payloadEnd = extractor.VariableBits(8) * 8 + extractor.Position;
                if (payloadEnd > extractor.BackPosition) {
                    return false;
                }
                if (payloadID == jocPayloadID) {
                    JOC.Decode(extractor);
                    HasObjects = true;
                } else if (payloadID == oamdPayloadID) {
                    OAMD.Decode(extractor, sampleOffset);
                }
                extractor.Position = payloadEnd;
            }
            return true;
        }

        /// <summary>
        /// EMDF marker.
        /// </summary>
        const int syncWord = 0x5838;

        /// <summary>
        /// Payload ID for Object Audio Metadata.
        /// </summary>
        const int oamdPayloadID = 11;

        /// <summary>
        /// Payload ID for Joint Object Coding.
        /// </summary>
        const int jocPayloadID = 14;
    }
}
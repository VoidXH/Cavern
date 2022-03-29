using System.Collections.Generic;

using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Decodes Extensible Metadata Delivery Format (EMDF) from the reserved fields of an E-AC-3 bitstream.
    /// </summary>
    class ExtensibleMetadataDecoder {
        /// <summary>
        /// EMDF marker.
        /// </summary>
        const int syncWord = 0x5838;

        /// <summary>
        /// Payload ID for Object Audio Metadata.
        /// </summary>
        internal const int oamdPayloadID = 11;

        /// <summary>
        /// Payload ID for Joint Object Coding.
        /// </summary>
        const int jocPayloadID = 14;

        /// <summary>
        /// This decoder has found and decoded an EMDF frame.
        /// </summary>
        public bool IsValid { get; private set; }

        /// <summary>
        /// Payloads contained in this EMDF document.
        /// </summary>
        public IReadOnlyList<ExtensibleMetadataPayload> Payloads => payloads;

        /// <summary>
        /// The Joint Object Coding data in this frame.
        /// </summary>
        /// <remarks>There can be only one JOC payload in every E-AC-3 frame.</remarks>
        public JointObjectCoding JOC { get; private set; }

        /// <summary>
        /// The Object Audio Metadata in this frame.
        /// </summary>
        /// <remarks>There can be only one OAMD payload in every E-AC-3 frame.</remarks>
        public ObjectAudioMetadata OAMD { get; private set; }

        /// <summary>
        /// EMDF source stream.
        /// </summary>
        readonly BitExtractor extractor;

        /// <summary>
        /// Payloads contained in this EMDF document.
        /// </summary>
        readonly List<ExtensibleMetadataPayload> payloads = new List<ExtensibleMetadataPayload>();

        /// <summary>
        /// Decode EMDF from a bitstream.
        /// </summary>
        public ExtensibleMetadataDecoder(BitExtractor extractor) {
            this.extractor = extractor;
            while (extractor.Position != extractor.BackPosition - 16) {
                int syncword = extractor.Peek(16);
                if ((syncword == syncWord) && (IsValid = Decode()))
                    break;
                ++extractor.Position;
            }
        }

        /// <summary>
        /// Get a payload by its ID if it exists.
        /// </summary>
        public ExtensibleMetadataPayload GetPayloadByID(int id) {
            for (int i = 0, c = payloads.Count; i < c; ++i)
                if (payloads[i].ID == id)
                    return payloads[i];
            return null;
        }

        /// <summary>
        /// Tries to decode an EMDF block, returns if succeeded.
        /// </summary>
        bool Decode() {
            if (extractor.Read(16) != syncWord) {
                IsValid = false;
                return false;
            }

            if (extractor.Position + extractor.Read(16) /* length */ > extractor.BackPosition) {
                IsValid = false;
                return false;
            }
            int version = extractor.Read(2);
            if (version == 3)
                version += extractor.VariableBits(2);
            int key = extractor.Read(3);
            if (key == 7)
                key += extractor.VariableBits(3);
            if (version != 0 || key != 0)
                return false;
            int payloadID;
            while ((payloadID = extractor.Read(5)) != 0) {
                if (payloadID == 0x1F)
                    payloadID += extractor.VariableBits(5);
                if (payloadID > jocPayloadID) {
                    payloads.Clear();
                    JOC = null;
                    OAMD = null;
                    return false;
                }
                ExtensibleMetadataPayload payload = new ExtensibleMetadataPayload(payloadID, extractor);
                payloads.Add(payload);
                if (payloadID == jocPayloadID)
                    JOC = new JointObjectCoding(payload);
                else if (payloadID == oamdPayloadID)
                    OAMD = new ObjectAudioMetadata(payload);
            }
            return true;
        }
    }
}
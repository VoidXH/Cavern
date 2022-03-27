using System.Collections.Generic;

using Cavern.Format.Common;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Decodes Extensible Metadata Delivery Format (EMDF) from the reserved fields of an E-AC3 bitstream.
    /// </summary>
    class ExtensibleMetadataDecoder {
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
        /// <remarks>There can be only one JOC payload in every E-AC3 frame.</remarks>
        public JointObjectCoding JOC { get; private set; }

        /// <summary>
        /// The Object Audio Metadata in this frame.
        /// </summary>
        /// <remarks>There can be only one OAMD payload in every E-AC3 frame.</remarks>
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
                if (syncword == syncWord) {
                    IsValid = true;
                    break;
                }
                ++extractor.Position;
            }
            if (IsValid)
                Decode();
        }

        void Decode() {
            if (extractor.Read(16) != syncWord) {
                IsValid = false;
                return;
            }
            int emdf_container_length = extractor.Read(16);
            if (extractor.Position + emdf_container_length > extractor.BackPosition) {
                IsValid = false;
                return;
            }
            int emdf_version = extractor.Read(2);
            if (emdf_version == 3)
                emdf_version += extractor.VariableBits(2);
            int key_id = extractor.Read(3);
            if (key_id == 7)
                key_id += extractor.VariableBits(3);
            if (emdf_version != 0 || key_id != 0)
                throw new UnsupportedFeatureException("EMDFextra");
            int emdf_payload_id;
            while ((emdf_payload_id = extractor.Read(5)) != 0) {
                if (emdf_payload_id == 0x1F)
                    emdf_payload_id += extractor.VariableBits(5);
                ExtensibleMetadataPayload payload = new ExtensibleMetadataPayload(emdf_payload_id, extractor);
                payloads.Add(payload);
                if (payload.ID == jocPayloadID)
                    JOC = new JointObjectCoding(payload);
                else if (payload.ID == oamdPayloadID)
                    OAMD = new ObjectAudioMetadata(payload);
            }
        }
    }
}
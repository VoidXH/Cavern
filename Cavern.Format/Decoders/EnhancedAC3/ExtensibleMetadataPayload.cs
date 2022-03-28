using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Decodes a single payload from an EMDF stream.
    /// </summary>
    class ExtensibleMetadataPayload {
        public int ID { get; }

        public byte[] Payload { get; private set; }

        /// <summary>
        /// Decodes a single payload from an EMDF stream.
        /// </summary>
        public ExtensibleMetadataPayload(int id, BitExtractor extractor) {
            ID = id;

            if (smploffste = extractor.ReadBit()) {
                smploffst = extractor.Read(11);
                extractor.Skip(1);
            }
            if (duratione = extractor.ReadBit())
                duration = extractor.VariableBits(11);
            if (groupide = extractor.ReadBit())
                groupid = extractor.VariableBits(2);
            if (codecdatae = extractor.ReadBit())
                extractor.Skip(8);

            bool discard_unknown_payload = extractor.ReadBit();
            if (!discard_unknown_payload) {
                bool payload_frame_aligned = false;
                if (!smploffste) {
                    payload_frame_aligned = extractor.ReadBit();
                    if (payload_frame_aligned) {
                        create_duplicate = extractor.ReadBit();
                        remove_duplicate = extractor.ReadBit();
                    }
                }
                if (smploffste || payload_frame_aligned) {
                    priority = extractor.Read(5);
                    proc_allowed = extractor.Read(2);
                }
            }

            int emdf_payload_size = extractor.VariableBits(8);
            Payload = extractor.ReadBytes(emdf_payload_size);
        }

        public void MergeWith(ExtensibleMetadataPayload payload) {

        }

#pragma warning disable IDE0052 // Remove unread private members
        readonly bool codecdatae;
        readonly bool create_duplicate;
        readonly bool duratione;
        readonly bool groupide;
        readonly bool remove_duplicate;
        readonly bool smploffste;
        readonly int duration;
        readonly int groupid;
        readonly int priority;
        readonly int proc_allowed;
        readonly int smploffst;
#pragma warning restore IDE0052 // Remove unread private members
    }
}
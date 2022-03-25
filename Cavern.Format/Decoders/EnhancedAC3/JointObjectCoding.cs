using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// A decoded JOC frame from an EMDF payload.
    /// </summary>
    class JointObjectCoding {
        /// <summary>
        /// Decodes a JOC frame from an EMDF payload.
        /// </summary>
        public JointObjectCoding(ExtensibleMetadataPayload payload) {
            BitExtractor extractor = new BitExtractor(payload.Payload);
            DecodeHeader(extractor);
            DecodeInfo(extractor);
            DecodeData(extractor);
        }

        void DecodeHeader(BitExtractor extractor) {
            joc_dmx_config_idx = extractor.Read(3);
            joc_num_objects = extractor.Read(6) + 1;
            joc_ext_config_idx = extractor.Read(3);

            b_joc_obj_present = new bool[joc_num_objects];
            joc_num_bands_idx = new int[joc_num_objects];
            b_joc_sparse = new bool[joc_num_objects];
            joc_num_quant_idx = new bool[joc_num_objects];
            joc_slope_idx = new bool[joc_num_objects];
            joc_num_dpoints = new int[joc_num_objects];
            joc_offset_ts_bits = new int[joc_num_objects, 2];
        }

        void DecodeInfo(BitExtractor extractor) {
            joc_clipgain_x_bits = extractor.Read(3);
            joc_clipgain_y_bits = extractor.Read(5);
            joc_seq_count_bits = extractor.Read(10);
            for (int obj = 0; obj < joc_num_objects; ++obj) {
                if (b_joc_obj_present[obj] = extractor.ReadBit()) {
                    joc_num_bands_idx[obj] = extractor.Read(3);
                    b_joc_sparse[obj] = extractor.ReadBit();
                    joc_num_quant_idx[obj] = extractor.ReadBit();

                    // joc_data_point_info
                    joc_slope_idx[obj] = extractor.ReadBit();
                    joc_num_dpoints[obj] = extractor.Read(1) + 1;
                    if (joc_slope_idx[obj])
                        for (int dp = 0; dp < joc_num_dpoints[obj]; ++dp)
                            joc_offset_ts_bits[obj, dp] = extractor.Read(5);
                }
            }
        }

        void DecodeData(BitExtractor extractor) {
            // TODO
        }

#pragma warning disable IDE0052 // Remove unread private members
        bool[] b_joc_obj_present;
        bool[] b_joc_sparse;
        bool[] joc_num_quant_idx;
        bool[] joc_slope_idx;
        int joc_dmx_config_idx;
        int joc_ext_config_idx;
        int joc_num_objects;
        int joc_clipgain_x_bits;
        int joc_clipgain_y_bits;
        int joc_seq_count_bits;
        int[] joc_num_bands_idx;
        int[] joc_num_dpoints;
        int[,] joc_offset_ts_bits;
#pragma warning restore IDE0052 // Remove unread private members
    }
}
using Cavern.Format.Common;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// A decoded JOC frame from an EMDF payload.
    /// </summary>
    partial class JointObjectCoding {
        /// <summary>
        /// Number of full bandwidth input channels.
        /// </summary>
        public int ChannelCount { get; private set; }

        /// <summary>
        /// Number of rendered dynamic objects.
        /// </summary>
        public int ObjectCount { get; private set; }

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
            if (joc_dmx_config_idx > 4)
                throw new UnsupportedFeatureException("DMXconfig");
            ChannelCount = (joc_dmx_config_idx == 0 || joc_dmx_config_idx == 3) ? 5 : 7;
            ObjectCount = extractor.Read(6) + 1;
            joc_ext_config_idx = extractor.Read(3);

            b_joc_obj_present = new bool[ObjectCount];
            joc_num_bands = new byte[ObjectCount];
            b_joc_sparse = new bool[ObjectCount];
            joc_num_quant_idx = new bool[ObjectCount];
            joc_slope_idx = new bool[ObjectCount];
            joc_num_dpoints = new int[ObjectCount];
            joc_offset_ts = new int[ObjectCount][];
            joc_channel_idx = new int[ObjectCount][][];
            joc_vec = new int[ObjectCount][][];
            joc_mtx = new int[ObjectCount][][][];
        }

        void DecodeInfo(BitExtractor extractor) {
            joc_clipgain_x_bits = extractor.Read(3);
            joc_clipgain_y_bits = extractor.Read(5);
            joc_seq_count_bits = extractor.Read(10);
            for (int obj = 0; obj < ObjectCount; ++obj) {
                if (b_joc_obj_present[obj] = extractor.ReadBit()) {
                    joc_num_bands[obj] = JointObjectCodingTables.joc_num_bands[extractor.Read(3)];
                    b_joc_sparse[obj] = extractor.ReadBit();
                    joc_num_quant_idx[obj] = extractor.ReadBit();

                    // joc_data_point_info
                    joc_slope_idx[obj] = extractor.ReadBit();
                    joc_num_dpoints[obj] = extractor.Read(1) + 1;
                    if (joc_slope_idx[obj]) {
                        joc_offset_ts[obj] = new int[2];
                        for (int dp = 0; dp < joc_num_dpoints[obj]; ++dp)
                            joc_offset_ts[obj][dp] = extractor.Read(5) + 1;
                    }

                    // Allocation
                    joc_channel_idx[obj] = new int[joc_num_dpoints[obj]][];
                    joc_vec[obj] = new int[joc_num_dpoints[obj]][];
                    joc_mtx[obj] = new int[joc_num_dpoints[obj]][][];
                    for (int dp = 0; dp < joc_num_dpoints[obj]; ++dp) {
                        joc_channel_idx[obj][dp] = new int[joc_num_bands[obj]];
                        joc_vec[obj][dp] = new int[joc_num_bands[obj]];
                        joc_mtx[obj][dp] = new int[ChannelCount][];
                        for (int ch = 0; ch < ChannelCount; ++ch)
                            joc_mtx[obj][dp][ch] = new int[joc_num_bands[obj]];
                    }
                }
            }
        }

        void DecodeData(BitExtractor extractor) {
            for (int obj = 0; obj < ObjectCount; ++obj) {
                if (b_joc_obj_present[obj]) {
                    for (int dp = 0; dp < joc_num_dpoints[obj]; ++dp) {
                        int[][] joc_huff_code;
                        if (b_joc_sparse[obj]) {
                            joc_channel_idx[obj][dp][0] = extractor.Read(3);
                            joc_huff_code = JointObjectCodingTables.GetHuffCodeTable(ChannelCount, HuffmanType.IDX);
                            for (int pb = 1; pb < joc_num_bands[obj]; ++pb)
                                joc_channel_idx[obj][dp][pb] = HuffmanDecode(joc_huff_code, extractor);
                            joc_huff_code =
                                JointObjectCodingTables.GetHuffCodeTable(joc_num_quant_idx[obj] ? 1 : 0, HuffmanType.VEC);
                            for (int pb = 0; pb < joc_num_bands[obj]; ++pb)
                                joc_vec[obj][dp][pb] = HuffmanDecode(joc_huff_code, extractor);
                        } else {
                            joc_huff_code = JointObjectCodingTables.GetHuffCodeTable(joc_num_quant_idx[obj] ? 1 : 0,
                                HuffmanType.MTX);
                            for (int ch = 0; ch < ChannelCount; ++ch)
                                for (int pb = 0; pb < joc_num_bands[obj]; ++pb)
                                    joc_mtx[obj][dp][ch][pb] = HuffmanDecode(joc_huff_code, extractor);
                        }
                    }
                }
            }
        }

        int HuffmanDecode(int[][] joc_huff_code, BitExtractor extractor) {
            int node = 0;
            do {
                node = joc_huff_code[node][extractor.ReadBit() ? 1 : 0];
            } while (node > 0);
            return -1 - node;
        }

#pragma warning disable IDE0052 // Remove unread private members
        bool[] b_joc_obj_present;
        bool[] b_joc_sparse;
        bool[] joc_num_quant_idx;
        bool[] joc_slope_idx;
        byte[] joc_num_bands;
        int joc_dmx_config_idx;
        int joc_ext_config_idx;
        int joc_clipgain_x_bits;
        int joc_clipgain_y_bits;
        int joc_seq_count_bits;
        int[] joc_num_dpoints;
        int[][] joc_offset_ts;
        int[][][] joc_channel_idx;
        int[][][] joc_vec;
        int[][][][] joc_mtx;
#pragma warning restore IDE0052 // Remove unread private members
    }
}
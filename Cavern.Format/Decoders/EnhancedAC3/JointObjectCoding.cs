using Cavern.Format.Common;
using Cavern.Format.Utilities;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    /// <summary>
    /// Joint object coding decoder and renderer.
    /// </summary>
    partial class JointObjectCoding {
        /// <summary>
        /// The object is active and will have rendered audio data.
        /// </summary>
        // TODO: mute inactive objects
        public bool[] ObjectActive = new bool[0];

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
        public void Decode(BitExtractor extractor) {
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
            UpdateCache();
            joc_ext_config_idx = extractor.Read(3);
        }

        void DecodeInfo(BitExtractor extractor) {
            joc_clipgain_x_bits = extractor.Read(3);
            joc_clipgain_y_bits = extractor.Read(5);
            joc_seq_count_bits = extractor.Read(10);
            for (int obj = 0; obj < ObjectCount; ++obj) {
                if (ObjectActive[obj] = extractor.ReadBit()) {
                    joc_num_bands[obj] = JointObjectCodingTables.joc_num_bands[extractor.Read(3)];
                    b_joc_sparse[obj] = extractor.ReadBit();
                    joc_num_quant_idx[obj] = extractor.ReadBit();

                    // joc_data_point_info
                    joc_slope_idx[obj] = extractor.ReadBit();
                    dataPoints[obj] = extractor.Read(1) + 1;
                    if (joc_slope_idx[obj])
                        for (int dp = 0; dp < dataPoints[obj]; ++dp)
                            joc_offset_ts[obj][dp] = extractor.Read(5) + 1;
                }
            }
        }

        void DecodeData(BitExtractor extractor) {
            for (int obj = 0; obj < ObjectCount; ++obj) {
                if (ObjectActive[obj]) {
                    for (int dp = 0; dp < dataPoints[obj]; ++dp) {
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
        int joc_dmx_config_idx;
        int joc_ext_config_idx;
        int joc_clipgain_x_bits;
        int joc_clipgain_y_bits;
        int joc_seq_count_bits;
#pragma warning restore IDE0052 // Remove unread private members
    }
}
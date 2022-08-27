using System;

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
        /// Multiplier for the output signal's amplitude.
        /// </summary>
        public float Gain { get; private set; }

        /// <summary>
        /// Decodes a JOC frame from an EMDF payload.
        /// </summary>
        public void Decode(BitExtractor extractor) {
            DecodeHeader(extractor);
            DecodeInfo(extractor);
            DecodeData(extractor);
        }

        void DecodeHeader(BitExtractor extractor) {
            int downmixConfig = extractor.Read(3);
            if (downmixConfig > 4) {
                throw new UnsupportedFeatureException("joc_dmx_config_idx");
            }
            ChannelCount = (downmixConfig == 0 || downmixConfig == 3) ? 5 : 7;
            ObjectCount = extractor.Read(6) + 1;
            UpdateCache();
            if (extractor.Read(3) != 0) {
                throw new UnsupportedFeatureException("joc_ext_config_idx");
            }
        }

        void DecodeInfo(BitExtractor extractor) {
            int gainPower = extractor.Read(3);
            Gain = 1 + (extractor.Read(5) / 32f) * MathF.Pow(2, gainPower - 4);
            extractor.Skip(10); // Sequence counter
            for (int obj = 0; obj < ObjectCount; ++obj) {
                if (ObjectActive[obj] = extractor.ReadBit()) {
                    bandsIndex[obj] = (byte)extractor.Read(3);
                    bands[obj] = JointObjectCodingTables.joc_num_bands[bandsIndex[obj]];
                    sparseCoded[obj] = extractor.ReadBit();
                    quantizationTable[obj] = (byte)extractor.ReadBitInt();

                    // joc_data_point_info
                    steepSlope[obj] = extractor.ReadBit();
                    dataPoints[obj] = extractor.Read(1) + 1;
                    if (steepSlope[obj]) {
                        for (int dp = 0; dp < dataPoints[obj]; ++dp) {
                            timeslotOffsets[obj][dp] = extractor.Read(5) + 1;
                        }
                    }
                }
            }
        }

        void DecodeData(BitExtractor extractor) {
            for (int obj = 0; obj < ObjectCount; ++obj) {
                if (ObjectActive[obj]) {
                    for (int dp = 0; dp < dataPoints[obj]; ++dp) {
                        int[][] codeTable;
                        if (sparseCoded[obj]) {
                            jocChannel[obj][dp][0] = extractor.Read(3);
                            codeTable = JointObjectCodingTables.GetHuffCodeTable(ChannelCount, HuffmanType.IDX);
                            for (int pb = 1; pb < bands[obj]; ++pb) {
                                jocChannel[obj][dp][pb] = HuffmanDecode(codeTable, extractor);
                            }
                            codeTable =
                                JointObjectCodingTables.GetHuffCodeTable(quantizationTable[obj], HuffmanType.VEC);
                            for (int pb = 0; pb < bands[obj]; ++pb) {
                                jocVector[obj][dp][pb] = HuffmanDecode(codeTable, extractor);
                            }
                        } else {
                            codeTable = JointObjectCodingTables.GetHuffCodeTable(quantizationTable[obj], HuffmanType.MTX);
                            for (int ch = 0; ch < ChannelCount; ++ch) {
                                for (int pb = 0; pb < bands[obj]; ++pb) {
                                    jocMatrix[obj][dp][ch][pb] = HuffmanDecode(codeTable, extractor);
                                }
                            }
                        }
                    }
                }
            }
        }

        int HuffmanDecode(int[][] codeTable, BitExtractor extractor) {
            int node = 0;
            do {
                node = codeTable[node][extractor.ReadBit() ? 1 : 0];
            } while (node > 0);
            return -1 - node;
        }
    }
}
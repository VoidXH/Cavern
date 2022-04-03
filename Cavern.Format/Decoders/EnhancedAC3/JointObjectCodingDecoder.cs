using System;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    partial class JointObjectCoding {
        void DecodeSparse(int obj) {
            int[][][] joc_channel_idx = cache.joc_channel_idx;
            int[][][] joc_vec = cache.joc_vec;
            if (b_joc_obj_present[obj]) {
                int nquant = joc_num_quant_idx[obj] ? 192 : 96;
                int offset = joc_num_quant_idx[obj] ? 100 : 50;
                for (int dp = 0; dp < joc_num_dpoints[obj]; ++dp) {
                    for (int pb = 0; pb < joc_num_bands[obj]; ++pb) {
                        int joc_channel_idx_mod;
                        if (pb == 0)
                            joc_channel_idx_mod = joc_channel_idx[obj][dp][pb];
                        else
                            joc_channel_idx_mod = (joc_channel_idx[obj][dp][pb - 1] +
                                joc_channel_idx[obj][dp][pb]) % ChannelCount;
                        for (int ch = 0; ch < ChannelCount; ++ch) {
                            if (ch == joc_channel_idx_mod) {
                                if (pb == 0)
                                    joc_mix_mtx[obj][dp][ch][pb] = (offset + joc_vec[obj][dp][pb]) % nquant;
                                else {
                                    joc_mix_mtx[obj][dp][ch][pb] = (joc_mix_mtx[obj][dp][ch][pb - 1] +
                                    joc_vec[obj][dp][pb]) % nquant;
                                }
                            } else
                                joc_mix_mtx[obj][dp][ch][pb] = offset;
                        }
                    }
                }
            }
        }

        void DecodeCoarse(int obj) {
            int[][][][] joc_mtx = cache.joc_mtx;
            if (b_joc_obj_present[obj]) {
                int nquant = joc_num_quant_idx[obj] ? 192 : 96;
                int offset = joc_num_quant_idx[obj] ? 96 : 48;
                for (int dp = 0; dp < joc_num_dpoints[obj]; ++dp) {
                    for (int ch = 0; ch < ChannelCount; ++ch) {
                        joc_mix_mtx[obj][dp][ch][0] = (offset + joc_mtx[obj][dp][ch][0]) % nquant;
                        for (int pb = 1; pb < joc_num_bands[obj]; ++pb)
                            joc_mix_mtx[obj][dp][ch][pb] = (joc_mix_mtx[obj][dp][ch][pb - 1] +
                                joc_mtx[obj][dp][ch][pb]) % nquant;
                    }
                }
            }
        }

        void DecodeSideInfo() {
            for (int obj = 0; obj < ObjectCount; ++obj) {
                int nquant = joc_num_quant_idx[obj] ? 192 : 96;
                for (int dp = 0; dp < joc_num_dpoints[obj]; ++dp)
                    for (int ch = 0; ch < ChannelCount; ++ch)
                        for (int pb = 0; pb < joc_num_bands[obj]; ++pb)
                            joc_mix_mtx[obj][dp][ch][pb] = (joc_mix_mtx[obj][dp][ch][pb] - nquant / 2) *
                                820f / (4096 * (joc_num_quant_idx[obj] ? 2 : 1));
            }
        }

        void InterpolateSideInfo(int num_qmf_timeslots, float[][][] prevMatrix) {
            int[][] joc_offset_ts = cache.joc_offset_ts;
            for (int obj = 0; obj < ObjectCount; ++obj) {
                for (int ch = 0; ch < ChannelCount; ++ch) {
                    for (byte sb = 0; sb < QuadratureMirrorFilterBank.subbands; ++sb) {
                        int id = Array.BinarySearch(JointObjectCodingTables.joc_num_bands, joc_num_bands[obj]);
                        if (id < 0)
                            id = ~id - 1;
                        int pb = Array.BinarySearch(JointObjectCodingTables.parameterBandMapping[id], sb);
                        if (pb < 0)
                            pb = ~pb - 1;
                        for (int ts = 0; ts < num_qmf_timeslots; ++ts) {
                            if (joc_slope_idx[obj]) {
                                if (joc_num_dpoints[obj] == 1) {
                                    if (ts < joc_offset_ts[obj][0])
                                        joc_mix_mtx_interp[obj][ts][ch][sb] = prevMatrix[obj][ch][sb];
                                    else
                                        joc_mix_mtx_interp[obj][ts][ch][sb] = joc_mix_mtx[obj][0][ch][sb];
                                } else {
                                    if (ts < joc_offset_ts[obj][0])
                                        joc_mix_mtx_interp[obj][ts][ch][sb] = prevMatrix[obj][ch][sb];
                                    else if (ts < joc_offset_ts[obj][1])
                                        joc_mix_mtx_interp[obj][ts][ch][sb] = joc_mix_mtx[obj][0][ch][sb];
                                    else
                                        joc_mix_mtx_interp[obj][ts][ch][sb] = joc_mix_mtx[obj][1][ch][sb];
                                }
                            } else {
                                if (joc_num_dpoints[obj] == 1) {
                                    float delta = joc_mix_mtx[obj][0][ch][pb] - prevMatrix[obj][ch][sb];
                                    joc_mix_mtx_interp[obj][ts][ch][sb] = prevMatrix[obj][ch][sb] +
                                        (ts + 1) * delta / num_qmf_timeslots;
                                } else {
                                    int ts_2 = num_qmf_timeslots / 2;
                                    if (ts < ts_2) {
                                        float delta = joc_mix_mtx[obj][0][ch][pb] - prevMatrix[obj][ch][sb];
                                        joc_mix_mtx_interp[obj][ts][ch][sb] = prevMatrix[obj][ch][sb] +
                                            (ts + 1) * delta / ts_2;
                                    } else {
                                        float delta = joc_mix_mtx[obj][1][ch][pb] - joc_mix_mtx[obj][0][ch][pb];
                                        joc_mix_mtx_interp[obj][ts][ch][sb] = joc_mix_mtx[obj][0][ch][pb] +
                                            (ts - ts_2 + 1) * delta / (num_qmf_timeslots - ts_2);
                                    }
                                }
                            }
                        }
                        prevMatrix[obj][ch][sb] = joc_mix_mtx[obj][joc_num_dpoints[obj] - 1][ch][pb];
                    }
                }
            }
        }

        /// <summary>
        /// Get the object mixing matrices.
        /// </summary>
        /// <param name="frameSize">Length of the entire time window of all time slots</param>
        /// <param name="prevMatrix">Cache array for last interpolation results</param>
        public float[][][][] GetMixingMatrices(int frameSize, float[][][] prevMatrix) {
            int num_qmf_timeslots = frameSize / QuadratureMirrorFilterBank.subbands;
            joc_mix_mtx = new float[ObjectCount][][][];
            joc_mix_mtx_interp = new float[ObjectCount][][][];

            for (int obj = 0; obj < ObjectCount; ++obj) {
                joc_mix_mtx[obj] = new float[joc_num_dpoints[obj]][][];
                for (int dp = 0; dp < joc_num_dpoints[obj]; ++dp) {
                    joc_mix_mtx[obj][dp] = new float[ChannelCount][];
                    for (int ch = 0; ch < ChannelCount; ++ch)
                        joc_mix_mtx[obj][dp][ch] = new float[QuadratureMirrorFilterBank.subbands];
                }

                joc_mix_mtx_interp[obj] = new float[num_qmf_timeslots][][];
                for (int ts = 0; ts < num_qmf_timeslots; ++ts) {
                    joc_mix_mtx_interp[obj][ts] = new float[ChannelCount][];
                    for (int ch = 0; ch < ChannelCount; ++ch)
                        joc_mix_mtx_interp[obj][ts][ch] = new float[QuadratureMirrorFilterBank.subbands];
                }
            }

            for (int obj = 0; obj < ObjectCount; ++obj) {
                if (b_joc_sparse[obj])
                    DecodeSparse(obj);
                else
                    DecodeCoarse(obj);
            }
            DecodeSideInfo();
            InterpolateSideInfo(num_qmf_timeslots, prevMatrix);
            return joc_mix_mtx_interp;
        }

        float[][][][] joc_mix_mtx;
        float[][][][] joc_mix_mtx_interp;
    }
}
using System;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    partial class JointObjectCoding {
        void DecodeSparse(int obj) {
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
                                joc_channel_idx[obj][dp][pb]) % joc_num_channels;
                        for (int ch = 0; ch < joc_num_channels; ++ch) {
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
            if (b_joc_obj_present[obj]) {
                int nquant = joc_num_quant_idx[obj] ? 192 : 96;
                int offset = joc_num_quant_idx[obj] ? 96 : 48;
                for (int dp = 0; dp < joc_num_dpoints[obj]; ++dp) {
                    for (int ch = 0; ch < joc_num_channels; ++ch) {
                        joc_mix_mtx[obj][dp][ch][0] = (offset + joc_mtx[obj][dp][ch][1]) % nquant;
                        for (int pb = 1; pb < joc_num_bands[obj]; ++pb)
                            joc_mix_mtx[obj][dp][ch][pb] = (joc_mix_mtx[obj][dp][ch][pb - 1] +
                                joc_mtx[obj][dp][ch][pb]) % nquant;
                    }
                }
            }
        }

        void DecodeSideInfo() {
            for (int obj = 0; obj < joc_num_objects; ++obj) {
                int nquant = joc_num_quant_idx[obj] ? 192 : 96;
                for (int dp = 0; dp < joc_num_dpoints[obj]; ++dp)
                    for (int ch = 0; ch < joc_num_channels; ++ch)
                        for (int pb = 0; pb < joc_num_bands[obj]; ++pb)
                            joc_mix_mtx[obj][dp][ch][pb] = (joc_mix_mtx[obj][dp][ch][pb] - nquant / 2) *
                                820f / (4096 * (joc_num_quant_idx[obj] ? 2 : 1));
            }
        }

        void InterpolateSideInfo(int num_qmf_timeslots) {
            for (int obj = 0; obj < joc_num_objects; ++obj) {
                for (int ch = 0; ch < joc_num_channels; ++ch) {
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
                                        joc_mix_mtx_interp[obj][ts][ch][sb] = joc_mix_mtx_prev[obj][ch][sb];
                                    else
                                        joc_mix_mtx_interp[obj][ts][ch][sb] = joc_mix_mtx[obj][0][ch][sb];
                                } else {
                                    if (ts < joc_offset_ts[obj][0])
                                        joc_mix_mtx_interp[obj][ts][ch][sb] = joc_mix_mtx_prev[obj][ch][sb];
                                    else if (ts < joc_offset_ts[obj][1])
                                        joc_mix_mtx_interp[obj][ts][ch][sb] = joc_mix_mtx[obj][0][ch][sb];
                                    else
                                        joc_mix_mtx_interp[obj][ts][ch][sb] = joc_mix_mtx[obj][1][ch][sb];
                                }
                            } else {
                                if (joc_num_dpoints[obj] == 1) {
                                    float delta = joc_mix_mtx[obj][0][ch][pb] - joc_mix_mtx_prev[obj][ch][sb];
                                    joc_mix_mtx_interp[obj][ts][ch][sb] = joc_mix_mtx_prev[obj][ch][sb] +
                                        (ts + 1) * delta / num_qmf_timeslots;
                                } else {
                                    int ts_2 = num_qmf_timeslots / 2;
                                    if (ts < ts_2) {
                                        float delta = joc_mix_mtx[obj][0][ch][pb] - joc_mix_mtx_prev[obj][ch][sb];
                                        joc_mix_mtx_interp[obj][ts][ch][sb] = joc_mix_mtx_prev[obj][ch][sb] +
                                            (ts + 1) * delta / ts_2;
                                    } else {
                                        float delta = joc_mix_mtx[obj][1][ch][pb] - joc_mix_mtx[obj][0][ch][pb];
                                        joc_mix_mtx_interp[obj][ts][ch][sb] = joc_mix_mtx[obj][0][ch][pb] +
                                            (ts - ts_2 + 1) * delta / (num_qmf_timeslots - ts_2);
                                    }
                                }
                            }
                        }
                        joc_mix_mtx_prev[obj][ch][sb] = joc_mix_mtx[obj][joc_num_dpoints[obj] - 1][ch][pb];
                    }
                }
            }
        }

        /// <summary>
        /// Convert channel-based samples to object-based samples.
        /// </summary>
        /// <param name="input">Samples for each full bandwidth channel</param>
        public float[][] Decode(float[][] input) {

            int num_qmf_timeslots = input[0].Length / QuadratureMirrorFilterBank.subbands;
            joc_mix_mtx = new float[joc_num_objects][][][];
            joc_mix_mtx_interp = new float[joc_num_objects][][][];

            for (int obj = 0; obj < joc_num_objects; ++obj) {
                joc_mix_mtx[obj] = new float[joc_num_dpoints[obj]][][];
                for (int dp = 0; dp < joc_num_dpoints[obj]; ++dp) {
                    joc_mix_mtx[obj][dp] = new float[joc_num_channels][];
                    for (int ch = 0; ch < joc_num_channels; ++ch)
                        joc_mix_mtx[obj][dp][ch] = new float[QuadratureMirrorFilterBank.subbands];
                }

                joc_mix_mtx_interp[obj] = new float[num_qmf_timeslots][][];
                for (int ts = 0; ts < num_qmf_timeslots; ++ts) {
                    joc_mix_mtx_interp[obj][ts] = new float[joc_num_channels][];
                    for (int ch = 0; ch < joc_num_channels; ++ch)
                        joc_mix_mtx_interp[obj][ts][ch] = new float[QuadratureMirrorFilterBank.subbands];
                }
            }

            if (joc_mix_mtx_prev == null) {
                joc_mix_mtx_prev = new float[joc_num_objects][][];
                for (int obj = 0; obj < joc_num_objects; ++obj) {
                    joc_mix_mtx_prev[obj] = new float[joc_num_channels][];
                    for (int ch = 0; ch < joc_num_channels; ++ch)
                        joc_mix_mtx_prev[obj][ch] = new float[QuadratureMirrorFilterBank.subbands];
                }
            }

            for (int obj = 0; obj < joc_num_objects; ++obj) {
                if (b_joc_sparse[obj])
                    DecodeSparse(obj);
                else
                    DecodeCoarse(obj);
            }
            DecodeSideInfo();
            InterpolateSideInfo(num_qmf_timeslots);

            if (applier == null)
                applier = new JointObjectCodingApplier(joc_num_channels, joc_num_objects, input[0].Length);
            return applier.Apply(input, joc_mix_mtx_interp);
        }

        static JointObjectCodingApplier applier; // TODO: elsewhere, sync, buffer: always decode complete timeslots

        float[][][][] joc_mix_mtx;
        float[][][][] joc_mix_mtx_interp;
        static float[][][] joc_mix_mtx_prev; // TODO: sync between frames normally
    }
}
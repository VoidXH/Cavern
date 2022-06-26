namespace Cavern.Format.Decoders.EnhancedAC3 {
    partial class JointObjectCoding {
        void DecodeSparse(int obj) {
            if (ObjectActive[obj]) {
                int nquant = joc_num_quant_idx[obj] ? 192 : 96;
                int offset = joc_num_quant_idx[obj] ? 100 : 50;
                for (int dp = 0; dp < dataPoints[obj]; ++dp) {
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
            if (ObjectActive[obj]) {
                int nquant = joc_num_quant_idx[obj] ? 192 : 96;
                int offset = joc_num_quant_idx[obj] ? 96 : 48;
                for (int dp = 0; dp < dataPoints[obj]; ++dp) {
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
                int hnquant = joc_num_quant_idx[obj] ? 96 : 48;
                float mul = 820f / (joc_num_quant_idx[obj] ? 8192 : 4096);
                for (int dp = 0; dp < dataPoints[obj]; ++dp)
                    for (int ch = 0; ch < ChannelCount; ++ch)
                        for (int pb = 0; pb < joc_num_bands[obj]; ++pb)
                            joc_mix_mtx[obj][dp][ch][pb] = (joc_mix_mtx[obj][dp][ch][pb] - hnquant) * mul;
            }
        }

        void InterpolateSideInfo(int num_qmf_timeslots, float[][][] prevMatrix) {
            for (int obj = 0; obj < ObjectCount; ++obj) {
                if (!ObjectActive[obj]) {
                    for (int ts = 0; ts < num_qmf_timeslots; ++ts)
                        for (int ch = 0; ch < ChannelCount; ++ch)
                            for (byte sb = 0; sb < QuadratureMirrorFilterBank.subbands; ++sb)
                                joc_mix_mtx_interp[obj][ts][ch][sb] = 0;
                    continue;
                }
                for (int ch = 0; ch < ChannelCount; ++ch) {
                    for (byte sb = 0; sb < QuadratureMirrorFilterBank.subbands; ++sb) {
                        int pb = JointObjectCodingTables.parameterBandMapping[joc_num_bands_idx[obj]][sb];
                        for (int ts = 0; ts < num_qmf_timeslots; ++ts) {
                            if (joc_slope_idx[obj]) {
                                if (ts < joc_offset_ts[obj][0])
                                    joc_mix_mtx_interp[obj][ts][ch][sb] = prevMatrix[obj][ch][sb];
                                else if (dataPoints[obj] == 1)
                                    joc_mix_mtx_interp[obj][ts][ch][sb] = joc_mix_mtx[obj][0][ch][sb];
                                else
                                    joc_mix_mtx_interp[obj][ts][ch][sb] =
                                        joc_mix_mtx[obj][ts < joc_offset_ts[obj][1] ? 1 : 0][ch][sb];
                            } else {
                                if (dataPoints[obj] == 1) {
                                    float delta = joc_mix_mtx[obj][0][ch][pb] - prevMatrix[obj][ch][sb];
                                    joc_mix_mtx_interp[obj][ts][ch][sb] = prevMatrix[obj][ch][sb] +
                                        (ts + 1) * delta / num_qmf_timeslots;
                                } else {
                                    int ts_2 = num_qmf_timeslots >> 1;
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
                        prevMatrix[obj][ch][sb] = joc_mix_mtx[obj][dataPoints[obj] - 1][ch][pb];
                    }
                }
            }
        }

        /// <summary>
        /// Get the object mixing matrices.
        /// </summary>
        /// <param name="frameSize">Length of the entire time window of all time slots</param>
        public float[][][][] GetMixingMatrices(int frameSize) {
            for (int obj = 0; obj < ObjectCount; ++obj) {
                if (b_joc_sparse[obj])
                    DecodeSparse(obj);
                else
                    DecodeCoarse(obj);
            }
            DecodeSideInfo();
            int num_qmf_timeslots = frameSize / QuadratureMirrorFilterBank.subbands;
            InterpolateSideInfo(num_qmf_timeslots, prevMatrix);
            return joc_mix_mtx_interp;
        }
    }
}
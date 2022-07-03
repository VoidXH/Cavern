using System;
using System.Threading;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    partial class JointObjectCoding {
        void DecodeSparse(int obj, float[][][] mixMatrix, int nquant) {
            int offset = joc_num_quant_idx[obj] * 50 + 50;
            int[][] sourceMatrix = joc_vec[obj];
            int[][] inputChannel = joc_channel_idx[obj];
            int bands = joc_num_bands[obj];
            for (int dp = 0; dp < dataPoints[obj]; ++dp) {
                float[][] dpMatrix = mixMatrix[dp];
                int[] dpInput = inputChannel[dp];
                int joc_channel_idx_mod = dpInput[0];
                for (int ch = 0; ch < ChannelCount; ++ch) {
                    if (ch == joc_channel_idx_mod)
                        dpMatrix[ch][0] = (offset + sourceMatrix[dp][0]) % nquant;
                    else
                        dpMatrix[ch][0] = offset;
                }

                for (int pb = 1; pb < bands; ++pb) {
                    joc_channel_idx_mod = (dpInput[pb - 1] + dpInput[pb]) % ChannelCount;
                    for (int ch = 0; ch < ChannelCount; ++ch) {
                        if (ch == joc_channel_idx_mod)
                            dpMatrix[ch][pb] = (dpMatrix[ch][pb - 1] + sourceMatrix[dp][pb]) % nquant;
                        else
                            dpMatrix[ch][pb] = offset;
                    }
                }
            }
        }

        void DecodeCoarse(int obj, float[][][] mixMatrix, int hnquant) {
            int nquant = hnquant * 2;
            int bands = joc_num_bands[obj];
            int[][][] sourceMatrix = joc_mtx[obj];
            for (int dp = 0; dp < dataPoints[obj]; ++dp) {
                float[][] dpMatrix = mixMatrix[dp];
                int[][] dpSource = sourceMatrix[dp];
                for (int ch = 0; ch < ChannelCount; ++ch) {
                    float[] chMatrix = dpMatrix[ch];
                    int[] chSource = dpSource[ch];
                    chMatrix[0] = (hnquant + chSource[0]) % nquant;
                    for (int pb = 1; pb < bands; ++pb)
                        chMatrix[pb] = (chMatrix[pb - 1] + chSource[pb]) % nquant;
                }
            }
        }

        void DecodeObjectSideInfo(int obj, float[][][] mixMatrix, int hnquant, float mul) {
            for (int dp = 0; dp < dataPoints[obj]; ++dp) {
                float[][] dpMix = mixMatrix[dp];
                for (int ch = 0; ch < ChannelCount; ++ch) {
                    float[] chMix = dpMix[ch];
                    for (int pb = 0; pb < joc_num_bands[obj]; ++pb)
                        chMix[pb] = (chMix[pb] - hnquant) * mul;
                }
            }
        }

        void GetMixingMatrices(int obj, int timeslots, float[][][] mixMatrix,
            float[][][] interpolationMatrix, float[][] prevMatrix) {
            int hnquant = joc_num_quant_idx[obj] * 48 + 48;
            if (ObjectActive[obj]) {
                if (b_joc_sparse[obj])
                    DecodeSparse(obj, mixMatrix, hnquant * 2);
                else
                    DecodeCoarse(obj, mixMatrix, hnquant);
            }
            DecodeObjectSideInfo(obj, mixMatrix, hnquant, .2f - joc_num_quant_idx[obj] * .1f);

            // Side info interpolation below
            if (!ObjectActive[obj]) {
                for (int ts = 0; ts < timeslots; ++ts)
                    for (int ch = 0; ch < ChannelCount; ++ch)
                        Array.Clear(interpolationMatrix[ts][ch], 0, QuadratureMirrorFilterBank.subbands);
                return;
            }

            if (dataPoints[obj] == 1) {
                if (steepSlope[obj]) {
                    for (int ts = 0; ts < timeslots; ++ts) {
                        float[][] timeslotInterp = interpolationMatrix[ts];
                        float[][] source =
                            ts < joc_offset_ts[obj][0] ? prevMatrix : mixMatrix[ts < joc_offset_ts[obj][1] ? 1 : 0];
                        for (int ch = 0; ch < ChannelCount; ++ch) {
                            float[] channelInterp = timeslotInterp[ch];
                            float[] sourceChannel = source[ch];
                            for (byte sb = 0; sb < QuadratureMirrorFilterBank.subbands; ++sb)
                                channelInterp[sb] = sourceChannel[sb];
                        }
                    }
                } else {
                    for (int ts = 0; ts < timeslots;) {
                        float[][] timeslotInterp = interpolationMatrix[ts];
                        float lerp = ++ts / timeslots;
                        for (int ch = 0; ch < ChannelCount; ++ch) {
                            float[] channelInterp = timeslotInterp[ch];
                            float[] channelPrev = prevMatrix[ch];
                            float[] mix = mixMatrix[0][ch];
                            for (byte sb = 0; sb < QuadratureMirrorFilterBank.subbands; ++sb) {
                                int pb = JointObjectCodingTables.parameterBandMapping[joc_num_bands_idx[obj]][sb];
                                channelInterp[sb] = channelPrev[sb] + (mix[pb] - channelPrev[sb]) * lerp;
                            }
                        }
                    }
                }
            } else {
                if (steepSlope[obj]) {
                    for (int ts = 0; ts < timeslots;) {
                        float[][] timeslotInterp = interpolationMatrix[ts++];
                        float[][] source = ts < joc_offset_ts[obj][0] ? prevMatrix : mixMatrix[0];
                        for (int ch = 0; ch < ChannelCount; ++ch) {
                            float[] channelInterp = timeslotInterp[ch];
                            float[] sourceChannel = source[ch];
                            for (byte sb = 0; sb < QuadratureMirrorFilterBank.subbands; ++sb)
                                channelInterp[sb] = sourceChannel[sb];
                        }
                    }
                } else {
                    int ts_2 = timeslots >> 1;
                    for (int ts = 0; ts < timeslots;) {
                        float[][] timeslotInterp = interpolationMatrix[ts++];
                        float lerp;
                        float[][] from, to;
                        if (ts <= ts_2) {
                            lerp = ts / ts_2;
                            from = prevMatrix;
                            to = mixMatrix[0];
                        } else {
                            lerp = (ts - ts_2) / (timeslots - ts_2);
                            from = mixMatrix[0];
                            to = mixMatrix[1];
                        }

                        for (int ch = 0; ch < ChannelCount; ++ch) {
                            float[] channelInterp = timeslotInterp[ch];
                            float[] channelFrom = from[ch];
                            float[] channelTo = to[ch];
                            if (ts <= ts_2) {
                                for (byte sb = 0; sb < QuadratureMirrorFilterBank.subbands; ++sb)
                                    channelInterp[sb] = channelFrom[sb] + (channelTo[sb] - channelFrom[sb]) * lerp;
                            } else {
                                for (byte sb = 0; sb < QuadratureMirrorFilterBank.subbands; ++sb) {
                                    int pb = JointObjectCodingTables.parameterBandMapping[joc_num_bands_idx[obj]][sb];
                                    channelInterp[sb] = channelFrom[pb] + (channelTo[pb] - channelFrom[pb]) * lerp;
                                }
                            }
                        }
                    }
                }
            }

            for (int ts = 0; ts < timeslots; ++ts) {
                for (int ch = 0; ch < ChannelCount; ++ch) {
                    float[] channelPrev = prevMatrix[ch];
                    float[] mixSource = mixMatrix[dataPoints[obj] - 1][ch];
                    for (byte sb = 0; sb < QuadratureMirrorFilterBank.subbands; ++sb) {
                        int pb = JointObjectCodingTables.parameterBandMapping[joc_num_bands_idx[obj]][sb];
                        channelPrev[sb] = mixSource[pb];
                    }
                }
            }
        }

        void GetMixingMatrices(int num_qmf_timeslots, float[][][] prevMatrix) {
            int runs = ObjectCount;
            using ManualResetEvent reset = new ManualResetEvent(false);
            for (int obj = 0; obj < ObjectCount; ++obj) {
                ThreadPool.QueueUserWorkItem(
                   new WaitCallback(objIndex => {
                       int obj = (int)objIndex;
                       GetMixingMatrices(obj, num_qmf_timeslots, joc_mix_mtx[obj],
                           joc_mix_mtx_interp[obj], prevMatrix[obj]);
                       if (Interlocked.Decrement(ref runs) == 0)
                           reset.Set();
                   }), obj);
            }
            reset.WaitOne();
        }

        /// <summary>
        /// Get the object mixing matrices.
        /// </summary>
        /// <param name="frameSize">Length of the entire time window of all time slots</param>
        public float[][][][] GetMixingMatrices(int frameSize) {
            int num_qmf_timeslots = frameSize / QuadratureMirrorFilterBank.subbands;
            GetMixingMatrices(num_qmf_timeslots, prevMatrix);
            return joc_mix_mtx_interp;
        }
    }
}
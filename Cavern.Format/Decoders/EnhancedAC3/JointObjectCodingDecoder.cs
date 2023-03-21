using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Cavern.Format.Decoders.EnhancedAC3 {
    partial class JointObjectCoding {
        /// <summary>
        /// The JOC contained vector-based and not matrix-based encoded data at some point.
        /// </summary>
        public bool HadSparse { get; private set; }

        /// <summary>
        /// Decode a JOC matrix around a <paramref name="center"/> value in a coding where each object only takes one
        /// channel's data. In this step, the values are positive integers.
        /// </summary>
        void DecodeSparse(int obj, float[][][] mixMatrix, int center) {
            HadSparse = true;
            int max = center * 2;
            int[][] sourceVector = jocVector[obj];
            int[][] inputChannel = jocChannel[obj];
            int bands = this.bands[obj];
            for (int dp = 0; dp < dataPoints[obj]; dp++) {
                float[][] dpMatrix = mixMatrix[dp];
                int[] dpVector = sourceVector[dp];
                Array.Clear(dpVector, 0, dpVector.Length); // TODO: unmute sparse when the standard is fixed
                int[] dpChannel = inputChannel[dp];

                for (int pb = 0; pb < bands; pb++) {
                    int channel;
                    if (pb == 0) {
                        channel = dpChannel[0];
                    } else {
                        channel = (dpChannel[pb - 1] + dpChannel[pb]) % ChannelCount;
                    }

                    for (int ch = 0; ch < ChannelCount; ch++) {
                        if (ch == channel) {
                            if (pb == 0) {
                                dpMatrix[ch][pb] = (center + dpVector[pb]) % max;
                            } else {
                                dpMatrix[ch][pb] = (dpMatrix[ch][pb - 1] + dpVector[pb]) % max;
                            }
                        } else {
                            dpMatrix[ch][pb] = center;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Decode and dequantize a complete JOC matrix around a <paramref name="quantizedCenter"/> value.
        /// </summary>
        unsafe void DecodeCoarse(int obj, float[][][] mixMatrix, int quantizedCenter, float gainStep) {
            float center = quantizedCenter * gainStep;
            float max = center * 2;
            int bands = this.bands[obj];
            int[][][] sourceMatrix = jocMatrix[obj];
            for (int dp = 0; dp < dataPoints[obj]; dp++) {
                int[][] dpSource = sourceMatrix[dp];
                float[][] dpMatrix = mixMatrix[dp];
                for (int ch = 0; ch < ChannelCount; ch++) {
                    int[] chSource = dpSource[ch];
                    float[] chMatrix = dpMatrix[ch];
                    fixed (int* source = chSource) {
                        fixed (float* destination = chMatrix) {
                            DecodeCoarseChannel(source, destination, center, gainStep, max, bands);
                        }
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe void DecodeCoarseChannel(int* source, float* destination, float center, float gainStep, float max, int bands) {
            *destination = (center + *source * gainStep) % max;
            while (--bands != 0) {
                float next = (*destination + *++source * gainStep) % max;
                *(destination++) -= center;
                *destination = next;
            }
            *destination -= center;
        }

        /// <summary>
        /// Convert the values of the decoded JOC matrix to the mixing range.
        /// </summary>
        unsafe void Dequantize(int obj, float[][][] mixMatrix, int center, float gainStep) {
            for (int dp = 0; dp < dataPoints[obj]; dp++) {
                float[][] dpMix = mixMatrix[dp];
                for (int ch = 0; ch < ChannelCount; ch++) {
                    fixed (float* chMix = dpMix[ch]) {
                        DequantizeObject(chMix, center, gainStep, bands[obj]);
                    }
                }
            }
        }

        /// <summary>
        /// Convert the values of the decoded object to the mixing range.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe static void DequantizeObject(float* channel, int center, float gainStep, int bands) {
            while (bands != 0) {
                *(channel++) = (*channel - center) * gainStep;
                --bands;
            }
        }

        /// <summary>
        /// Handle mixing of a timeslot with steep slope when the number of data points is 1
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SteepSingleDataPointTimeslot(int ts, float[][][] interpolationMatrix, float[][] source) {
            float[][] timeslotInterp = interpolationMatrix[ts];
            for (int ch = 0; ch < ChannelCount; ch++) {
                Array.Copy(source[ch], timeslotInterp[ch], QuadratureMirrorFilterBank.subbands);
            }
        }

        void GetMixingMatrices(int obj, int timeslots, float[][][] mixMatrix,
            float[][][] interpolationMatrix, float[][] prevMatrix) {
            int centerValue = quantizationTable[obj] * 48 + 48;
            if (ObjectActive[obj]) {
                float gainStep = .2f - quantizationTable[obj] * .1f;
                if (sparseCoded[obj]) {
                    DecodeSparse(obj, mixMatrix, centerValue);
                    Dequantize(obj, mixMatrix, centerValue, gainStep);
                } else {
                    DecodeCoarse(obj, mixMatrix, centerValue, gainStep);
                }
            } else {
                // The final result is in the interpolation matrix.
                // For the previously calculated mixing matrix, it will be calculated later.
                for (int ts = 0; ts < timeslots; ts++) {
                    for (int ch = 0; ch < ChannelCount; ch++) {
                        Array.Clear(interpolationMatrix[ts][ch], 0, QuadratureMirrorFilterBank.subbands);
                    }
                }
                return;
            }

            byte[] pbMapping = JointObjectCodingTables.parameterBandMapping[bandsIndex[obj]];
            if (dataPoints[obj] == 1) {
                if (steepSlope[obj]) {
                    int splitPoint = timeslotOffsets[obj][0];
                    for (int ts = 0; ts < splitPoint; ts++) {
                        SteepSingleDataPointTimeslot(ts, interpolationMatrix, prevMatrix);
                    }
                    for (int ts = splitPoint; ts < timeslots; ts++) {
                        SteepSingleDataPointTimeslot(ts, interpolationMatrix, mixMatrix[ts < timeslotOffsets[obj][1] ? 1 : 0]);
                    }
                } else {
                    for (int ch = 0; ch < ChannelCount; ch++) {
                        float[] channelPrev = prevMatrix[ch];
                        float[] mix = mixMatrix[0][ch];
                        for (int ts = 0; ts < timeslots;) {
                            float[] channelInterp = interpolationMatrix[ts][ch];
                            float lerp = (float)++ts / timeslots;
                            for (int sb = 0; sb < QuadratureMirrorFilterBank.subbands; sb++) {
                                channelInterp[sb] = channelPrev[sb] + (mix[pbMapping[sb]] - channelPrev[sb]) * lerp;
                            }
                        }
                    }
                }
            } else {
                if (steepSlope[obj]) {
                    for (int ts = 0; ts < timeslots;) {
                        float[][] timeslotInterp = interpolationMatrix[ts++];
                        float[][] source = ts < timeslotOffsets[obj][0] ? prevMatrix : mixMatrix[0];
                        for (int ch = 0; ch < ChannelCount; ch++) {
                            float[] channelInterp = timeslotInterp[ch];
                            float[] sourceChannel = source[ch];
                            for (byte sb = 0; sb < QuadratureMirrorFilterBank.subbands; sb++) {
                                channelInterp[sb] = sourceChannel[sb];
                            }
                        }
                    }
                } else {
                    int ts_2 = timeslots >> 1;
                    for (int ts = 0; ts < timeslots;) {
                        float[][] timeslotInterp = interpolationMatrix[ts++];
                        float lerp;
                        float[][] from, to;
                        if (ts <= ts_2) {
                            lerp = (float)ts / ts_2;
                            from = prevMatrix;
                            to = mixMatrix[0];
                        } else {
                            lerp = (float)(ts - ts_2) / (timeslots - ts_2);
                            from = mixMatrix[0];
                            to = mixMatrix[1];
                        }

                        for (int ch = 0; ch < ChannelCount; ch++) {
                            float[] channelInterp = timeslotInterp[ch];
                            float[] channelFrom = from[ch];
                            float[] channelTo = to[ch];
                            if (ts <= ts_2) {
                                for (byte sb = 0; sb < QuadratureMirrorFilterBank.subbands; sb++) {
                                    channelInterp[sb] = channelFrom[sb] + (channelTo[sb] - channelFrom[sb]) * lerp;
                                }
                            } else {
                                for (byte sb = 0; sb < QuadratureMirrorFilterBank.subbands; sb++) {
                                    int pb = JointObjectCodingTables.parameterBandMapping[bandsIndex[obj]][sb];
                                    channelInterp[sb] = channelFrom[pb] + (channelTo[pb] - channelFrom[pb]) * lerp;
                                }
                            }
                        }
                    }
                }
            }

            for (int ts = 0; ts < timeslots; ts++) {
                for (int ch = 0; ch < ChannelCount; ch++) {
                    float[] channelPrev = prevMatrix[ch];
                    float[] mixSource = mixMatrix[dataPoints[obj] - 1][ch];
                    for (byte sb = 0; sb < QuadratureMirrorFilterBank.subbands; sb++) {
                        channelPrev[sb] = mixSource[pbMapping[sb]];
                    }
                }
            }
        }

        /// <summary>
        /// Get the object mixing matrices.
        /// </summary>
        /// <param name="frameSize">Length of the entire time window of all time slots</param>
        public float[][][][] GetMixingMatrices(int frameSize) {
            int qmfTimeslots = frameSize / QuadratureMirrorFilterBank.subbands;
            int runs = ObjectCount;
            taskWaiter.Reset();
            for (int obj = 0; obj < ObjectCount; obj++) {
                ThreadPool.QueueUserWorkItem(
                   new WaitCallback(objIndex => {
                       int obj = (int)objIndex;
                       GetMixingMatrices(obj, qmfTimeslots, mixMatrix[obj],
                           interpolatedMatrix[obj], prevMatrix[obj]);
                       if (Interlocked.Decrement(ref runs) == 0) {
                           taskWaiter.Set();
                       }
                   }), obj);
            }
            taskWaiter.Wait();
            return interpolatedMatrix;
        }
    }
}
using Cavern.Utilities;
using Cavern.Waveforms;

namespace Cavern.QuickEQ.Measurement {
    /// <summary>
    /// Functions for analyzing phase curves.
    /// </summary>
    public class PhaseAnalyzer {
        /// <summary>
        /// Get the excess phase curve from an <paramref name="impulseResponse"/>.
        /// </summary>
        public static float[] GetExcessPhase(float[] impulseResponse, DelayDeterminationType delayType) {
            using FFTCache cache = new FFTCache(impulseResponse.Length);
            return GetExcessPhase(impulseResponse, delayType, cache);
        }

        /// <summary>
        /// Get the excess phase curve from an <paramref name="impulseResponse"/>.
        /// </summary>
        public static float[] GetExcessPhase(float[] impulseResponse, DelayDeterminationType delayType, FFTCache cache) =>
            GetExcessPhase(impulseResponse.FFT(cache), delayType, cache);

        /// <summary>
        /// Get the excess phase curve from a <paramref name="transferFunction"/>.
        /// </summary>
        public static float[] GetExcessPhase(Complex[] transferFunction, DelayDeterminationType delayType) {
            using FFTCache cache = new FFTCache(transferFunction.Length);
            return GetExcessPhase(transferFunction, delayType, cache);
        }

        /// <summary>
        /// Get the excess phase curve from a <paramref name="transferFunction"/>.
        /// </summary>
        public static float[] GetExcessPhase(Complex[] transferFunction, DelayDeterminationType delayType, FFTCache cache) {
            Complex[] minimumPhaseTF = transferFunction.GetZeroPhase();
            minimumPhaseTF.Threshold(1e-10f);
            Measurements.ConvertToMinimumPhase(minimumPhaseTF, cache);
            float[] minimumPhase = Measurements.GetPhase(minimumPhaseTF);
            Measurements.UnwrapPhase(minimumPhase);

            float[] actualPhase = PhaseDelayCompensation.GetUndelayedPhase(transferFunction, delayType, true, 0);
            minimumPhase.SubtractFrom(actualPhase);
            return actualPhase;
        }


        /// <summary>
        /// Get the excess phase curve from a set of <paramref name="signals"/>.
        /// </summary>
        public static MultichannelWaveform GetExcessPhase(MultichannelWaveform signals, DelayDeterminationType delayType) {
            float[][] result = new float[signals.Channels][];
            FFTCachePool.ForAllUnchecked(signals, (signals, index, cache) => result[index] = GetExcessPhase(signals, delayType, cache), true);
            return new MultichannelWaveform(result);
        }

        /// <summary>
        /// Get the excess phase curve from a set of <paramref name="transferFunctions"/>.
        /// </summary>
        public static MultichannelWaveform GetExcessPhase(MultichannelTransferFunction transferFunctions, DelayDeterminationType delayType) {
            float[][] result = new float[transferFunctions.Channels][];
            FFTCachePool.ForAllUnchecked(transferFunctions, (transferFunction, index, cache) => result[index] = GetExcessPhase(transferFunction, delayType, cache), true);
            return new MultichannelWaveform(result);
        }
    }
}

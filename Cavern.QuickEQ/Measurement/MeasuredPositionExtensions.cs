using System.Collections.Generic;
using System.Linq;

using Cavern.Utilities;
using Cavern.Utilities.Threading;
using Cavern.Waveforms;

namespace Cavern.QuickEQ.Measurement {
    /// <summary>
    /// Operations on a list of <see cref="MeasuredPosition"/>s.
    /// </summary>
    public static class MeasuredPositionExtensions {
        /// <summary>
        /// Get the measured signals with the following dimensions: [measurement no.][channel][sample].
        /// </summary>
        /// <param name="measurements">The recorded measurements</param>
        /// <param name="reference">Index of the reference measurement in <paramref name="measurements"/></param>
        /// <param name="measurementSignal">The measurement signal used for recording</param>
        /// <param name="multithreading">Whether to use multiple threads</param>
        /// <exception cref="DifferentSignalLengthsException">The measurement signal's length doesn't equal the length of recorded measurements.</exception>
        public static MultichannelWaveform[] GetExcitements(this IReadOnlyList<MeasuredPosition> measurements, int reference, float[] measurementSignal,
            bool multithreading) {
            FFTCachePool pool = new FFTCachePool(measurementSignal.Length);
            FFTCache cache = pool.Lease();
            Complex[] referenceFFT = Measurements.FFT(measurementSignal, cache);
            pool.Return(cache);
            for (int i = 0; i < measurements.Count; i++) {
                if (measurements[i].ImpulseResponses[0].ComplexResponse.Length != referenceFFT.Length) {
                    throw new DifferentSignalLengthsException();
                }
            }

            MultichannelWaveform[] result = new MultichannelWaveform[measurements.Count];
            for (int i = 0; i < measurements.Count; i++) {
                int sourceIndex = i == 0 ? reference : (i - 1 < reference ? i - 1 : i); // Reference first, then the rest
                Complex[][] source = measurements[sourceIndex].ImpulseResponses.Select(x => x.ComplexResponse).ToArray();
                float[][] target = new float[source.Length][];
                Parallelizer.For(0, source.Length, j => {
                    FFTCache channelCache = pool.Lease();
                    Complex[] working = source[j].FastClone();
                    working.ConvolveInPlace(referenceFFT, channelCache);
                    pool.Return(channelCache);
                    target[j] = Measurements.GetRealPart(working);
                }, multithreading);
                result[i] = new MultichannelWaveform(target);
            }
            return result;
        }
    }
}

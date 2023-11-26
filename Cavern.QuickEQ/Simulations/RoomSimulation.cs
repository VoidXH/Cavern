using System;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.QuickEQ.Simulations {
    /// <summary>
    /// Creates room simulations by combining impulse responses with filters.
    /// </summary>
    public static class RoomSimulation {
        /// <summary>
        /// Creates a room simulation by convolving impulse responses with filters.
        /// </summary>
        /// <remarks>The resulting impulse responses will have the length of the measurements.</remarks>
        public static MultichannelWaveform Simulate(MultichannelWaveform measurement, MultichannelWaveform filters) {
            float[][] results = new float[measurement.Channels][];
            using FFTCache cache = new FFTCache(measurement[0].Length);
            for (int i = 0; i < results.Length; i++) {
                float[] impulse = measurement[i];
                float[] sizeMatchedFilter = filters[i];
                Array.Resize(ref sizeMatchedFilter, impulse.Length);
                results[i] = FastConvolver.Convolve(impulse, sizeMatchedFilter, cache);
            }
            return new MultichannelWaveform(results);
        }
    }
}
using System;
using System.Runtime.CompilerServices;

using Cavern.Utilities;

namespace Cavern.Filters {
    partial class BiquadFilter {
        /// <summary>
        /// Get the transfer function quickly, without FFT.
        /// </summary>
        /// <param name="bins">Number of frequency bins to evaluate (linear spacing from 0 to Nyquist).</param>
        /// <returns>Complex transfer function values, one per bin.</returns>
        public Complex[] GetTransferFunction(int bins) {
            if (bins <= 0) {
                throw new ArgumentOutOfRangeException(nameof(bins), "Must be positive.");
            }

            if (CavernAmp.Available && nativeInstance != IntPtr.Zero) {
                return GetTransferFunctionNative(bins);
            }

            Complex[] response = new Complex[bins];
            for (int i = 0; i < bins; i++) {
                // z = e^(jω), so z⁻¹ = e^(-jω)
                Complex zInverse = Complex.UnitPhase(-(float)(2 * Math.PI * i / bins));
                Complex zInverseSq = zInverse * zInverse;
                Complex numerator = new Complex(b0) + zInverse * b1 + zInverseSq * b2;
                Complex denominator = new Complex(1) + zInverse * a1 + zInverseSq * a2;
                response[i] = numerator / denominator;
            }
            return response;
        }

        /// <summary>
        /// Get the transfer function using CavernAmp native implementation.
        /// </summary>
        /// <param name="bins">Number of frequency bins to evaluate (linear spacing from 0 to Nyquist).</param>
        /// <returns>Complex transfer function values, one per bin.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe Complex[] GetTransferFunctionNative(int bins) {
            Complex[] response = new Complex[bins];
            fixed (Complex* pResponse = response) {
                CavernAmp.BiquadFilter_GetTransferFunction(nativeInstance, bins, pResponse);
            }
            return response;
        }

        /// <summary>
        /// Get the frequency response quickly, without FFT.
        /// </summary>
        /// <param name="bins">Number of frequency bins to evaluate (linear spacing from 0 to Nyquist).</param>
        /// <returns>Frequency response values, one per bin.</returns>
        public float[] GetFrequencyResponse(int bins) {
            if (bins <= 0) {
                throw new ArgumentOutOfRangeException(nameof(bins), "Must be positive.");
            }

            if (CavernAmp.Available && nativeInstance != IntPtr.Zero) {
                return GetFrequencyResponseNative(bins);
            }

            float[] response = new float[bins];
            for (int i = 0; i < bins; i++) {
                float omega = 2 * MathF.PI * i / bins;
                float cos = MathF.Cos(omega);
                float sin = -MathF.Sin(omega); // z⁻¹ = e^(-jω)
                // Numerator: b0 + b1·z⁻¹ + b2·z⁻²
                float numReal = b0 + b1 * cos + b2 * (cos * cos - sin * sin);
                float numImag = b1 * sin + b2 * (2 * cos * sin);
                // Denominator: 1 + a1·z⁻¹ + a2·z⁻²
                float denReal = 1 + a1 * cos + a2 * (cos * cos - sin * sin);
                float denImag = a1 * sin + a2 * (2 * cos * sin);
                // |H| = sqrt(numReal² + numImag²) / sqrt(denReal² + denImag²)
                response[i] = MathF.Sqrt(numReal * numReal + numImag * numImag) / MathF.Sqrt(denReal * denReal + denImag * denImag);
            }
            return response;
        }

        /// <summary>
        /// Get the frequency response using CavernAmp native implementation.
        /// </summary>
        /// <param name="bins">Number of frequency bins to evaluate (linear spacing from 0 to Nyquist).</param>
        /// <returns>Frequency response values, one per bin.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        unsafe float[] GetFrequencyResponseNative(int bins) {
            float[] response = new float[bins];
            fixed (float* pResponse = response) {
                CavernAmp.BiquadFilter_GetFrequencyResponse(nativeInstance, bins, pResponse);
            }
            return response;
        }
    }
}

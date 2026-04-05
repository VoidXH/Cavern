using System.Runtime.CompilerServices;

namespace Cavern.QuickEQ.Equalization {
    partial class Equalizer {
        /// <summary>
        /// Clamp the gains of the EQ between the desired <paramref name="bottom"/> and <paramref name="peak"/>.
        /// </summary>
        public void Clamp(double bottom, double peak) {
            LimitDips(bottom);
            LimitPeaks(peak);
        }

        /// <summary>
        /// Make sure the EQ won't go under the desired <paramref name="bottom"/>.
        /// </summary>
        public void LimitDips(double bottom) => LimitDips(0, bands.Count - 1, bottom);

        /// <summary>
        /// Make sure the EQ won't go over the desired <paramref name="peak"/> between the frequency limits.
        /// </summary>
        public void LimitDips(double bottom, double startFreq, double endFreq) {
            (int startBand, int endBand) = GetBandLimits(startFreq, endFreq);
            if (startBand == -1 && endBand == -1) {
                return; // The frequency range is not on the curve
            }
            LimitDips(startBand, endBand, bottom);
        }

        /// <summary>
        /// Make sure the EQ won't go over the desired <paramref name="peak"/>.
        /// </summary>
        public void LimitPeaks(double peak) => LimitPeaks(0, bands.Count - 1, peak);

        /// <summary>
        /// Make sure the EQ won't go over the desired <paramref name="peak"/> between the frequency limits.
        /// </summary>
        public void LimitPeaks(double peak, double startFreq, double endFreq) {
            (int startBand, int endBand) = GetBandLimits(startFreq, endFreq);
            if (startBand == -1 && endBand == -1) {
                return; // The frequency range is not on the curve
            }
            LimitPeaks(startBand, endBand, peak);
        }

        /// <summary>
        /// Make sure the EQ won't go under the desired <paramref name="bottom"/> between the band limits.
        /// </summary>
        /// <param name="startBand">First band to limit (inclusive)</param>
        /// <param name="endBand">Last band to limit (inclusive)</param>
        /// <param name="peak">Maximum allowed value of the range set by <paramref name="startBand"/> and <paramref name="endBand"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void LimitDips(int startBand, int endBand, double bottom) {
            while (startBand <= endBand) {
                if (bands[startBand].Gain < bottom) {
                    bands[startBand] = new Band(bands[startBand].Frequency, bottom);
                }
                startBand++;
            }
            if (PeakGain < bottom) {
                PeakGain = bottom;
            }
        }

        /// <summary>
        /// Make sure the EQ won't go over the desired <paramref name="peak"/> between the band limits.
        /// </summary>
        /// <param name="startBand">First band to limit (inclusive)</param>
        /// <param name="endBand">Last band to limit (inclusive)</param>
        /// <param name="peak">Maximum allowed value of the range set by <paramref name="startBand"/> and <paramref name="endBand"/></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void LimitPeaks(int startBand, int endBand, double peak) {
            while (startBand <= endBand) {
                if (bands[startBand].Gain > peak) {
                    bands[startBand] = new Band(bands[startBand].Frequency, peak);
                }
                startBand++;
            }
            if (PeakGain > peak) {
                PeakGain = peak;
            }
        }
    }
}

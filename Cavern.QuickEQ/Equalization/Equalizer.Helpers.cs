namespace Cavern.QuickEQ.Equalization {
    // Helper functions for handling Equalizers more easily
    partial class Equalizer {
        /// <summary>
        /// Get the band index range corresponding to the selected frequency limits (both inclusive).
        /// </summary>
        internal (int startBand, int endBand) GetBandLimits(double startFreq, double endFreq) {
            int first = GetFirstBand(startFreq);
            if (first == -1) {
                return (-1, -1);
            }
            int last = GetFirstBand(endFreq);
            if (last == -1) {
                last = bands.Count - 1;
            }
            return (first, last);
        }

        /// <summary>
        /// Convert a frequency range to band indices, or return the whole range if the frequencies are not set (NaN).
        /// </summary>
        /// <param name="freqStart">First frequency to measure</param>
        /// <param name="freqEnd">Last frequency to measure</param>
        (int first, int last) GetMeasurementLimits(double freqStart, double freqEnd) {
            int first = -1, last = -1;
            if (double.IsNaN(freqStart)) {
                (first, last) = GetBandLimits(freqStart, freqEnd);
            }
            if (first == -1) {
                (first, last) = (0, bands.Count - 1);
            }
            return (first, last);
        }

        /// <summary>
        /// Get which band index is the first after a given <paramref name="freq"/>uency. Returns -1 if such a band was not found.
        /// </summary>
        int GetFirstBand(double freq) {
            for (int i = 0, c = bands.Count; i < c; i++) {
                if (bands[i].Frequency >= freq) {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Get which band index is the first after a given <paramref name="freq"/>uency, or 0 if such a band was not found.
        /// </summary>
        int GetFirstBandSafe(double freq) {
            int result = GetFirstBand(freq);
            return result != -1 ? result : 0;
        }
    }
}

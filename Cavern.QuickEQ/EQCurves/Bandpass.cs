using System;

using Cavern.Filters;
using Cavern.Filters.Utilities;
using Cavern.QuickEQ.SignalGeneration;
using Cavern.QuickEQ.Utilities;
using Cavern.Utilities;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>
    /// Bandpass EQ curve, recommended for stage subwoofers.
    /// </summary>
    public class Bandpass : EQCurve {
        /// <summary>
        /// Multiplier for each frequency that gives the position of the needed gain in <see cref="spectrum"/>.
        /// </summary>
        double positioner;

        /// <summary>
        /// Precalculated EQ spectrum.
        /// </summary>
        float[] spectrum;

        /// <summary>
        /// Bandpass gain loss compensation.
        /// </summary>
        double gain;

        /// <summary>
        /// Bandpass EQ curve, with the default settings recommended for stage subwoofers: maximum flatness, first order, 6 dB gain.
        /// </summary>
        /// <param name="lowFreq">Low frequency (highpass) cutoff knee</param>
        /// <param name="highFreq">High frequency (lowpass) cutoff knee</param>
        /// <param name="sampleRate">Sample rate of the system to be EQ'd</param>
        /// <param name="resolution">Sample resolution for <see cref="this[double]"/>, must be a power of 2</param>
        public Bandpass(double lowFreq, double highFreq, int sampleRate, int resolution) :
            this(lowFreq, highFreq, sampleRate, resolution, QFactor.reference, 1, 6) { }

        /// <summary>
        /// Bandpass EQ curve with custom Q-factor, order, and gain.
        /// </summary>
        /// <param name="lowFreq">Low frequency (highpass) cutoff knee</param>
        /// <param name="highFreq">High frequency (lowpass) cutoff knee</param>
        /// <param name="sampleRate">Sample rate of the system to be EQ'd</param>
        /// <param name="resolution">Sample resolution for <see cref="this[double]"/>, must be a power of 2</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="order">Each order increases the slope with 12 dB/octave</param>
        /// <param name="gain">Filter gain</param>
        public Bandpass(double lowFreq, double highFreq, int sampleRate, int resolution, double q, int order, double gain)
            => SetFilter(lowFreq, highFreq, sampleRate, resolution, q, order, gain);

        /// <summary>
        /// Set the filter parameters from a BandpassFlat filter.
        /// </summary>
        void SetFilter(double lowFreq, double highFreq, int sampleRate, int resolution, double q, int order, double gain) {
            positioner = resolution * 2.0 / sampleRate;
            float[] reference = SweepGenerator.Exponential(20, sampleRate * .5f, resolution * 2, sampleRate),
                response = reference.FastClone();
            BandpassFlat filter = new BandpassFlat(lowFreq, highFreq, sampleRate, q, order);
            filter.Process(response);
            spectrum = Measurements.GetSpectrum(Measurements.GetFrequencyResponse(reference, response));
            GraphUtils.ConvertToDecibels(spectrum);
            this.gain = gain;
        }

        /// <summary>
        /// Get the curve's gain in decibels at a given frequency.
        /// </summary>
        public override double this[double frequency] => spectrum[(int)(frequency * positioner)] + gain;

        /// <inheritdoc/>
        public override void FromBase64(string source) {
            string data = FromBase64String(source);
            string[] parts = data.Split('|');
            if (parts.Length != 7 || parts[0] != nameof(Bandpass)) {
                throw new FormatException("Invalid Bandpass data.");
            }
            BandpassFlat filter = new BandpassFlat(
                double.Parse(parts[1]), double.Parse(parts[2]), int.Parse(parts[3]),
                double.Parse(parts[5]), int.Parse(parts[6]));
            SetFilter(filter.LowFreq, filter.HighFreq, filter.SampleRate, 1024, filter.Q, filter.Order, double.Parse(parts[4]));
        }

        /// <inheritdoc/>
        public override string ToBase64() {
            BandpassFlat filter = new BandpassFlat(20, 20000, 48000, QFactor.reference, 1);
            return ToBase64String($"{nameof(Bandpass)}|{filter.LowFreq}|{filter.HighFreq}|{filter.SampleRate}|{gain}|{filter.Q}|{filter.Order}");
        }
    }
}

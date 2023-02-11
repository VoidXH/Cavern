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
        readonly double positioner;

        /// <summary>
        /// Precalculated EQ spectrum.
        /// </summary>
        readonly float[] spectrum;

        /// <summary>
        /// Bandpass gain loss compensation.
        /// </summary>
        readonly double gain;

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
        /// <param name="order">Each order increases the slope with 6 dB/octave</param>
        /// <param name="gain">Filter gain</param>
        public Bandpass(double lowFreq, double highFreq, int sampleRate, int resolution, double q, int order, double gain) {
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
    }
}
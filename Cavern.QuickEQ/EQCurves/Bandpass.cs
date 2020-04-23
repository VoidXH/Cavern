using Cavern.Filters;
using Cavern.Filters.Utilities;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>Bandpass EQ curve, recommended for stage subwoofers.</summary>
    public class Bandpass : EQCurve {
        /// <summary>Multiplier for each frequency that gives the position of the needed gain in <see cref="spectrum"/>.</summary>
        readonly float positioner;
        /// <summary>Precalculated EQ spectrum.</summary>
        readonly float[] spectrum;
        /// <summary>Bandpass gain loss compensation.</summary>
        readonly float gain;

        /// <summary>Bandpass EQ curve, recommended for stage subwoofers.</summary>
        /// <param name="lowFreq">Low frequency (highpass) cutoff knee</param>
        /// <param name="highFreq">High frequency (lowpass) cutoff knee</param>
        /// <param name="sampleRate">Sample rate of the system to be EQ'd</param>
        /// <param name="resolution">Sample resolution for <see cref="At(float)"/>, must be a power of 2</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="order">Each order increases the slope with 6 dB/octave</param>
        /// <param name="gain">Filter gain</param>
        public Bandpass(double lowFreq, double highFreq, int sampleRate, int resolution, double q = QFactor.reference, int order = 1, float gain = 6) {
            positioner = resolution * 2f / sampleRate;
            float[] reference = SweepGenerator.Exponential(20, sampleRate * .5f, resolution * 2, sampleRate), response = (float[])reference.Clone();
            BandpassFlat filter = new BandpassFlat(lowFreq, highFreq, sampleRate, q, order);
            filter.Process(response);
            spectrum = Measurements.GetSpectrum(Measurements.GetFrequencyResponse(reference, response));
            GraphUtils.ConvertToDecibels(spectrum);
            this.gain = gain;
        }

        /// <summary>Get the curve's gain in decibels at a given frequency.</summary>
        public override float At(float frequency) => spectrum[(int)(frequency * positioner)] + gain;
    }
}
using Cavern.Filters;

namespace Cavern.QuickEQ.EQCurves {
    /// <summary>Bandpass EQ curve, recommended for stage subwoofers.</summary>
    public class Bandpass : EQCurve {
        /// <summary>Multiplier for each frequency that gives the position of the needed gain in <see cref="spectrum"/>.</summary>
        readonly float positioner;
        /// <summary>Precalculated EQ spectrum.</summary>
        readonly float[] spectrum;

        /// <summary>Bandpass EQ curve, recommended for stage subwoofers.</summary>
        /// <param name="lowFreq">Low frequency (highpass) cutoff knee</param>
        /// <param name="highFreq">High frequency (lowpass) cutoff knee</param>
        /// <param name="sampleRate">Sample rate of the system to be EQ'd</param>
        /// <param name="resolution">Sample resolution for <see cref="At(float)"/>, must be a power of 2</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="order">Each order increases the slope with 6 dB/octave</param>
        /// <param name="gain">Filter gain</param>
        public Bandpass(float lowFreq, float highFreq, int sampleRate, int resolution, float q = .7071067811865475f, int order = 1, float gain = 0) {
            positioner = resolution * 2f / sampleRate;
            float[] reference = Measurements.ExponentialSweep(20, sampleRate * .5f, resolution * 2, sampleRate), response = (float[])reference.Clone();
            BandpassFlat filter = new BandpassFlat(lowFreq, highFreq, sampleRate, q, order, gain);
            filter.Process(response);
            spectrum = Measurements.GetSpectrum(Measurements.GetFrequencyResponse(reference, response));
            GraphUtils.ConvertToDecibels(spectrum);
        }

        /// <summary>Get the curve's gain in decibels at a given frequency.</summary>
        public override float At(float frequency) => spectrum[(int)(frequency * positioner)];
    }
}
using Cavern.Filters.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Combination of a lowpass and a highpass filter.
    /// </summary>
    public class BandpassFlat : Filter {
        readonly int order;
        readonly Highpass[] highpasses;
        readonly Lowpass[] lowpasses;

        /// <summary>
        /// Combination of a lowpass and a highpass filter with 24 dB/octave rolloffs and no additional gain.
        /// </summary>
        /// <param name="lowFreq">Low frequency (highpass) cutoff knee</param>
        /// <param name="highFreq">High frequency (lowpass) cutoff knee</param>
        /// <param name="sampleRate">Sample rate of the system to be EQ'd</param>
        public BandpassFlat(double lowFreq, double highFreq, int sampleRate) :
            this(lowFreq, highFreq, sampleRate, QFactor.reference, 4, 0) { }

        /// <summary>
        /// Combination of a lowpass and a highpass filter with custom Q-factor and slope, but no additional gain.
        /// </summary>
        /// <param name="lowFreq">Low frequency (highpass) cutoff knee</param>
        /// <param name="highFreq">High frequency (lowpass) cutoff knee</param>
        /// <param name="sampleRate">Sample rate of the system to be EQ'd</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="order">Each order increases the slope with 6 dB/octave</param>
        public BandpassFlat(double lowFreq, double highFreq, int sampleRate, double q, int order) :
            this(lowFreq, highFreq, sampleRate, q, order, 0) { }

        /// <summary>
        /// Combination of a lowpass and a highpass filter with custom Q-factor, slopa, and additional gain.
        /// </summary>
        /// <param name="lowFreq">Low frequency (highpass) cutoff knee</param>
        /// <param name="highFreq">High frequency (lowpass) cutoff knee</param>
        /// <param name="sampleRate">Sample rate of the system to be EQ'd</param>
        /// <param name="q">Q-factor of the filter</param>
        /// <param name="order">Each order increases the slope with 6 dB/octave</param>
        /// <param name="gain">Filter gain</param>
        public BandpassFlat(double lowFreq, double highFreq, int sampleRate, double q, int order, double gain) {
            this.order = order;
            lowpasses = new Lowpass[order];
            highpasses = new Highpass[order];
            highpasses[0] = new Highpass(sampleRate, lowFreq, q);
            lowpasses[0] = new Lowpass(sampleRate, highFreq, q, gain);
            for (int filter = 1; filter < order; ++filter) {
                highpasses[filter] = new Highpass(sampleRate, lowFreq, q);
                lowpasses[filter] = new Lowpass(sampleRate, highFreq, q);
            }
        }

        /// <summary>
        /// Apply bandpass on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        public override void Process(float[] samples) {
            for (int filter = 0; filter < order; ++filter) {
                lowpasses[filter].Process(samples);
                highpasses[filter].Process(samples);
            }
        }

        /// <summary>
        /// Apply bandpass on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public override void Process(float[] samples, int channel, int channels) {
            for (int filter = 0; filter < order; ++filter) {
                lowpasses[filter].Process(samples, channel, channels);
                highpasses[filter].Process(samples, channel, channels);
            }
        }
    }
}
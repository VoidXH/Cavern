using System;

using Cavern.Filters.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Combination of a lowpass and a highpass filter.
    /// </summary>
    public class BandpassFlat : Filter {
        /// <summary>
        /// Low frequency (highpass) cutoff knee.
        /// </summary>
        public double LowFreq => highpasses[0].CenterFreq;

        /// <summary>
        /// High frequency (lowpass) cutoff knee.
        /// </summary>
        public double HighFreq => lowpasses[0].CenterFreq;

        /// <summary>
        /// Sample rate of the system to be EQ'd.
        /// </summary>
        public int SampleRate => lowpasses[0].SampleRate;

        /// <summary>
        /// Q-factor of each filter component.
        /// </summary>
        public double Q => lowpasses[0].Q;

        // TODO: is it really 6 dB?
        /// <summary>
        /// Each order increases the slope with 6 dB/octave.
        /// </summary>
        public int Order {
            get => highpasses.Length;
            set {
                if (value < 1) {
                    throw new ArgumentOutOfRangeException(nameof(Order));
                }

                Array.Resize(ref lowpasses, value);
                Array.Resize(ref highpasses, value);
                FillOrders();
            }
        }

        /// <summary>
        /// Total filter gain.
        /// </summary>
        public double Gain => lowpasses[0].Gain; // Only applied in the first lowpass

        Lowpass[] lowpasses;
        Highpass[] highpasses;

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
        /// <param name="q">Q-factor of each filter component</param>
        /// <param name="order">Each order increases the slope with 6 dB/octave</param>
        public BandpassFlat(double lowFreq, double highFreq, int sampleRate, double q, int order) :
            this(lowFreq, highFreq, sampleRate, q, order, 0) { }

        /// <summary>
        /// Combination of a lowpass and a highpass filter with custom Q-factor, slopa, and additional gain.
        /// </summary>
        /// <param name="lowFreq">Low frequency (highpass) cutoff knee</param>
        /// <param name="highFreq">High frequency (lowpass) cutoff knee</param>
        /// <param name="sampleRate">Sample rate of the system to be EQ'd</param>
        /// <param name="q">Q-factor of each filter component</param>
        /// <param name="order">Each order increases the slope with 6 dB/octave</param>
        /// <param name="gain">Total filter gain</param>
        public BandpassFlat(double lowFreq, double highFreq, int sampleRate, double q, int order, double gain) {
            lowpasses = new Lowpass[order];
            lowpasses[0] = new Lowpass(sampleRate, highFreq, q, gain);
            highpasses = new Highpass[order];
            highpasses[0] = new Highpass(sampleRate, lowFreq, q);
            FillOrders();
        }

        /// <inheritdoc/>
        public override void Process(float[] samples) {
            for (int filter = 0; filter < highpasses.Length; filter++) {
                lowpasses[filter].Process(samples);
                highpasses[filter].Process(samples);
            }
        }

        /// <inheritdoc/>
        public override void Process(float[] samples, int channel, int channels) {
            for (int filter = 0; filter < highpasses.Length; filter++) {
                lowpasses[filter].Process(samples, channel, channels);
                highpasses[filter].Process(samples, channel, channels);
            }
        }

        /// <inheritdoc/>
        public override object Clone() => new BandpassFlat(LowFreq, HighFreq, SampleRate, Q, Order, Gain);

        /// <summary>
        /// When the <see cref="Order"/> was changed, copy the first element to all other indices.
        /// </summary>
        void FillOrders() {
            for (int filter = 1; filter < lowpasses.Length; filter++) {
                lowpasses[filter] = (Lowpass)lowpasses[filter - 1].Clone();
                if (filter == 1) {
                    lowpasses[filter].Gain = 0;
                }
                highpasses[filter] = (Highpass)highpasses[0].Clone();
            }
        }
    }
}
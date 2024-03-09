namespace Cavern.Filters {
    /// <summary>
    /// Simple first-order biquad filter with the option to invert the phase response.
    /// </summary>
    public abstract class PhaseSwappableBiquadFilter : BiquadFilter {
        /// <summary>
        /// Biquad filters usually achieve their effect by delaying lower frequencies. Phase swapping delays higher frequencies.
        /// </summary>
        public bool PhaseSwapped {
            get => phaseSwapped;
            set {
                phaseSwapped = value;
                Reset(CenterFreq, Q, Gain);
            }
        }

        /// <summary>
        /// State of phase swapping to be used when the filter is reset.
        /// </summary>
        protected bool phaseSwapped;

        /// <inheritdoc/>
        protected PhaseSwappableBiquadFilter(int sampleRate, double centerFreq) : base(sampleRate, centerFreq) { }

        /// <inheritdoc/>
        protected PhaseSwappableBiquadFilter(int sampleRate, double centerFreq, double q) : base(sampleRate, centerFreq, q) { }

        /// <inheritdoc/>
        protected PhaseSwappableBiquadFilter(int sampleRate, double centerFreq, double q, double gain) :
            base(sampleRate, centerFreq, q, gain) { }
    }
}
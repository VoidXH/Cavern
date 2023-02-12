namespace Cavern.SpecialSources {
    /// <summary>
    /// A source that plays silence.
    /// </summary>
    public class MuteSource : StreamedSource {
        /// <summary>
        /// Empty cache to return.
        /// </summary>
        MultichannelWaveform samples = new MultichannelWaveform(1, 0);

        /// <summary>
        /// A source that plays silence.
        /// </summary>
        /// <param name="listener">Take the listener's sample rate to prevent redundant resampling calls</param>
        public MuteSource(Listener listener) {
            Clip = new Clip(samples, listener.SampleRate);
        }

        /// <summary>
        /// Get the next samples in the audio stream.
        /// </summary>
        protected internal override MultichannelWaveform GetSamples() {
            if (samples[0].Length != PitchedUpdateRate) {
                samples = new MultichannelWaveform(1, PitchedUpdateRate);
            }
            return samples;
        }
    }
}
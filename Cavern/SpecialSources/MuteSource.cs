namespace Cavern.SpecialSources {
    /// <summary>
    /// A source that plays silence.
    /// </summary>
    public class MuteSource : StreamedSource {
        /// <summary>
        /// Empty cache to return.
        /// </summary>
        readonly float[][] samples = new float[][] { new float[0] };

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
        protected internal override float[][] GetSamples() {
            if (samples[0].Length != PitchedUpdateRate) {
                samples[0] = new float[PitchedUpdateRate];
            }
            return samples;
        }
    }
}
using System;

namespace Cavern.SpecialSources {
    /// <summary>
    /// Important properties of concatenated clips don't match.
    /// </summary>
    public class InfiniteSourceMismatchException : Exception {
        /// <summary>
        /// Important properties of concatenated clips don't match.
        /// </summary>
        public InfiniteSourceMismatchException(string message) : base(message) { }
    }

    /// <summary>
    /// A <see cref="Source"/> with an intro <see cref="Clip"/> and a looping part after.
    /// </summary>
    public class InfiniteSource : Source {
        /// <summary>
        /// Clip to start playback with.
        /// </summary>
        public Clip intro;

        /// <summary>
        /// Clip to play continuously after.
        /// </summary>
        public Clip loopClip;

        /// <summary>
        /// The intro playback has finished.
        /// </summary>
        bool introPassed;

        /// <summary>
        /// Cache the samples if the source should be rendered. This wouldn't be thread safe.
        /// </summary>
        /// <returns>The collection should be performed, as all requirements are met</returns>
        protected internal override bool Precollect() {
            Clip = introPassed ? loopClip : intro;
            return base.Precollect();
        }

        /// <summary>
        /// Get the next samples in the audio stream.
        /// </summary>
        protected internal override MultichannelWaveform GetSamples() {
            Rendered = base.GetSamples();
            if (!introPassed && TimeSamples + PitchedUpdateRate >= Clip.Samples) {
                if (intro.Channels != loopClip.Channels) {
                    throw new InfiniteSourceMismatchException("Intro and loop clip channel count don't match.");
                }
                if (intro.SampleRate != loopClip.SampleRate) {
                    throw new InfiniteSourceMismatchException("Intro and loop clip sample rate don't match.");
                }

                introPassed = true;
                TimeSamples -= Clip.Samples;
                MultichannelWaveform beginning = new MultichannelWaveform(loopClip.Channels, -TimeSamples);
                loopClip.GetDataNonLooping(beginning, 0);
                for (int channel = 0; channel < loopClip.Channels; ++channel) {
                    for (int start = PitchedUpdateRate + TimeSamples, sample = start; sample < PitchedUpdateRate; ++sample) {
                        Rendered[channel][sample] = beginning[channel][sample - start];
                    }
                }
                TimeSamples = -TimeSamples - PitchedUpdateRate;
            }

            return Rendered;
        }
    }
}
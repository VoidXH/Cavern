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
        public Clip Intro;

        /// <summary>
        /// Clip to play continuously after.
        /// </summary>
        public Clip LoopClip;

        /// <summary>
        /// The intro playback has finished.
        /// </summary>
        bool introPassed;

        /// <summary>
        /// Cache the samples if the source should be rendered. This wouldn't be thread safe.
        /// </summary>
        /// <returns>The collection should be performed, as all requirements are met</returns>
        protected internal override bool Precollect() {
            Clip = introPassed ? LoopClip : Intro;
            return base.Precollect();
        }

        /// <summary>
        /// Get the next samples in the audio stream.
        /// </summary>
        protected internal override float[][] GetSamples() {
            Rendered = base.GetSamples();
            if (!introPassed && TimeSamples + PitchedUpdateRate >= Clip.Samples) {
                if (Intro.Channels != LoopClip.Channels) {
                    throw new InfiniteSourceMismatchException("Intro and loop clip channel count don't match.");
                }
                if (Intro.SampleRate != LoopClip.SampleRate) {
                    throw new InfiniteSourceMismatchException("Intro and loop clip sample rate don't match.");
                }

                introPassed = true;
                TimeSamples -= Clip.Samples;
                float[][] beginning = new float[LoopClip.Channels][];
                for (int channel = 0; channel < LoopClip.Channels; ++channel) {
                    beginning[channel] = new float[-TimeSamples];
                }
                LoopClip.GetDataNonLooping(beginning, 0);
                for (int channel = 0; channel < LoopClip.Channels; ++channel) {
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
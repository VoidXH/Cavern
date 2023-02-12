using System;

using Cavern.Utilities;

namespace Cavern.QuickEQ.SignalGeneration {
    /// <summary>
    /// Generates noise for a single channel.
    /// </summary>
    public sealed class NoiseGenerator : Source {
        /// <summary>
        /// Target output channel.
        /// </summary>
        public int channel;

        /// <summary>
        /// Rendered output array kept to save allocation time.
        /// </summary>
        float[] rendered = new float[0];

        /// <summary>
        /// Random number generator.
        /// </summary>
        readonly Random generator = new Random();

        /// <summary>
        /// Set up rendering environment.
        /// </summary>
        protected override bool Precollect() {
            int renderBufferSize = Listener.Channels.Length * listener.UpdateRate;
            if (rendered.Length != renderBufferSize) {
                rendered = new float[renderBufferSize];
            }
            return true;
        }

        /// <summary>
        /// Generate noise on the target channel.
        /// </summary>
        protected override float[] Collect() {
            rendered.Clear();
            if (IsPlaying && !Mute) {
                if (channel < 0 || channel >= Listener.Channels.Length) {
                    return rendered;
                }
                float gain = Volume * 2;
                for (int sample = channel; sample < rendered.Length; sample += Listener.Channels.Length) {
                    rendered[sample] = (float)generator.NextDouble() * gain - Volume;
                }
            }
            return rendered;
        }
    }
}
using System;

namespace Cavern.QuickEQ {
    /// <summary>Generates noise for a single channel.</summary>
    public sealed class NoiseGenerator : Source {
        /// <summary>Target output channel.</summary>
        public int channel = 0;

        /// <summary>Rendered output array kept to save allocation time.</summary>
        float[] rendered = new float[0];

        /// <summary>Random number generator.</summary>
        readonly Random generator = new Random();

        /// <summary>Set up rendering environment.</summary>
        protected override bool Precollect() {
            int renderBufferSize = Listener.Channels.Length * listener.UpdateRate;
            if (rendered.Length != renderBufferSize)
                rendered = new float[renderBufferSize];
            return true;
        }

        /// <summary>Generate noise on the target channel.</summary>
        protected override float[] Collect() {
            Array.Clear(rendered, 0, rendered.Length);
            if (IsPlaying && !Mute) {
                int channels = Listener.Channels.Length;
                if (channel < 0 || channel >= channels)
                    return rendered;
                float gain = Volume * 2;
                for (int sample = channel; sample < rendered.Length; sample += channels)
                    rendered[sample] = (float)generator.NextDouble() * gain - Volume;
            }
            return rendered;
        }
    }
}
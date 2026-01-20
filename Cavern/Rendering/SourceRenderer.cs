using System.Numerics;

using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Rendering {
    /// <summary>
    /// Represents a technique for mixing <see cref="Source"/>s to <see cref="Listener"/> environments.
    /// </summary>
    public abstract class SourceRenderer {
        /// <summary>
        /// Mix the audio <paramref name="source"/> to its <see cref="Listener"/>.
        /// </summary>
        /// <param name="listener">Rendering environment</param>
        /// <param name="source">The audio source to render</param>
        /// <param name="direction">The direction of the <see cref="Source"/> from the <see cref="Listener"/>,
        /// in a space where the <see cref="Listener"/> is the origin</param>
        /// <param name="samples">Samples rendered by <see cref="Source.Precollect"/></param>
        /// <param name="rendered">The per-<see cref="Source"/> output mix for the <see cref="Listener.Channels"/></param>
        /// <param name="gain">Mixing volume in linear gain, not decibels</param>
        public abstract void Render(Listener listener, Source source, Vector3 direction, float[] samples, float[] rendered, float gain);

        /// <summary>
        /// Mix the input <paramref name="samples"/> to the LFE tracks of the <paramref name="rendered"/> result.
        /// The <paramref name="gain"/> shouldn't account for the -10 dB LFE mixing level, it will be calculated here.
        /// </summary>
        /// <param name="samples"></param>
        /// <param name="rendered"></param>
        /// <param name="gain"></param>
        protected static void MixToLFE(float[] samples, float[] rendered, float gain) {
            gain *= Gain.minus10dB;
            Channel[] channels = Listener.Channels;
            for (int channel = 0; channel < channels.Length; channel++) {
                if (channels[channel].LFE) {
                    WaveformUtils.Mix(samples, rendered, channel, channels.Length, gain);
                }
            }
        }
    }
}

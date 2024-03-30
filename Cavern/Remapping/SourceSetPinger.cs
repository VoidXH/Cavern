using System.Collections.Generic;
using System.Linq;

namespace Cavern.Remapping {
    /// <summary>
    /// Gets the next few samples of multiple related <see cref="Source"/>s.
    /// </summary>
    internal sealed class SourceSetPinger {
        /// <summary>
        /// A dummy listener to forcefully get source samples.
        /// </summary>
        readonly Listener pinger;

        /// <summary>
        /// Outputs from the <see cref="pinger"/> will be passed through this cache array.
        /// </summary>
        readonly float[][] input;

        /// <summary>
        /// Sources to ping. Don't attach these to a <see cref="Listener"/> anywhere else.
        /// </summary>
        readonly Source[] sources;

        /// <summary>
        /// Gets the next few samples of multiple related <see cref="Source"/>s.
        /// </summary>
        /// <param name="sources">Sources to ping, which aren't attached to a <see cref="Listener"/> anywhere else</param>
        /// <param name="sampleRate">Sample rate of each of the <paramref name="sources"/></param>
        public SourceSetPinger(IReadOnlyList<Source> sources, int sampleRate) {
            pinger = new Listener(false) {
                SampleRate = sampleRate
            };
            input = new float[sources.Count][];
            this.sources = sources.ToArray();
            for (int i = 0; i < input.Length; i++) {
                pinger.AttachSource(sources[i]);
            }
        }

        /// <summary>
        /// Get the next set of samples.
        /// </summary>
        public float[][] Update(int samplesPerSource) {
            pinger.UpdateRate = samplesPerSource;
            pinger.Ping();
            for (int i = 0; i < input.Length; i++) {
                input[i] = sources[i].Rendered[0];
            }
            return input;
        }
    }
}
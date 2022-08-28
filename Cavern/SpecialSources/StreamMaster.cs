using System.Collections.Generic;

namespace Cavern.SpecialSources {
    /// <summary>
    /// Supplies a group of sources with audio data.
    /// </summary>
    public class StreamMaster {
        /// <summary>
        /// Gets the next block of rendered samples.
        /// </summary>
        public delegate float[][] GetNextSampleBlock(int samplesPerSource);

        /// <summary>
        /// Gets the next block of rendered samples.
        /// </summary>
        readonly GetNextSampleBlock getter;

        /// <summary>
        /// Next output samples.
        /// </summary>
        float[][] nextSamples;

        /// <summary>
        /// Number of updates in the current frame. If reaches the number of supplied objects, new samples are retrieved.
        /// </summary>
        int updates;

        /// <summary>
        /// Supplies a group of sources with audio data.
        /// </summary>
        /// <param name="getter">Getter of the next block of rendered samples</param>
        public StreamMaster(GetNextSampleBlock getter) => this.getter = getter;

        /// <summary>
        /// Add a dummy clip to the sources to be able to be rendered.
        /// </summary>
        public void SetupSources(IReadOnlyList<Source> sources, int sampleRate) {
            Clip clip = new Clip(new float[1], 1, sampleRate);
            for (int i = 0, c = sources.Count; i < c; ++i) {
                sources[i].Clip = clip;
                sources[i].Loop = true;
            }
        }

        /// <summary>
        /// Get the samples for a given source, fetch new samples when needed.
        /// </summary>
        internal float[] Update(int source, int samples) {
            if (updates == 0) {
                nextSamples = getter(samples);
            }
            if (++updates == nextSamples.Length) {
                updates = 0;
            }
            return nextSamples[source];
        }
    }
}
using System.Runtime.CompilerServices;

using Cavern.SpecialSources;

namespace Cavern.Remapping {
    /// <summary>
    /// Creates new, intermediate sources of an existing channel-based render.
    /// </summary>
    public abstract class Upmixer {
        /// <summary>
        /// Gets samples for each source for a given update rate.
        /// </summary>
        public delegate float[][] SampleCollector(int samplesPerSource);

        /// <summary>
        /// This function is called when new samples are needed for the next frame, it should return a frame for each source.
        /// </summary>
        public event SampleCollector OnSamplesNeeded;

        /// <summary>
        /// Output sources created by the upmixing process.
        /// </summary>
        public Source[] IntermediateSources { get; private set; }

        /// <summary>
        /// Preallocated output source sample array reference cache.
        /// </summary>
        protected readonly float[][] output;

        /// <summary>
        /// Creates new, intermediate sources of an existing channel-based render.
        /// </summary>
        /// <param name="sourceCount">Number of output sources</param>
        /// <param name="sampleRate">Content sample rate</param>
        protected Upmixer(int sourceCount, int sampleRate) {
            StreamMaster reader = new StreamMaster(UpdateSourcesFully);
            IntermediateSources = new Source[sourceCount];
            output = new float[sourceCount][];
            output[0] = new float[0];
            for (int i = 0; i < sourceCount; i++) {
                IntermediateSources[i] = new StreamMasterSource(reader, i) {
                    VolumeRolloff = Rolloffs.Disabled
                };
            }
            reader.SetupSources(IntermediateSources, sampleRate);
        }

        /// <summary>
        /// Uses the sample collector to read new samples.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected float[][] GetNewSamples(int samplesPerSource) => OnSamplesNeeded(samplesPerSource);

        /// <summary>
        /// Get the input samples, place the upmixed targets in space, and return their samples.
        /// </summary>
        protected abstract float[][] UpdateSources(int samplesPerSource);

        /// <summary>
        /// Get the input samples, place the upmixed targets in space, and return their samples.
        /// Calls the overridden <see cref="UpdateSources(int)"/> and makes sure the cache arrays are the correct size.
        /// </summary>
        float[][] UpdateSourcesFully(int samplesPerSource) {
            if (output[0].Length != samplesPerSource) {
                for (int i = 0; i < output.Length; i++) {
                    output[i] = new float[samplesPerSource];
                }
            }
            return UpdateSources(samplesPerSource);
        }
    }
}
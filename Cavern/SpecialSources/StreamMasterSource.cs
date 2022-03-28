namespace Cavern.SpecialSources {
    /// <summary>
    /// A streamed source that uses a <see cref="StreamMaster"/> to fetch new samples from.
    /// </summary>
    public class StreamMasterSource : StreamedSource {
        /// <summary>
        /// The supplier of samples.
        /// </summary>
        readonly StreamMaster master;

        /// <summary>
        /// Source ID used by the <see cref="master"/>.
        /// </summary>
        readonly int source;

        /// <summary>
        /// A streamed source that uses a <see cref="StreamMaster"/> to fetch new samples from.
        /// </summary>
        /// <param name="master">The supplier of samples</param>
        /// <param name="source">Source ID used by the <see cref="master"/></param>
        public StreamMasterSource(StreamMaster master, int source) {
            this.master = master;
            this.source = source;
        }

        /// <summary>
        /// Get the next samples in the audio stream.
        /// </summary>
        protected internal override float[][] GetSamples() => new float[1][] { master.Update(source, PitchedUpdateRate) };
    }
}
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
        /// Source index used by the <see cref="master"/>.
        /// </summary>
        readonly int sourceIndex;

        /// <summary>
        /// A streamed source that uses a <see cref="StreamMaster"/> to fetch new samples from.
        /// </summary>
        /// <param name="master">The supplier of samples</param>
        /// <param name="sourceIndex">Source index used by the <see cref="master"/></param>
        public StreamMasterSource(StreamMaster master, int sourceIndex) {
            this.master = master;
            this.sourceIndex = sourceIndex;
        }

        /// <summary>
        /// Get the next samples in the audio stream.
        /// </summary>
        protected internal override MultichannelWaveform GetSamples() =>
            new MultichannelWaveform(master.Update(sourceIndex, PitchedUpdateRate));
    }
}
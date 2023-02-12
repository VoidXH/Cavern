using Cavern.Utilities;

namespace Cavern.Remapping {
    /// <summary>
    /// Easily editable clip for <see cref="Remapper"/>'s channel spoofing.
    /// </summary>
    sealed class RemappedChannel : Clip {
        /// <summary>
        /// Easily editable clip for <see cref="Remapper"/>'s channel spoofing.
        /// </summary>
        /// <param name="updateRate">Source stream's update rate</param>
        public RemappedChannel(int updateRate) : base(new MultichannelWaveform(1, updateRate), updateRate) =>
            SampleRate = Listener.DefaultSampleRate;

        /// <summary>
        /// Apply the new update rate of the <see cref="Remapper"/>.
        /// </summary>
        public void Remake(int updateRate) => data = new MultichannelWaveform(1, updateRate);

        /// <summary>
        /// Read samples from the source for the next frame.
        /// </summary>
        /// <param name="stream">Source stream</param>
        /// <param name="channel">Target channel</param>
        /// <param name="channels">Source channel count</param>
        public void Update(float[] stream, int channel, int channels) =>
            WaveformUtils.ExtractChannel(stream, data[0], channel, channels);
    }
}
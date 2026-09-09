namespace Cavern.Format.Utilities {
    /// <summary>
    /// Mixing utilities for creating specific waveform structures.
    /// </summary>
    public static class WaveformTransforms {
        /// <summary>
        /// Creates a channel-offset waveform array where channels are played sequentially.
        /// </summary>
        /// <param name="samples">All input samples.</param>
        public static float[][] OffsetByChannel(float[][] samples) => OffsetByChannel(samples, samples.Length);

        /// <summary>
        /// Creates a channel-offset waveform array where channels are played sequentially with a given period.
        /// </summary>
        /// <param name="samples">Input samples per channel.</param>
        /// <param name="period">Channels separated by this many channels are played simultaneously.</param>
        /// <returns>Array of sample arrays ready for sequential writing.</returns>
        /// <remarks>To play all channels sequentially, set the <paramref name="period"/> to the number of channels.</remarks>
        public static float[][] OffsetByChannel(float[][] samples, int period) {
            float[] empty = new float[samples[0].Length];
            float[][] holder = new float[samples.Length][];
            for (int curPeriod = 0; curPeriod < period; curPeriod++) {
                for (int channel = 0; channel < holder.Length; channel++) {
                    holder[channel] = channel % period == curPeriod ? samples[channel] : empty;
                }
            }
            return holder;
        }
    }
}

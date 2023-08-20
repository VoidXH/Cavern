using System;

using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Abstract audio filter.
    /// </summary>
    /// <remarks>You have to override at least one Process function, otherwise they'll call each other.</remarks>
    public abstract class Filter {
        /// <summary>
        /// Apply this filter on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        public virtual void Process(float[] samples) => Process(samples, 0, 1);

        /// <summary>
        /// Apply this filter on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        /// <param name="samples">Input samples</param>
        /// <param name="channel">Channel to filter</param>
        /// <param name="channels">Total channels</param>
        public virtual void Process(float[] samples, int channel, int channels) {
            int channelSize = samples.Length / channels;
            float[] singleChannel = new float[channelSize];
            WaveformUtils.ExtractChannel(samples, singleChannel, channel, channels);
            Process(singleChannel);
            WaveformUtils.Insert(singleChannel, samples, channel, channels);
        }

        /// <summary>
        /// Some filters, like <see cref="Normalizer"/> require block-by-block processing, and can't handle an entire signal
        /// at once. This function will process the <paramref name="samples"/> block by block with a given <paramref name="blockSize"/>.
        /// </summary>
        public void ProcessInBlocks(float[] samples, long blockSize) {
            float[] block = new float[blockSize];
            for (long i = 0; i < samples.LongLength; i += blockSize) {
                long currentBlock = Math.Min(blockSize, samples.LongLength - i);
                Array.Copy(samples, i, block, 0, currentBlock);
                Process(block);
                Array.Copy(block, 0, samples, i, currentBlock);
            }
        }

        /// <summary>
        /// Some filters, like <see cref="Normalizer"/> require block-by-block processing, and can't handle an entire signal
        /// at once. This function will process a single channel of the multichannel signal contained in the <paramref name="samples"/>
        /// block by block with a given <paramref name="blockSize"/>.
        /// </summary>
        public void ProcessInBlocks(float[] samples, int channel, int channels, long blockSize) {
            float[] block = new float[blockSize];
            long samplesPerChannel = samples.LongLength / channels;
            for (long i = 0; i < samplesPerChannel; i += blockSize) {
                WaveformUtils.ExtractChannel(samples, i * channels, block, channel, channels);
                Process(block);
                WaveformUtils.Insert(block, samples, (int)i * channels + channel, channels);
            }
        }
    }
}
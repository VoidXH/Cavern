using System;
using System.Xml;

using Cavern.Utilities;

namespace Cavern.Filters {
    /// <summary>
    /// Abstract audio filter.
    /// </summary>
    /// <remarks>You have to override at least one Process function, otherwise they'll call each other.</remarks>
    public abstract class Filter : ICloneable {
        /// <summary>
        /// <see cref="Process"/>ing a Dirac-delta will result in an impulse response that will result in the same exact filter
        /// when used as convolution samples.
        /// </summary>
        public virtual bool LinearTimeInvariant => true;

        /// <summary>
        /// Using an XML <paramref name="reader"/> that is currently at an element start, return the described filter in the XML file.
        /// </summary>
        /// <exception cref="XmlException">The filter is unknown or the reading of the filter from XML is not implemented</exception>
        public static Filter FromXml(XmlReader reader) {
            Type filterType = Type.GetType("Cavern.Filters." + reader.Name);
            if (filterType != null && typeof(BiquadFilter).IsAssignableFrom(filterType)) {
                BiquadFilter filter = (BiquadFilter)Activator.CreateInstance(filterType, Listener.DefaultSampleRate, 100);
                filter.ReadXml(reader);
                return filter;
            }

            switch (reader.Name) {
                case nameof(BypassFilter):
                    BypassFilter bypass = new BypassFilter(string.Empty);
                    bypass.ReadXml(reader);
                    return bypass;
                case nameof(Comb):
                    Comb comb = new Comb(Listener.DefaultSampleRate, 0, 0);
                    comb.ReadXml(reader);
                    return comb;
                case nameof(Convolver):
                    Convolver convolver = new Convolver(Array.Empty<float>(), Listener.DefaultSampleRate);
                    convolver.ReadXml(reader);
                    return convolver;
                case nameof(Delay):
                    Delay delay = new Delay(0);
                    delay.ReadXml(reader);
                    return delay;
                case nameof(Echo):
                    Echo echo = new Echo(Listener.DefaultSampleRate);
                    echo.ReadXml(reader);
                    return echo;
                case nameof(FastConvolver):
                    FastConvolver fastConvolver = new FastConvolver(new float[2]);
                    fastConvolver.ReadXml(reader);
                    return fastConvolver;
                case nameof(Gain):
                    Gain gain = new Gain(0);
                    gain.ReadXml(reader);
                    return gain;
                case nameof(SpikeConvolver):
                    SpikeConvolver spikeConvolver = new SpikeConvolver(new float[0], 0);
                    spikeConvolver.ReadXml(reader);
                    return spikeConvolver;
                default:
                    throw new XmlException();
            }
        }

        /// <summary>
        /// Apply this filter on an array of samples. One filter should be applied to only one continuous stream of samples.
        /// </summary>
        /// <param name="samples">Input samples</param>
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

        /// <inheritdoc/>
        public abstract object Clone();

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
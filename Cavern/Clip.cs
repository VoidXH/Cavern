using System;

namespace Cavern {
    /// <summary>
    /// Audio content.
    /// </summary>
    public class Clip {
        /// <summary>
        /// Name of the clip.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Channel count for the clip.
        /// </summary>
        public int Channels => data.Channels;

        /// <summary>
        /// The length of the clip in samples, for a single channel.
        /// </summary>
        public int Samples => data[0].Length;

        /// <summary>
        /// The length of the clip in seconds.
        /// </summary>
        public float Length => Samples / (float)SampleRate;

        /// <summary>
        /// Sampling rate of the clip.
        /// </summary>
        public int SampleRate { get; protected set; }

        /// <summary>
        /// Samples for each channel.
        /// </summary>
        protected MultichannelWaveform data;

        /// <summary>
        /// Audio content.
        /// </summary>
        /// <param name="data">Audio data, with the size of [channels][samples for given channel]</param>
        /// <param name="sampleRate">Sample rate</param>
        public Clip(MultichannelWaveform data, int sampleRate) {
            SampleRate = sampleRate;
            this.data = data;
        }

        /// <summary>
        /// Audio content.
        /// </summary>
        /// <param name="data">Audio data, with interlaced channels</param>
        /// <param name="channels">Channel count</param>
        /// <param name="sampleRate">Sample rate</param>
        public Clip(float[] data, int channels, int sampleRate) {
            SampleRate = sampleRate;
            this.data = new MultichannelWaveform(data, channels);
        }

        /// <summary>
        /// Implicit null check.
        /// </summary>
        public static implicit operator bool(Clip clip) => clip != null;

        /// <summary>
        /// Fills an array with sample data from the clip.
        /// Clip data overflows, and free samples are filled with the beginning of the Clip.
        /// </summary>
        /// <param name="output">Audio data cache</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        /// <returns>The operation was successful as the channel counts matched.</returns>
        public bool GetData(MultichannelWaveform output, int offset) {
            if (output.Channels != data.Channels) {
                return false;
            }

            int dataPos = 0;
            while (dataPos < output[0].Length) {
                int samplesThisRound = data[0].Length - offset;
                if (samplesThisRound > output[0].Length - dataPos) {
                    samplesThisRound = output[0].Length - dataPos;
                }
                for (int channel = 0; channel < data.Channels; channel++) {
                    Array.Copy(data[channel], offset, output[channel], dataPos, samplesThisRound);
                }
                dataPos += samplesThisRound;
                if ((offset += samplesThisRound) == data[0].Length) {
                    offset = 0;
                }
            }
            return true;
        }

        /// <summary>
        /// Fills an array with sample data from the clip.
        /// Clip data doesn't overflow and free samples are filled with zeros.
        /// </summary>
        /// <param name="output">Audio data cache</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        /// <returns>The operation was successful as the channel counts matched.</returns>
        public bool GetDataNonLooping(MultichannelWaveform output, int offset) {
            if (output.Channels != data.Channels) {
                return false;
            }

            int endPos = data[0].Length - offset;
            if (endPos > output[0].Length) {
                endPos = output[0].Length;
            }
            for (int channel = 0; channel < data.Channels; channel++) {
                Array.Copy(data[channel], offset, output[channel], 0, endPos);
            }
            for (int channel = 0; channel < data.Channels; channel++) {
                Array.Clear(output[channel], endPos, output[channel].Length - endPos);
            }
            return true;
        }

        /// <summary>
        /// Fills an array with sample data from the clip.
        /// Clip data overflows, and free samples are filled with the beginning of the Clip.
        /// </summary>
        /// <param name="output">Audio data cache</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        /// <returns>The operation was successful as the channel counts matched.</returns>
        public unsafe bool GetData(float[] output, int offset) {
            if (output.Length % data.Channels != 0) {
                return false;
            }

            fixed (float* pData = output) {
                int dataPos = 0;
                int perChannel = output.Length / data.Channels;
                while (dataPos < perChannel) {
                    int samplesThisRound = data[0].Length - offset;
                    if (samplesThisRound > perChannel - dataPos) {
                        samplesThisRound = perChannel - dataPos;
                    }
                    for (int channel = 0; channel < data.Channels; channel++) {
                        float* dataOut = pData + channel + data.Channels * dataPos;
                        fixed (float* pSource = data[channel]) {
                            float* source = pSource + offset,
                                end = source + samplesThisRound;
                            while (source != end) {
                                *dataOut = *source++;
                                dataOut += data.Channels;
                            }
                        }
                    }
                    dataPos += samplesThisRound;
                    if ((offset += samplesThisRound) == data[0].Length) {
                        offset = 0;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Fills an array with a single channel's sample data from the clip.
        /// Clip data overflows, and free samples are filled with the beginning of the Clip.
        /// </summary>
        /// <param name="output">Audio data cache</param>
        /// <param name="channel">Channel ID to get samples from</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        /// <returns>The operation was successful.</returns>
        public bool GetData(float[] output, int channel, int offset) {
            if (channel >= data.Channels) {
                return false;
            }

            int dataPos = 0;
            float[] source = data[channel];
            while (dataPos < output.Length) {
                int samplesThisRound = source.Length - offset;
                if (samplesThisRound > output.Length - dataPos) {
                    samplesThisRound = output.Length - dataPos;
                }
                Array.Copy(source, offset, output, dataPos, samplesThisRound);
                dataPos += samplesThisRound;
                if ((offset += samplesThisRound) == source.Length) {
                    offset = 0;
                }
            }
            return true;
        }

        /// <summary>
        /// Fills an array with sample data from the clip. Clip data doesn't overflow and free samples are filled with zeros.
        /// </summary>
        /// <param name="output">Audio data cache</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        /// <returns>The operation was successful as the channel counts matched.</returns>
        public bool GetDataNonLooping(float[] output, int offset) {
            if (output.Length % data.Channels != 0) {
                return false;
            }

            int endPos = data.Channels * (data[0].Length - offset);
            if (endPos > output.Length) {
                endPos = output.Length;
            }
            for (int channel = 0; channel < data.Channels; channel++) {
                int dataPos = channel;
                float[] channelData = data[channel];
                int channelOffset = offset;
                while (dataPos < endPos) {
                    output[dataPos] = channelData[channelOffset++];
                    dataPos += data.Channels;
                }
            }
            Array.Clear(output, endPos, output.Length - endPos);
            return true;
        }

        /// <summary>
        /// Overwrite samples in this clip.
        /// </summary>
        /// <param name="input">Data source</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        /// <returns>The operation was successful as the channel counts matched.</returns>
        public bool SetData(MultichannelWaveform input, int offset) {
            if (input.Channels != data.Channels) {
                return false;
            }

            int dataPos = 0;
            while (dataPos < input[0].Length) {
                int samplesThisRound = data[0].Length - offset;
                if (samplesThisRound > input[0].Length - dataPos) {
                    samplesThisRound = input[0].Length - dataPos;
                }
                for (int channel = 0; channel < data.Channels; channel++) {
                    Array.Copy(input[channel], dataPos, data[channel], offset, samplesThisRound);
                }
                dataPos += samplesThisRound;
                if ((offset += samplesThisRound) == data[0].Length) {
                    offset = 0;
                }
            }
            return true;
        }

        /// <summary>
        /// Overwrite samples in this clip.
        /// </summary>
        /// <param name="input">Data source</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        /// <returns>The operation was successful as the channel counts matched.</returns>
        public unsafe bool SetData(float[] input, int offset) {
            if (input.Length % data.Channels != 0) {
                return false;
            }

            fixed (float* pData = input) {
                int dataPos = 0;
                while (dataPos < input.Length) {
                    int samplesThisRound = data[0].Length - offset;
                    if (samplesThisRound > input.Length - dataPos) {
                        samplesThisRound = input.Length - dataPos;
                    }
                    for (int channel = 0; channel < data.Channels; channel++) {
                        float* dataIn = pData + channel + dataPos;
                        fixed (float* pSource = data[channel]) {
                            float* source = pSource + offset,
                                end = source + samplesThisRound;
                            while (source != end) {
                                *source++ = *dataIn;
                                dataIn += data.Channels;
                            }
                        }
                    }
                    dataPos += samplesThisRound * data.Channels;
                    if ((offset += samplesThisRound) == data[0].Length) {
                        offset = 0;
                    }
                }
            }
            return true;
        }
    }
}
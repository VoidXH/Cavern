using System;

using Cavern.Utilities;

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
        public int Channels => data.Length;

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
        protected float[][] data;

        /// <summary>
        /// Audio content.
        /// </summary>
        /// <param name="data">Audio data, with the size of [channels][samples for given channel]</param>
        /// <param name="sampleRate">Sample rate</param>
        public Clip(float[][] data, int sampleRate) {
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
            int sampleCount = data.Length / channels;
            this.data = new float[channels][];
            for (int channel = 0; channel < channels; channel++) {
                WaveformUtils.ExtractChannel(data, this.data[channel] = new float[sampleCount], channel, channels);
            }
        }

        /// <summary>
        /// Implicit null check.
        /// </summary>
        public static implicit operator bool(Clip clip) => clip != null;

        /// <summary>
        /// Fills an array with sample data from the clip.
        /// Clip data overflows, and free samples are filled with the beginning of the Clip.
        /// </summary>
        /// <param name="data">Audio data cache</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        /// <returns>The operation was successful as the channel counts matched.</returns>
        public bool GetData(float[][] data, int offset) {
            if (data.Length != this.data.Length) {
                return false;
            }

            int dataPos = 0;
            while (dataPos < data[0].Length) {
                int samplesThisRound = this.data[0].Length - offset;
                if (samplesThisRound > data[0].Length - dataPos) {
                    samplesThisRound = data[0].Length - dataPos;
                }
                for (int channel = 0; channel < this.data.Length; channel++) {
                    Array.Copy(this.data[channel], offset, data[channel], dataPos, samplesThisRound);
                }
                dataPos += samplesThisRound;
                if ((offset += samplesThisRound) == this.data[0].Length) {
                    offset = 0;
                }
            }
            return true;
        }

        /// <summary>
        /// Fills an array with sample data from the clip.
        /// Clip data doesn't overflow and free samples are filled with zeros.
        /// </summary>
        /// <param name="data">Audio data cache</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        /// <returns>The operation was successful as the channel counts matched.</returns>
        public bool GetDataNonLooping(float[][] data, int offset) {
            if (data.Length != this.data.Length) {
                return false;
            }

            int endPos = this.data[0].Length - offset;
            if (endPos > data[0].Length)
                endPos = data[0].Length;
            for (int channel = 0; channel < this.data.Length; channel++) {
                Array.Copy(this.data[channel], offset, data[channel], 0, endPos);
            }
            for (int channel = 0; channel < this.data.Length; channel++) {
                Array.Clear(data[channel], endPos, data[channel].Length - endPos);
            }
            return true;
        }

        /// <summary>
        /// Fills an array with sample data from the clip.
        /// Clip data overflows, and free samples are filled with the beginning of the Clip.
        /// </summary>
        /// <param name="data">Audio data cache</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        /// <returns>The operation was successful as the channel counts matched.</returns>
        public unsafe bool GetData(float[] data, int offset) {
            if (data.Length % this.data.Length != 0) {
                return false;
            }

            fixed (float* pData = data) {
                int dataPos = 0;
                int perChannel = data.Length / this.data.Length;
                while (dataPos < perChannel) {
                    int samplesThisRound = this.data[0].Length - offset;
                    if (samplesThisRound > perChannel - dataPos) {
                        samplesThisRound = perChannel - dataPos;
                    }
                    for (int channel = 0; channel < this.data.Length; channel++) {
                        float* dataOut = pData + channel + this.data.Length * dataPos;
                        fixed (float* pSource = this.data[channel]) {
                            float* source = pSource + offset,
                                end = source + samplesThisRound;
                            while (source != end) {
                                *dataOut = *source++;
                                dataOut += this.data.Length;
                            }
                        }
                    }
                    dataPos += samplesThisRound;
                    if ((offset += samplesThisRound) == this.data[0].Length) {
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
        /// <param name="data">Audio data cache</param>
        /// <param name="channel">Channel ID to get samples from</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        /// <returns>The operation was successful as the channel counts matched.</returns>
        public bool GetData(float[] data, int channel, int offset) {
            if (data.Length != this.data[0].Length) {
                return false;
            }

            int dataPos = 0;
            float[] source = this.data[channel];
            while (dataPos < data.Length) {
                int samplesThisRound = source.Length - offset;
                if (samplesThisRound > data.Length - dataPos) {
                    samplesThisRound = data.Length - dataPos;
                }
                Array.Copy(source, offset, data, dataPos, samplesThisRound);
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
        /// <param name="data">Audio data cache</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        /// <returns>The operation was successful as the channel counts matched.</returns>
        public bool GetDataNonLooping(float[] data, int offset) {
            if (data.Length % this.data.Length != 0) {
                return false;
            }

            int endPos = this.data.Length * (this.data[0].Length - offset);
            if (endPos > data.Length) {
                endPos = data.Length;
            }
            for (int channel = 0; channel < this.data.Length; channel++) {
                int dataPos = channel;
                float[] output = this.data[channel];
                int channelOffset = offset;
                while (dataPos < endPos) {
                    data[dataPos] = output[channelOffset++];
                    dataPos += this.data.Length;
                }
            }
            Array.Clear(data, endPos, data.Length - endPos);
            return true;
        }

        /// <summary>
        /// Overwrite samples in this clip.
        /// </summary>
        /// <param name="data">Data source</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        /// <returns>The operation was successful as the channel counts matched.</returns>
        public bool SetData(float[][] data, int offset) {
            if (data.Length != this.data.Length) {
                return false;
            }

            int dataPos = 0;
            while (dataPos < data[0].Length) {
                int samplesThisRound = this.data[0].Length - offset;
                if (samplesThisRound > data[0].Length - dataPos) {
                    samplesThisRound = data[0].Length - dataPos;
                }
                for (int channel = 0; channel < this.data.Length; channel++) {
                    Array.Copy(data[channel], dataPos, this.data[channel], offset, samplesThisRound);
                }
                dataPos += samplesThisRound;
                if ((offset += samplesThisRound) == this.data[0].Length) {
                    offset = 0;
                }
            }
            return true;
        }

        /// <summary>
        /// Overwrite samples in this clip.
        /// </summary>
        /// <param name="data">Data source</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        /// <returns>The operation was successful as the channel counts matched.</returns>
        public unsafe bool SetData(float[] data, int offset) {
            if (data.Length % this.data.Length != 0) {
                return false;
            }

            fixed (float* pData = data) {
                int dataPos = 0;
                while (dataPos < data.Length) {
                    int samplesThisRound = this.data[0].Length - offset;
                    if (samplesThisRound > data.Length - dataPos) {
                        samplesThisRound = data.Length - dataPos;
                    }
                    for (int channel = 0; channel < this.data.Length; channel++) {
                        float* dataIn = pData + channel + dataPos;
                        fixed (float* pSource = this.data[channel]) {
                            float* source = pSource + offset,
                                end = source + samplesThisRound;
                            while (source != end) {
                                *source++ = *dataIn;
                                dataIn += this.data.Length;
                            }
                        }
                    }
                    dataPos += samplesThisRound * this.data.Length;
                    if ((offset += samplesThisRound) == this.data[0].Length) {
                        offset = 0;
                    }
                }
            }
            return true;
        }
    }
}
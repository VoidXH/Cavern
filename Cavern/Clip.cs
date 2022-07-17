using System;

namespace Cavern {
    /// <summary>
    /// Audio content.
    /// </summary>
    public class Clip { // TOOD: lööps can be optimized to the sky
        /// <summary>
        /// Name of the clip.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Channel count for the clip.
        /// </summary>
        public int Channels {
            get => data.Length;
            set => Array.Resize(ref data, value);
        }

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
            for (int channel = 0; channel < channels; ++channel) {
                float[] targetArray = this.data[channel] = new float[sampleCount];
                for (int sample = 0; sample < sampleCount; ++sample) {
                    targetArray[sample] = data[sample * channels + channel];
                }
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
        public bool GetData(float[][] data, int offset) {
            if (data.Length != this.data.Length) {
                return false; // Channel count doesn't match
            }

            for (int dataPos = 0; dataPos < data[0].Length; ++dataPos) {
                if (offset >= this.data[0].Length) {
                    offset %= this.data[0].Length; // TODO: just don't
                }
                for (int channel = 0; channel < this.data.Length; ++channel) {
                    data[channel][dataPos] = this.data[channel][offset];
                }
                ++offset;
            }
            return true;
        }

        /// <summary>
        /// Fills an array with sample data from the clip.
        /// Clip data doesn't overflow and free samples are filled with zeros.
        /// </summary>
        /// <param name="data">Audio data cache</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        public bool GetDataNonLooping(float[][] data, int offset) {
            if (data.Length != this.data.Length) {
                return false; // Channel count doesn't match
            }

            int dataPos = 0,
                endPos = this.data[0].Length - offset;
            if (endPos > data[0].Length)
                endPos = data[0].Length;
            for (; dataPos < endPos; ++dataPos) {
                for (int channel = 0; channel < this.data.Length; ++channel) {
                    data[channel][dataPos] = this.data[channel][offset];
                }
                ++offset;
            }
            for (int channel = 0; channel < this.data.Length; ++channel) {
                Array.Clear(data[channel], dataPos, data[0].Length - dataPos);
            }
            return true;
        }

        /// <summary>
        /// Fills an array with sample data from the clip.
        /// Clip data overflows, and free samples are filled with the beginning of the Clip.
        /// </summary>
        /// <param name="data">Audio data cache</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        public bool GetData(float[] data, int offset) {
            if (data.Length % this.data.Length != 0) {
                return false; // Channel count doesn't match
            }

            int dataPos = 0;
            while (dataPos < data.Length) {
                if (offset >= this.data[0].Length) {
                    offset %= this.data[0].Length; // TODO: same as above
                }
                for (int channel = 0; channel < this.data.Length; ++channel) {
                    data[dataPos + channel] = this.data[channel][offset];
                }
                dataPos += this.data.Length;
                ++offset;
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
        public bool GetData(float[] data, int channel, int offset) {
            if (data.Length % this.data.Length != 0) {
                return false; // Channel count doesn't match
            }

            int dataPos = 0;
            while (dataPos < data.Length) {
                if (offset >= this.data[0].Length) {
                    offset %= this.data[0].Length; // TODO: same as above
                }
                data[dataPos++] = this.data[channel][offset++];
            }
            return true;
        }

        /// <summary>
        /// Fills an array with sample data from the clip. Clip data doesn't overflow and free samples are filled with zeros.
        /// </summary>
        /// <param name="data">Audio data cache</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        public bool GetDataNonLooping(float[] data, int offset) {
            if (data.Length != this.data.Length) {
                return false; // Channel count doesn't match
            }

            int dataPos = 0,
                endPos = this.data[0].Length - offset;
            if (endPos > data.Length) {
                endPos = data.Length;
            }
            while (dataPos < endPos) {
                for (int channel = 0; channel < this.data.Length; ++channel) {
                    data[channel + dataPos] = this.data[channel][offset];
                }
                dataPos += this.data.Length;
                ++offset;
            }
            Array.Clear(data, dataPos, data.Length - dataPos);
            return true;
        }

        /// <summary>
        /// Overwrite samples in this clip.
        /// </summary>
        /// <param name="data">Data source</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        public bool SetData(float[][] data, int offset) {
            if (data.Length != this.data.Length) {
                return false; // Channel count doesn't match
            }

            for (int dataPos = 0; dataPos < data[0].Length; ++dataPos) {
                if (offset >= this.data[0].Length) {
                    offset %= this.data[0].Length;
                }
                for (int channel = 0; channel < this.data.Length; ++channel) {
                    this.data[channel][offset] = data[channel][dataPos];
                }
                ++offset;
            }
            return true;
        }

        /// <summary>
        /// Overwrite samples in this clip.
        /// </summary>
        /// <param name="data">Data source</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        public bool SetData(float[] data, int offset) {
            if (data.Length != this.data.Length) {
                return false; // Channel count doesn't match
            }

            int dataPos = 0;
            while (dataPos < data.Length) {
                if (offset >= this.data[0].Length) {
                    offset %= this.data[0].Length;
                }
                for (int channel = 0; channel < this.data.Length; ++channel) {
                    this.data[channel][offset] = data[dataPos + channel];
                }
                dataPos += this.data.Length;
                ++offset;
            }
            return true;
        }
    }
}
using System;

namespace Cavern {
    /// <summary>Audio content.</summary>
    public class Clip {
        /// <summary>Name of the clip.</summary>
        public string Name;

        /// <summary>Channel count for the clip.</summary>
        public int Channels {
            get => samples.Length;
            set => Array.Resize(ref samples, value);
        }

        /// <summary>The length of the clip in samples, for a single channel.</summary>
        public int Samples => samples[0].Length;

        /// <summary>The length of the clip in seconds.</summary>
        public float Length => Samples / (float)SampleRate;

        /// <summary>Sampling rate of the clip.</summary>
        public readonly int SampleRate;

        /// <summary>Samples for each channel.</summary>
        float[][] samples;

        /// <summary>Audio content.</summary>
        /// <param name="data">Audio data, with the size of [channels][samples for given channel]</param>
        /// <param name="sampleRate">Sample rate</param>
        public Clip(float[][] data, int sampleRate) {
            SampleRate = sampleRate;
            samples = data;
        }

        /// <summary>Audio content.</summary>
        /// <param name="data">Audio data, with interlaced channels</param>
        /// <param name="channels">Channel count</param>
        /// <param name="sampleRate">Sample rate</param>
        public Clip(float[] data, int channels, int sampleRate) {
            SampleRate = sampleRate;
            int sampleCount = data.Length / channels;
            samples = new float[channels][];
            for (int channel = 0; channel < channels; ++channel) {
                float[] targetArray = samples[channel] = new float[sampleCount];
                for (int sample = 0; sample < sampleCount; ++sample)
                    targetArray[sample] = data[sample * channels + channel];
            }
        }

        /// <summary>Fills an array with sample data from the clip.</summary>
        /// <param name="data">Audio data cache</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        public bool GetData(float[][] data, int offset) {
            int channels = Channels;
            if (data.Length != channels)
                return false;
            for (int dataPos = 0, dataSize = data[0].Length, sampleCount = Samples; dataPos < dataSize; ++dataPos) {
                if (offset >= sampleCount)
                    offset %= sampleCount;
                for (int channel = 0; channel < channels; ++channel)
                    data[channel][dataPos] = samples[channel][offset];
                ++offset;
            }
            return true;
        }

        /// <summary>Fills an array with sample data from the clip.</summary>
        /// <param name="data">Audio data cache</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        public bool GetData(float[] data, int offset) {
            int channels = Channels;
            if (data.Length % channels != 0)
                return false;
            int dataPos = 0, dataSize = data.Length, sampleCount = Samples;
            while (dataPos < dataSize) {
                if (offset >= sampleCount)
                    offset %= sampleCount;
                for (int channel = 0; channel < channels; ++channel)
                    data[dataPos + channel] = samples[channel][offset];
                dataPos += channels;
                ++offset;
            }
            return true;
        }

        /// <summary>Overwrite samples in this clip.</summary>
        /// <param name="data">Data source</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        public bool SetData(float[][] data, int offset) {
            int channels = Channels;
            if (data.Length != channels)
                return false;
            for (int dataPos = 0, dataSize = data[0].Length, sampleCount = Samples; dataPos < dataSize; ++dataPos) {
                if (offset >= sampleCount)
                    offset %= sampleCount;
                for (int channel = 0; channel < channels; ++channel)
                    samples[channel][offset] = data[channel][dataPos];
                ++offset;
            }
            return true;
        }

        /// <summary>Overwrite samples in this clip.</summary>
        /// <param name="data">Data source</param>
        /// <param name="offset">Offset from the beginning of the clip in samples, for a single channel</param>
        public bool SetData(float[] data, int offset) {
            int channels = Channels;
            if (data.Length % channels != 0)
                return false;
            int dataPos = 0, dataSize = data.Length, sampleCount = Samples;
            while (dataPos < dataSize) {
                if (offset >= sampleCount)
                    offset %= sampleCount;
                for (int channel = 0; channel < channels; ++channel)
                    samples[channel][offset] = data[dataPos + channel];
                dataPos += channels;
                ++offset;
            }
            return true;
        }

        /// <summary>Implicit null check.</summary>
        public static implicit operator bool(Clip clip) => clip != null;
    }
}
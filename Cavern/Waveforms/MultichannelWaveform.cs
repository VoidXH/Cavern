using System.Runtime.CompilerServices;

using Cavern.Utilities;
using Cavern.Waveforms;

namespace Cavern {
    /// <summary>
    /// Contains multiple waveforms of the same length.
    /// </summary>
    public class MultichannelWaveform : MultichannelBase<float> {
        /// <summary>
        /// Construct a multichannel data from multiple mono waveforms.
        /// </summary>
        public MultichannelWaveform(params float[][] source) : base(source) { }

        /// <summary>
        /// Construct an empty multichannel waveform of a given size.
        /// </summary>
        public MultichannelWaveform(int channels, int samplesPerChannel) : base(channels, samplesPerChannel) { }

        /// <summary>
        /// Construct a multichannel waveform from an interlaced signal.
        /// </summary>
        public MultichannelWaveform(float[] source, int channels) : this(channels, source.Length / channels) {
            for (int channel = 0; channel < channels; channel++) {
                WaveformUtils.ExtractChannel(source, this[channel], channel, channels);
            }
        }

        /// <inheritdoc/>
        public override object Clone() => new MultichannelWaveform(data.DeepCopy2D());

        /// <summary>
        /// Gets if the contained signal has no amplitude.
        /// </summary>
        public bool IsMute() {
            for (int i = 0; i < data.Length; i++) {
                if (!data[i].IsMute()) {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Split this signal to blocks of a given <paramref name="blockSize"/> on each channel.
        /// </summary>
        public MultichannelWaveform[] Split(int blockSize) {
            MultichannelWaveform[] result = new MultichannelWaveform[data[0].Length / blockSize];
            for (int block = 0; block < result.Length; block++) {
                int start = block * blockSize,
                    end = start + blockSize;
                float[][] target = new float[data.Length][];
                for (int channel = 0; channel < data.Length; channel++) {
                    target[channel] = data[channel][start..end];
                }
                result[block] = new MultichannelWaveform(target);
            }
            return result;
        }

        /// <summary>
        /// Multiplies all values in all channels' signals.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Gain(float multiplier) {
            for (int i = 0; i < data.Length; ++i) {
                float[] channel = data[i];
                for (int j = 0; j < channel.Length; j++) {
                    channel[j] *= multiplier;
                }
            }
        }

        /// <summary>
        /// Get the peak amplitude across all channels.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetPeak() {
            float max = data[0].GetPeak();
            for (int i = 1; i < data.Length; i++) {
                float current = data[i].GetPeak();
                if (max < current) {
                    max = current;
                }
            }
            return max;
        }

        /// <summary>
        /// Set the peak signal across all channels to 1 (0 dB FS).
        /// </summary>
        public void Normalize() => Gain(1 / GetPeak());

        /// <summary>
        /// Remove the 0s from the beginning of the multichannel signal.
        /// </summary>
        public void TrimStart() {
            int min = data[0].Length;
            for (int i = 0; i < data.Length; i++) {
                float[] check = data[i];
                int trim = 0;
                while (check[trim] == 0 && trim < check.Length) {
                    ++trim;
                }
                if (min > trim) {
                    min = trim;
                }
            }
            if (min != 0) {
                for (int i = 0; i < data.Length; i++) {
                    data[i] = data[i][min..];
                }
            }
        }

        /// <summary>
        /// Remove the 0s from the end of the signal, but keep the lengths of jagged arrays equal to the longest cut channel.
        /// </summary>
        public void TrimEnd() {
            int max = 0;
            for (int i = 0; i < data.Length; i++) {
                float[] check = data[i];
                int trim = check.Length;
                while (trim > 0 && check[trim - 1] == 0) {
                    --trim;
                }
                if (max < trim) {
                    max = trim;
                }
            }
            if (max != data[0].Length) {
                for (int i = 0; i < data.Length; i++) {
                    data[i] = data[i][..max];
                }
            }
        }
    }
}

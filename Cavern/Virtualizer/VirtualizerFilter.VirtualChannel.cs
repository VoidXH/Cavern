using System;

using Cavern.Channels;
using Cavern.Filters;
using Cavern.Utilities;

namespace Cavern.Virtualizer {
    /// <summary>
    /// Represents a virtualizable channel with impulse responses for both ears.
    /// </summary>
    public struct VirtualChannel : IEquatable<VirtualChannel> {
        /// <summary>
        /// Low frequency crossover filter for retaining bass outside the usable impulse response frequency range.
        /// </summary>
        public Crossover Crossover { get; }

        /// <summary>
        /// Combo convolution and delay filter for both ears.
        /// </summary>
        public DualConvolver Filter { get; }

        /// <summary>
        /// Virtual speaker angle difference from the subject's gaze on the vertical axis: elevation.
        /// </summary>
        public float x;

        /// <summary>
        /// Virtual speaker angle difference from the subject's gaze on the horizontal axis: azimuth.
        /// </summary>
        public float y;

        /// <summary>
        /// Constructs a virtualizable channel with impulse responses for both ears.
        /// </summary>
        public VirtualChannel(float x, float y, float[] leftEarIR, float[] rightEarIR, int sampleRate, float crossoverFrequency) {
            this.x = x;
            this.y = y;
            Crossover = new Crossover(sampleRate, crossoverFrequency);
            Filter = new DualConvolver(leftEarIR, rightEarIR, y % 180 > 0 ? GetDelay(y % 180) : 0, y < 0 ? GetDelay(-y) : 0);
        }

        /// <summary>
        /// Constructs a virtualizable channel with impulse responses for both ears, recalibrating gains by the position's angles
        /// if they were lost in recording/processing.
        /// </summary>
        public static VirtualChannel FromSubparMeasurement(float x, float y, float[] leftEarIR, float[] rightEarIR,
            int sampleRate, float crossoverFrequency) {
            if (y == 0 || y == 180) { // Middle
                WaveformUtils.Gain(leftEarIR, minus16dB / WaveformUtils.GetRMS(leftEarIR));
                Array.Copy(leftEarIR, rightEarIR, leftEarIR.Length);
            } else {
                float otherGain = minus10dB * QMath.DbToGain(-20 * MathF.Sin(MathF.Abs(y) * VectorExtensions.Deg2Rad));
                if (y < 0) { // Left side
                    WaveformUtils.Gain(leftEarIR, minus10dB / WaveformUtils.GetRMS(leftEarIR));
                    WaveformUtils.Gain(rightEarIR, otherGain / WaveformUtils.GetRMS(rightEarIR));
                } else { // Right side
                    WaveformUtils.Gain(leftEarIR, otherGain / WaveformUtils.GetRMS(leftEarIR));
                    WaveformUtils.Gain(rightEarIR, minus10dB / WaveformUtils.GetRMS(rightEarIR));
                }
            }
            return new VirtualChannel(x, y, leftEarIR, rightEarIR, sampleRate, crossoverFrequency);
        }

        /// <summary>
        /// Parse the virtual channels from a multichannel HRIR set, and use it above 120 Hz.
        /// </summary>
        public static VirtualChannel[] Parse(MultichannelWaveform hrir, int sampleRate) => Parse(hrir, sampleRate, 120);

        /// <summary>
        /// Parse the virtual channels from a multichannel HRIR set, and use it above a set frequency.
        /// </summary>
        public static VirtualChannel[] Parse(MultichannelWaveform hrir, int sampleRate, float crossoverFrequency) {
            hrir.TrimStart();
            hrir.Normalize();

            // L/R impulses of all channels in line
            if (hrir.Channels == 2) {
                int channelCount = 0,
                    lPeak = -1,
                    rPeak = -1;
                float[] l = hrir[0], r = hrir[1];
                for (int i = 0; i < l.Length; i++) {
                    if (l[i] > .5f || r[i] > .5f) {
                        ++channelCount;
                        if (lPeak == -1) {
                            lPeak = i;
                        } else if (rPeak == -1) {
                            rPeak = i;
                        }
                        i += 64; // Impulse responses can't be shorter than this, and this large peaks never happen after 64 samples
                    }
                }

                // Preprocessing
                MultichannelWaveform[] splits = hrir.Split(rPeak - lPeak);
                int lastToKeep = splits.Length;
                while (lastToKeep > 1 && splits[lastToKeep - 1].IsMute()) {
                    --lastToKeep;
                }
                if (splits.Length != lastToKeep) {
                    splits = splits[..lastToKeep];
                }
                for (int i = 0; i < splits.Length; i++) {
                    splits[i].TrimEnd();
                }

                // If there's an odd number of channels, use the center twice (assuming center is the third channel) -
                // Cavern only works with even channels, and this might keep symmetry
                if (splits.Length % 2 == 1) {
                    float[][][] newSplits = new float[splits.Length + 1][][];
                    Array.Copy(splits, newSplits, 3);
                    newSplits[3] = newSplits[2];
                    Array.Copy(splits, 3, newSplits, 4, splits.Length - 3);
                }

                ReferenceChannel[] channels = ChannelPrototype.GetStandardMatrix(splits.Length);
                VirtualChannel[] result = new VirtualChannel[splits.Length];
                for (int i = 0; i < result.Length; i++) {
                    ChannelPrototype prototype = ChannelPrototype.Mapping[(int)channels[i]];
                    // For the LFE, use the center's impulses, otherwise the LFE would be an overriding channel at the same position,
                    // and this would cut off high frequencies
                    MultichannelWaveform split = !prototype.LFE ? splits[i] : splits[i - 1];
                    result[i] = new VirtualChannel(prototype.X, prototype.Y, split[0], split[1], sampleRate, crossoverFrequency);
                }
                return result;
            }

            // Every channel is a single impulse
            else {
                throw new NotSupportedException("Non-stereo HRIR file");
            }
        }

        /// <summary>
        /// Get the secondary ear's delay by angle of attack.
        /// </summary>
        /// <remarks>This formula is based on measurements and the sine wave's usability was disproven.
        /// See Bence S. (2022). Extending HRTF with distance simulation based on ray-tracing.</remarks>
        static int GetDelay(float angle) => (int)((90 - Math.Abs(angle - 90)) / 2.7f);

        /// <summary>
        /// Check if the two virtual channels handle the same source channel position.
        /// </summary>
        public bool Equals(VirtualChannel other) => x == other.x && y == other.y;

        /// <summary>
        /// Precalculated -10 dB as voltage gain. Used by angle gain post-processing.
        /// </summary>
        const float minus10dB = .31622776601f;

        /// <summary>
        /// Precalculated -16 dB as voltage gain. Used by angle gain post-processing for middle channels to prevent double volume.
        /// </summary>
        const float minus16dB = .15848931924f;
    }
}
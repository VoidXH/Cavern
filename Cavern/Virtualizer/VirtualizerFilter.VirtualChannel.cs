using System;

using Cavern.Filters;
using Cavern.Remapping;
using Cavern.Utilities;

namespace Cavern.Virtualizer {
    /// <summary>
    /// Represents a virtualizable channel with impulse responses for both ears.
    /// </summary>
    public struct VirtualChannel {
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
        /// Parse the virtual channels from a multichannel HRIR set.
        /// </summary>
        public static VirtualChannel[] Parse(float[][] hrir, int sampleRate, float crossoverFrequency = 120) {
            hrir.TrimStart();
            hrir.Normalize();

            // L/R impulses of all channels in line
            if (hrir.Length == 2) {
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
                float[][][] splits = hrir.Split(rPeak - lPeak);
                int lastToKeep = splits.Length;
                while (splits[lastToKeep - 1].IsMute() && --lastToKeep > 0) ;
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
                    float[][] split = !prototype.LFE ? splits[i] : splits[i - 1];
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
    }
}
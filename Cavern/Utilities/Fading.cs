namespace Cavern.Utilities {
    /// <summary>
    /// Different track fading techniques.
    /// </summary>
    public static class Fading {
        /// <summary>
        /// Performs a linear fade for a mono channel from the <paramref name="source"/> into the <paramref name="target"/>.
        /// </summary>
        /// <param name="source">Starts at max gain, fades to mute</param>
        /// <param name="target">Starts at mute, fades to max gain</param>
        /// <param name="samples">The duration of the fade in samples</param>
        public static unsafe void Linear(float[] source, float[] target, int samples) {
            fixed (float* sourcePin = source)
            fixed (float* targetPin = target) {
                float* pSource = sourcePin;
                float* pTarget = targetPin;
                float step = 1f / samples;

                for (int i = 0; i < samples; i++) {
                    float mulRight = i * step;
                    float mulLeft = 1 - mulRight;
                    *pTarget = *pSource++ * mulLeft + *pTarget * mulRight;
                    pTarget++;
                }
            }
        }

        /// <summary>
        /// Performs a linear fade from the <paramref name="source"/> into the <paramref name="target"/>.
        /// </summary>
        /// <param name="source">Starts at max gain, fades to mute</param>
        /// <param name="target">Starts at mute, fades to max gain</param>
        /// <param name="samples">The duration of the fade in samples, for a single channel</param>
        /// <param name="channels">Number of the channels in the interlaced waveform</param>
        public static unsafe void Linear(float[] source, float[] target, int samples, int channels) {
            fixed (float* sourcePin = source)
            fixed (float* targetPin = target) {
                float* pSource = sourcePin,
                    pTarget = targetPin;
                float step = 1f / samples;

                for (int i = 0; i < samples; i++) {
                    float mulRight = i * step,
                        mulLeft = 1 - mulRight;
                    for (int j = 0; j < channels; j++) {
                        pTarget[j] = pSource[j] * mulLeft + pTarget[j] * mulRight;
                    }
                    pSource += channels;
                    pTarget += channels;
                }
            }
        }

        /// <summary>
        /// Performs a linear fade from the <paramref name="source"/> into the <paramref name="target"/>.
        /// </summary>
        /// <param name="source">Starts at max gain, fades to mute</param>
        /// <param name="target">Starts at mute, fades to max gain</param>
        /// <param name="samples">The duration of the fade in samples, for a single channel</param>
        public static void Linear(MultichannelWaveform source, MultichannelWaveform target, int samples) {
            for (int i = 0; i < source.Channels; i++) {
                Linear(source[i], target[i], samples);
            }
        }
    }
}
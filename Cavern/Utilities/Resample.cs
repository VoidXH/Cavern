namespace Cavern.Utilities {
    /// <summary>Audio resampling functions.</summary>
    public static class Resample {
        /// <summary>Resamples a single channel with the quality set by the user.</summary>
        /// <param name="samples">Samples of the source channel</param>
        /// <param name="to">New sample count</param>
        /// <param name="quality">Listener audio quality</param>
        /// <returns>Resampled version of the input channel</returns>
        public static float[] Adaptive(float[] samples, int to, QualityModes quality) {
            if (quality < QualityModes.High)
                return NearestNeighbour(samples, to);
            else if (quality < QualityModes.Perfect)
                return Lerp(samples, to);
            else
                return CatmullRom(samples, to);
        }

        /// <summary>Resamples a multichannel array with the quality set by the user.</summary>
        /// <param name="samples">Samples of the source channel</param>
        /// <param name="to">New sample count per channel</param>
        /// <param name="channels">Channel count</param>
        /// <param name="quality">Listener audio quality</param>
        /// <returns>Resampled version of the input</returns>
        public static float[] Adaptive(float[] samples, int to, int channels, QualityModes quality) {
            float[][] channelSplit = new float[channels][];
            int splitSize = samples.Length / channels;
            for (int channel = 0; channel < channels; ++channel)
                channelSplit[channel] = new float[splitSize];
            for (int sample = 0, outSample = 0; sample < splitSize; ++sample)
                for (int channel = 0; channel < channels; ++channel, ++outSample)
                    channelSplit[channel][sample] = samples[outSample];
            for (int channel = 0; channel < channels; ++channel)
                channelSplit[channel] = Adaptive(channelSplit[channel], to, quality);
            int newUpdateRate = channelSplit[0].Length;
            samples = new float[channels * newUpdateRate];
            for (int sample = 0, outSample = 0; sample < newUpdateRate; ++sample)
                for (int channel = 0; channel < channels; ++channel, ++outSample)
                    samples[outSample] = channelSplit[channel][sample];
            return samples;
        }

        /// <summary>Resamples a single channel with medium quality (nearest neighbour).</summary>
        /// <param name="samples">Samples of the source channel</param>
        /// <param name="to">New sample count</param>
        /// <returns>Returns a resampled version of the given array</returns>
        public static float[] NearestNeighbour(float[] samples, int to) {
            if (samples.Length == to)
                return samples;
            float[] output = new float[to];
            float ratio = samples.Length / (float)to;
            for (int i = 0; i < to; ++i)
                output[i] = samples[(int)(i * ratio)];
            return output;
        }

        /// <summary>Resamples a single channel with medium quality (linear interpolation).</summary>
        /// <param name="samples">Samples of the source channel</param>
        /// <param name="to">New sample count</param>
        /// <returns>Returns a resampled version of the given array</returns>
        public static float[] Lerp(float[] samples, int to) {
            if (samples.Length == to)
                return samples;
            float[] output = new float[to];
            float ratio = samples.Length / (float)to;
            int end = samples.Length - 1;
            for (int i = 0; i < to; ++i) {
                float fromPos = i * ratio;
                int sample = (int)fromPos;
                if (sample < end)
                    output[i] = QMath.Lerp(samples[sample], samples[++sample], fromPos % 1);
                else
                    output[i] = samples[sample];
            }
            return output;
        }

        /// <summary>Resamples a single channel with high quality (Catmull-Rom spline).</summary>
        /// <param name="samples">Samples of the source channel</param>
        /// <param name="to">New sample count</param>
        /// <returns>Returns a resampled version of the given array</returns>
        public static float[] CatmullRom(float[] samples, int to) {
            int from = samples.Length;
            if (from == to)
                return samples;
            float[] output = new float[to];
            float ratio = from / (float)to;
            int start = (int)(1 / ratio + 1), end = from - 3;
            for (int i = 0; i < start; ++i)
                output[i] = samples[i];
            for (int i = start; i < to; ++i) {
                float fromPos = i * ratio;
                int sample = (int)fromPos;
                if (sample < end) {
                    float t = fromPos % 1, t2 = t * t;
                    float p0 = samples[sample - 1], p1 = samples[sample], p2 = samples[sample + 1], p3 = samples[sample + 2];
                    output[i] = ((p1 * 2) + (p2 - p0) * t + (p0 * 2 - p1 * 5 + p2 * 4 - p3) * t2 +
                        (3 * p1 - p0 - 3 * p2 + p3) * t2 * t) * .5f;
                } else
                    output[i] = samples[sample];
            }
            return output;
        }
    }
}
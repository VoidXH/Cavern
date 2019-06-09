namespace Cavern.Utilities {
    /// <summary>Audio resampling functions.</summary>
    public static class Resample {
        /// <summary>Resamples a single channel with the quality set by the user.</summary>
        /// <param name="samples">Samples of the source channel</param>
        /// <param name="to">New sample count</param>
        /// <param name="quality">Listener audio quality</param>
        /// <returns>Returns a resampled version of the given array</returns>
        public static float[] Adaptive(float[] samples, int to, QualityModes quality) {
            if (quality < QualityModes.High)
                return NearestNeighbour(samples, to);
            else if (quality < QualityModes.Perfect)
                return Lerp(samples, to);
            else
                return CatmullRom(samples, to);
        }

        /// <summary>Resamples a single channel with medium quality (nearest neighbour).</summary>
        /// <param name="samples">Samples of the source channel</param>
        /// <param name="to">New sample count</param>
        /// <returns>Returns a resampled version of the given array</returns>
        public static float[] NearestNeighbour(float[] samples, int to) {
            int from = samples.Length;
            if (from == to)
                return samples;
            float[] output = new float[to];
            float ratio = from / (float)to;
            for (int i = 0; i < to; ++i)
                output[i] = samples[(int)(i * ratio)];
            return output;
        }

        /// <summary>Resamples a single channel with medium quality (linear interpolation).</summary>
        /// <param name="samples">Samples of the source channel</param>
        /// <param name="to">New sample count</param>
        /// <returns>Returns a resampled version of the given array</returns>
        public static float[] Lerp(float[] samples, int to) {
            int from = samples.Length;
            if (from == to)
                return samples;
            float[] output = new float[to];
            float ratio = from / (float)to;
            int end = samples.Length - 1;
            for (int i = 0; i < to; ++i) {
                float fromPos = i * ratio;
                int sample = (int)fromPos;
                if (sample < end)
                    output[i] = Utils.Lerp(samples[sample], samples[++sample], fromPos % 1);
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
            int start = (int)(1 / ratio + 1), end = samples.Length - 3;
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
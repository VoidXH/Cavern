using System;

namespace Cavern.Utilities {
    /// <summary>
    /// Averages any number of frames continuously.
    /// </summary>
    public class InfiniteAverage : MovingAverage {
        /// <summary>
        /// Number of captured and averaged frames.
        /// </summary>
        int frames;

        /// <summary>
        /// Averages any number of frames continuously.
        /// </summary>
        public InfiniteAverage() : base(0) { }

        /// <summary>
        /// Compute the average by adding the next frame.
        /// </summary>
        /// <remarks>The length of <paramref name="frame"/> must be constant across the use of this object.</remarks>
        public override void AddFrame(float[] frame) {
            frames++;
            if (Average != null) {
                float mul = 1f / (frames + 1);
                WaveformUtils.Gain(Average, frames * mul);
                WaveformUtils.Mix(frame, Average, mul);
            } else {
                Average = new float[frame.Length];
                Array.Copy(frame, Average, frame.Length);
            }
        }

        /// <summary>
        /// Reset the averaging.
        /// </summary>
        public override void Reset() {
            base.Reset();
            frames = 0;
        }
    }
}
using System;

namespace Cavern.Utilities {
    /// <summary>
    /// Averages multiple frames of windowed audio or spectrum data, and when a new window is added, the last one is removed.
    /// </summary>
    public class MovingAverage {
        /// <summary>
        /// The current moving average.
        /// </summary>
        public float[] Average { get; protected set; }

        /// <summary>
        /// The windows to average.
        /// </summary>
        readonly float[][] windows;

        /// <summary>
        /// Averages multiple frames of windowed audio or spectrum data, and when a new window is added, the last one is removed.
        /// </summary>
        /// <param name="frames">Number of windows to average</param>
        public MovingAverage(int frames) => windows = new float[frames][];

        /// <summary>
        /// Compute the <see cref="Average"/> by adding the next frame.
        /// </summary>
        /// <remarks>The length of <paramref name="frame"/> must be constant across the use of this object.</remarks>
        public virtual void AddFrame(float[] frame) {
            if (Average != null) {
                float mixGain = 1f / windows.Length;
                WaveformUtils.Mix(windows[0], Average, -mixGain);
                WaveformUtils.Mix(frame, Average, mixGain);
                for (int i = 1; i < windows.Length; i++) {
                    windows[i - 1] = windows[i];
                }
                windows[^1] = frame;
            } else {
                Average = new float[frame.Length];
                Array.Copy(frame, Average, frame.Length);
                for (int i = 0; i < windows.Length; i++) {
                    windows[i] = frame;
                }
            }
        }

        /// <summary>
        /// Reset the averaging.
        /// </summary>
        public virtual void Reset() => Average = null;
    }
}
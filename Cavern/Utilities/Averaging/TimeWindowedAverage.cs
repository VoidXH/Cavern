using System;
using System.Collections.Generic;

namespace Cavern.Utilities {
    /// <summary>
    /// Averages multiple frames of windowed audio or spectrum data over a fixed time span.
    /// </summary>
    public class TimeWindowedAverage : MovingAverage {
        /// <summary>
        /// Evaluation window size.
        /// </summary>
        readonly TimeSpan timeSpan;

        /// <summary>
        /// The average is accumulated in this array.
        /// </summary>
        float[] cache;

        /// <summary>
        /// Parsed windows to average.
        /// </summary>
        Queue<(DateTime creationTime, float[] samples)> frames;

        /// <summary>
        /// Averages multiple frames of windowed audio or spectrum data over a fixed time span.
        /// </summary>
        public TimeWindowedAverage(TimeSpan timeSpan) : base(0) => this.timeSpan = timeSpan;

        /// <summary>
        /// Compute the average by adding the next frame.
        /// </summary>
        /// <remarks>If the length of the <paramref name="frame"/> changes, the averaging will be reset.</remarks>
        public override void AddFrame(float[] frame) {
            if (cache == null || cache.Length != frame.Length) {
                cache = new float[frame.Length];
                Average = new float[frame.Length];
                frames = new Queue<(DateTime creationTime, float[] samples)>();
            }

            DateTime now = DateTime.Now;
            while (frames.TryPeek(out (DateTime creationTime, float[] samples) first) && first.creationTime + timeSpan < now) {
                WaveformUtils.Mix(first.samples, cache, -1);
                frames.Dequeue();
            }
            WaveformUtils.Mix(frame, cache);
            frames.Enqueue((now, frame));

            WaveformUtils.Insert(cache, Average, 1f / frames.Count);
        }
    }
}
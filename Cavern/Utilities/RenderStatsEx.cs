using System;

namespace Cavern.Utilities {
    /// <summary>
    /// Rendering environment statistics evaluator extended with audio quality metrics.
    /// </summary>
    public sealed class RenderStatsEx : RenderStats {
        /// <summary>
        /// Peak signal level of a frame across all audio frames in relative signal level.
        /// </summary>
        public float FrameLevelPeak { get; private set; }

        /// <summary>
        /// RMS level of the entire measured audio signal in relative signal level.
        /// </summary>
        public float FrameLevelRMS => MathF.Sqrt(rms.Sum / frames);

        /// <summary>
        /// The dynamic range of the entire measured audio signal in relative signal level.
        /// </summary>
        public float Macrodynamics => FrameLevelPeak / FrameLevelRMS;

        /// <summary>
        /// The maximum dynamic range that happens in a second, in relative signal level.
        /// </summary>
        public float Microdynamics { get; private set; }

        /// <summary>
        /// Peak signal level of a frame across all audio frames on the LFE channel in relative signal level.
        /// </summary>
        public float LFELevelPeak { get; private set; }

        /// <summary>
        /// RMS level of the entire measured audio signal on the LFE channel in relative signal level.
        /// </summary>
        public float LFELevelRMS => MathF.Sqrt(rms.Sum / frames);

        /// <summary>
        /// The dynamic range of the entire measured audio signal on the LFE channel in relative signal level.
        /// </summary>
        public float LFEMacrodynamics => FrameLevelPeak / FrameLevelRMS;

        /// <summary>
        /// The maximum dynamic range that happens in a second on the LFE channel, in relative signal level.
        /// </summary>
        public float LFEMicrodynamics { get; private set; }

        /// <summary>
        /// Used for calculating the global RMS by RMS(each frame's RMS). This holds the sum of squares and
        /// has to be evaluated with <see cref="FrameLevelRMS"/>.
        /// </summary>
        readonly AccurateSum rms = new AccurateSum();

        /// <summary>
        /// Number of measured audio frames.
        /// </summary>
        int frames;

        /// <summary>
        /// Averages the required values for calculating <see cref="Microdynamics"/>.
        /// </summary>
        MovingAverage averager;

        /// <summary>
        /// Process this many frames before the values of <see cref="averager"/> become valid.
        /// </summary>
        int skip;

        /// <summary>
        /// Rendering environment statistics evaluator extended with audio quality metrics.
        /// </summary>
        public RenderStatsEx(Listener listener) : base(listener) { }

        /// <summary>
        /// Update the stats according to the last <paramref name="frame"/>.
        /// </summary>
        /// <remarks>The <paramref name="frame"/> size must be constant across calls to get accurate results.</remarks>
        public override void Update(float[] frame) {
            base.Update(frame);

            float currentRMS = WaveformUtils.GetRMS(frame);
            if (FrameLevelPeak < currentRMS) {
                FrameLevelPeak = currentRMS;
            }
            currentRMS *= currentRMS;
            rms.Add(currentRMS);
            ++frames;

            if (averager == null) {
                skip = listener.SampleRate / frame.Length;
                averager = new MovingAverage(skip);
                return;
            }
            averager.AddFrame(new[] { currentRMS });
            if (skip-- > 0) {
                return;
            }
            float microdynamics = currentRMS / averager.Average[0];
            if (Microdynamics < microdynamics) {
                Microdynamics = microdynamics;
            }
        }
    }
}